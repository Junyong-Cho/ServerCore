using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    protected virtual void RegisterRecv()
    {
        if (_disconnected == 1)
            return;

        Interlocked.Increment(ref _refCount);

        if (_recvBuffer.FreeSize < 1024)
            _recvBuffer.Clean();

        var segment = _recvBuffer.WriteSegment;

        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

        try
        {
            bool pending = _socket!.ReceiveAsync(_recvArgs);

            if (pending == false)
                OnRecvComplete(_socket, _recvArgs);
        }
        catch(Exception e)
        {
            LogExceptionAndDisconnect(e);
            return;
        }
    }

    protected virtual void OnRecvComplete(object? sender, SocketAsyncEventArgs recvArgs)
    {
        if (recvArgs.SocketError != SocketError.Success)
        {
            LogExceptionAndDisconnect(recvArgs.SocketError);
            return;
        }

        int byteTransferred = recvArgs.BytesTransferred;

        if (byteTransferred <= 0)
        {
            LogExceptionAndDisconnect("ZeroByte Transferred");
            return;
        }

        if (_recvBuffer.OnWrite(byteTransferred) == false)
        {
            LogExceptionAndDisconnect("UnExpected Error on RecvBuffer Writing");
            return;
        }

        int len = OnRecv(_recvBuffer.ReadSegment);

        if (len < 0)
        {
            LogExceptionAndDisconnect($"RecvSession Packet Processing Error");
            return;
        }

        if (_recvBuffer.OnRead(len) == false)
        {
            LogExceptionAndDisconnect("UnExpected Error on RecvBuffer Reading");
            return;
        }

        Release(RegisterRecv);
    }
}

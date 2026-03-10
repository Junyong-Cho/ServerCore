using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    protected virtual void RegisterRecv()
    {
        Interlocked.Increment(ref _refCount);

        while (true)
        {
            if (_disconnected == 1)
            {
                Release();
                return;
            }

            if (_recvBuffer.FreeSize < 1024)
                _recvBuffer.Clean();

            var segment = _recvBuffer.WriteSegment;

            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try
            {
                bool pending = _socket!.ReceiveAsync(_recvArgs);

                if (pending == true)
                    return;

                OnRecvComplete(null, _recvArgs);
            }
            catch(Exception e)
            {
                LogExceptionAndDisconnectAndRelease(e);
                return;
            }
        }
    }

    protected virtual void OnRecvComplete(object? sender, SocketAsyncEventArgs recvArgs)
    {
        if (recvArgs.SocketError != SocketError.Success)
        {
            LogExceptionAndDisconnectAndRelease(recvArgs.SocketError);
            return;
        }

        int byteTransferred = recvArgs.BytesTransferred;

        if (byteTransferred <= 0)
        {
            LogExceptionAndDisconnectAndRelease(null);
            return;
        }

        if (_recvBuffer.OnWrite(byteTransferred) == false)
        {
            LogExceptionAndDisconnectAndRelease("UnExpected Error on RecvBuffer Writing");
            return;
        }

        int len = 0;

        try
        {
            len = OnRecv(_recvBuffer.ReadSegment);
        }
        catch(Exception e)
        {
            Console.WriteLine("OnRecv Error");
            LogExceptionAndDisconnectAndRelease(e);
            return;
        }

        if (len < 0)
        {
            LogExceptionAndDisconnectAndRelease($"RecvSession Packet Processing Error");
            return;
        }

        if (_recvBuffer.OnRead(len) == false)
        {
            LogExceptionAndDisconnectAndRelease("UnExpected Error on RecvBuffer Reading");
            return;
        }

        if (sender == null)
            return;

        RegisterRecv();

        Release();
    }
}

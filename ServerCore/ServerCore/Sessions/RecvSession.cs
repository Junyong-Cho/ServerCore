using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    protected virtual void RegisterRecv()
    {
#if DEBUG
        Hold(true);
#endif
        Hold();

        while (true)
        {
            if (_isDisconnected == 1)
            {
#if DEBUG
                Release(true);
#endif
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
#if DEBUG
                Release(true);
#endif
                LogExceptionAndDisconnectAndRelease(e);
                return;
            }
        }
    }

    protected virtual void OnRecvComplete(object? sender, SocketAsyncEventArgs recvArgs)
    {
        if (recvArgs.SocketError != SocketError.Success)
        {
#if DEBUG
            Release(true);
#endif
            LogExceptionAndDisconnectAndRelease($"OnRecvComplete : {recvArgs.SocketError}");
            return;
        }

        int bytesTransferred = recvArgs.BytesTransferred;

        if (bytesTransferred <= 0)
        {
#if DEBUG
            Release(true);
#endif
            LogExceptionAndDisconnectAndRelease($"OnRecvComplete BytesTransferred {bytesTransferred}");
            return;
        }

        if (_recvBuffer.OnWrite(bytesTransferred) == false)
        {
#if DEBUG
            Release(true);
#endif
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
#if DEBUG
            Release(true);
#endif
            Console.WriteLine("OnRecv Error");
            LogExceptionAndDisconnectAndRelease(e);
            return;
        }

        if (len < 0)
        {
#if DEBUG
            Release(true);
#endif
            LogExceptionAndDisconnectAndRelease($"RecvSession Packet Processing Error");
            return;
        }

        if (_recvBuffer.OnRead(len) == false)
        {
#if DEBUG
            Release(true);
#endif
            LogExceptionAndDisconnectAndRelease("UnExpected Error on RecvBuffer Reading");
            return;
        }

        if (sender == null)
            return;

        RegisterRecv();

#if DEBUG
        Release(true);
#endif
        Release();
    }
}

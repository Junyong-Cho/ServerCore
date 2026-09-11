using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    protected virtual void RegisterRecv()
    {
        Hold();

        while (true)
        {
            if (_isDisconnected == 1)
            {
                Release();
                return;
            }

            if (_recvBuffer.FreeSize < 1024)
                _recvBuffer.Clean();

            var segment = _recvBuffer.WriteSegment;

            try
            {
                _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

                bool pending = _socket!.ReceiveAsync(_recvArgs);

                if (pending == true)
                    return;

                OnRecvComplete(null, _recvArgs);
            }
            catch(Exception e)
            {
                LogExceptionAndDisconnect(e);
                Release();
                return;
            }
        }
    }

    protected virtual void OnRecvComplete(object? sender, SocketAsyncEventArgs recvArgs)
    {
        try
        {
            if (recvArgs.SocketError != SocketError.Success)
            {
                LogExceptionAndDisconnect($"OnRecvComplete : {recvArgs.SocketError}");
                return;
            }

            int bytesTransferred = recvArgs.BytesTransferred;

            if (bytesTransferred <= 0)
            {
                LogExceptionAndDisconnect($"OnRecvComplete BytesTransferred {bytesTransferred}");
                return;
            }

            if (_recvBuffer.OnWrite(bytesTransferred) == false)
            {
                LogExceptionAndDisconnect("UnExpected Error on RecvBuffer Writing");
                return;
            }

            int len = 0;

            try
            {
                len = OnRecv(_recvBuffer.ReadSegment);
            }
            catch (Exception e)
            {
                Console.WriteLine("OnRecv Error");
                LogExceptionAndDisconnect(e);
                return;
            }

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
            if (sender != null)
                RegisterRecv();
        }
        finally
        {
            if (sender != null)
                Release();
        }
    }
}

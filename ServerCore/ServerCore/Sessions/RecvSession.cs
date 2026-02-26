using System.Net.Sockets;

namespace ServerCore.Sessions;

partial class Session
{
    protected virtual void RegisterRecv()
    {
        if (_disconnected == 1)
            return;

        if (_recvBuffer.FreeSize < 1024)
            _recvBuffer.Clean();

        var segment = _recvBuffer.WriteSegment;

        _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

        try
        {
            bool pending = _socket!.ReceiveAsync(_recvArgs);

            if (pending == false)
                OnRecvComplete(null, _recvArgs);
        }
        catch(Exception e)
        {
            Console.WriteLine($"UnExpected RegisterRecv Error : {_socket!.RemoteEndPoint}");
            Console.WriteLine(e);
            Disconnect();
            return;
        }
    }

    protected virtual void OnRecvComplete(object? sender, SocketAsyncEventArgs recvArgs)
    {
        if (recvArgs.SocketError != SocketError.Success)
        {
            Console.WriteLine($"{recvArgs.SocketError} of {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        int byteTransferred = recvArgs.BytesTransferred;

        if (byteTransferred <= 0)
        {
            Console.WriteLine($"Zero Bytes Received : {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        if (_recvBuffer.OnWrite(byteTransferred) == false)
        {
            Console.WriteLine($"UnExpected Error on RecvBuffer Writing : {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        int len = OnRecv(_recvBuffer.ReadSegment);

        if (len < 0)
        {
            Console.WriteLine($"RecvSession Packet Processing Error : {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        if (_recvBuffer.OnRead(len) == false)
        {
            Console.WriteLine($"UnExpected Error on RecvBuffer Reading: {_socket!.RemoteEndPoint}");
            Disconnect();
            return;
        }

        RegisterRecv();
    }
}

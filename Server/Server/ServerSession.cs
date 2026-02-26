using ServerCore.Buffers;
using ServerCore.Sessions;
using System.Text;

namespace Server;

internal class ServerSession : Session
{

    protected override void OnConnect()
    {
        Console.WriteLine($"Connected User : {_socket!.RemoteEndPoint}");
        string msg = $"Hello client {_socket.RemoteEndPoint}";

        var segment = SendBufferHandler.Open(Encoding.UTF8.GetMaxByteCount(msg.Length));
        int len = Encoding.UTF8.GetBytes(msg, segment);

        segment = SendBufferHandler.Close(len);

        Send(segment);
    }

    protected override void OnDisconnect()
    {
        Console.WriteLine($"Disconnected User : {_socket!.RemoteEndPoint}");
        SessionPool<ServerSession>.Return(this);
    }

    protected override int OnRecv(ArraySegment<byte> segment)
    {
        string msg = Encoding.UTF8.GetString(segment);

        Console.WriteLine($"[From {_socket!.RemoteEndPoint} : {msg}");

        var sendSegment = SendBufferHandler.Open(segment.Count);

        Buffer.BlockCopy(segment.Array!, segment.Offset, sendSegment.Array!, sendSegment.Offset, segment.Count);

        sendSegment = SendBufferHandler.Close(segment.Count);

        Send(sendSegment);

        return segment.Count;
    }

    protected override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"To {_socket!.RemoteEndPoint} {numOfBytes} Sended");
    }
}

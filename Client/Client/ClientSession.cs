using ServerCore.Sessions;
using System.Text;

namespace Client;

internal class ClientSession : Session
{
    protected override void OnConnect()
    {
        Console.WriteLine($"Connected {_socket!.RemoteEndPoint}");
    }

    protected override void OnDisconnect()
    {
        Console.WriteLine($"Disconnected {_socket!.RemoteEndPoint}");
    }

    protected override int OnRecv(ArraySegment<byte> segment)
    {
        string msg = Encoding.UTF8.GetString(segment);

        Console.WriteLine($"[From Server] : {msg}");

        return segment.Count;
    }

    protected override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"{numOfBytes} Sended");
    }
}
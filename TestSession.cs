using System.Net;
using System.Text;

internal class TestSession : Session
{
    protected override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"Connected: {endPoint.ToString()}");

        string data = $"Hello {endPoint.ToString()}";
        int byteCount = Encoding.UTF8.GetMaxByteCount(data.Length);

        var segment = SendBufferHandler.Open(byteCount);

        byteCount = Encoding.UTF8.GetBytes(data, segment);

        SendBufferHandler.Close(byteCount);

        Send(segment);
    }

    protected override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"Disconnected: {endPoint.ToString()}");
    }

    protected override int OnRecv(ArraySegment<byte> segment)
    {
        string data = Encoding.UTF8.GetString(segment.Array!, segment.Offset, segment.Count);

        Console.WriteLine($"[From]: {data}");

        var sendSegment = SendBufferHandler.Open(segment.Count);

        Array.Copy(segment.Array!, segment.Offset, sendSegment.Array!, sendSegment.Offset, segment.Count);

        SendBufferHandler.Close(segment.Count);

        Send(sendSegment);

        return segment.Count;
    }

    protected override void OnSend(int numOfBytes)
    {
        
    }
}

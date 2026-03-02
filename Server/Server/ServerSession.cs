using ServerCore.Buffers;
using ServerCore.Sessions;
using System.Collections.Concurrent;
using System.Text;

namespace Server;

internal class ServerSession : Session
{
    static ServerSession?[] globalSession = new ServerSession[10000]; // 동접자 1만
    static ConcurrentStack<int> sessionIdxManager = new(Enumerable.Range(0, 10000));

    int sessionIdx;

    protected override void OnConnect()
    {
        if(sessionIdxManager.TryPop(out int idx))
        {
            sessionIdx = idx;

            globalSession[sessionIdx] = this;
        }
        else
        {
            sessionIdx = -1;
            Console.WriteLine("User Over");
            Disconnect();
            return;
        }

        Console.WriteLine($"Connected User : {_socket!.RemoteEndPoint}");
        string msg = $"Hello client {_socket.RemoteEndPoint}";

        var segment = SendBufferHandler.Open(Encoding.UTF8.GetMaxByteCount(msg.Length));
        int len = Encoding.UTF8.GetBytes(msg, segment);

        segment = SendBufferHandler.Close(len);

        Send(segment);
    }

    protected override void OnDisconnect()
    {
        SessionPool<ServerSession>.Return(this);

        Console.WriteLine("Disconnected");

        if (sessionIdx == -1)
            return;

        globalSession[sessionIdx] = null;
        sessionIdxManager.Push(sessionIdx);
    }

    protected override int OnRecv(ArraySegment<byte> segment)
    {
        string msg = Encoding.UTF8.GetString(segment);

        Console.WriteLine($"[From {_socket!.RemoteEndPoint}] : {msg}");

        var sendSegment = SendBufferHandler.Open(segment.Count);

        Buffer.BlockCopy(segment.Array!, segment.Offset, sendSegment.Array!, sendSegment.Offset, segment.Count);

        sendSegment = SendBufferHandler.Close(segment.Count);

        Send(sendSegment);

        return segment.Count;
    }

    protected override void OnSend(int numOfBytes)
    {
        //Console.WriteLine($"To {_socket!.RemoteEndPoint}] {numOfBytes} Sended");
    }
}
using System.Net;
using System.Net.Sockets;

abstract partial class Session
{
    object _lock = new();
    volatile int _disconnected = 0;

    Socket? _socket;

    Queue<ArraySegment<byte>> _sendQueue = new();
    List<ArraySegment<byte>> _pendingList = new();

    RecvBuffer _recvBuffer = new();

    SocketAsyncEventArgs _recvArgs = new();
    SocketAsyncEventArgs _sendArgs = new();

    protected abstract int OnRecv(ArraySegment<byte> segment);
    protected abstract void OnSend(int numOfBytes);
    protected abstract void OnDisconnected(EndPoint endPoint);
    protected abstract void OnConnected(EndPoint endPoint);

    public void Start(Socket socket)
    {
        _socket = socket;
        try
        {
            if (_socket.RemoteEndPoint == null)
            {
                Disconnect();
                return;
            }

        }
        catch(Exception e)
        {
            Console.WriteLine("Session Start Failed");
            Console.WriteLine(e);
            Disconnect();
            return;
        }
        OnConnected(_socket.RemoteEndPoint);

        _sendArgs.Completed += OnSendComplete;
        _recvArgs.Completed += OnRecvComplete;

        RegisterRecv();
    }

    void Disconnect()
    {
        if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            return;

        if (_socket != null && _socket!.RemoteEndPoint != null)
            OnDisconnected(_socket.RemoteEndPoint);

        _socket!.Shutdown(SocketShutdown.Both);
        _socket.Close();

        lock (_lock)
        {
            _sendQueue.Clear();
            _pendingList.Clear();
        }
    }
}

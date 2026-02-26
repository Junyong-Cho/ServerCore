using ServerCore.Buffers;
using System.Net.Sockets;

namespace ServerCore.Sessions;

public abstract partial class Session 
{
    volatile int _disconnected = 0;

    public int Disconnected => _disconnected;

    protected bool _isSending = false;

    protected object _lock = new();

    protected Socket? _socket;

    protected RecvBuffer _recvBuffer;
    protected SocketAsyncEventArgs _recvArgs = new();
    protected SocketAsyncEventArgs _sendArgs = new();

    protected List<ArraySegment<byte>> _sendingList;
    protected List<ArraySegment<byte>> _pendingList;

    protected abstract void OnSend(int numOfBytes);
    protected abstract int OnRecv(ArraySegment<byte> segment);
    protected abstract void OnConnect();
    protected abstract void OnDisconnect();

    public Session(int recvBufferSize = 1<<16, int sendBufferSize = 1<<16, int pendingListSize = 100)
    {
        _recvBuffer = new(recvBufferSize);

        SendBufferHandler.BufferSize = sendBufferSize;

        _sendingList = new(pendingListSize);
        _pendingList = new(pendingListSize);

        _recvArgs.Completed += OnRecvComplete;
        _sendArgs.Completed += OnSendComplete;
    }

    public void Start(Socket socket)
    {
        _socket = socket;

        OnConnect();

        RegisterRecv();
    }

    public virtual void Disconnect()
    {
        if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            return;

        if (_socket == null)
            return;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            OnDisconnect();
        }
        catch { }
    }

    public virtual void Reset()
    {
        _disconnected = 0;
        _isSending = false;
        _recvBuffer.Reset();
        _sendingList.Clear();
        _pendingList.Clear();
    }
}

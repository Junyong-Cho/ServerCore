using ServerCore.Buffers;
using System.Net.Sockets;

namespace ServerCore.Sessions;

public abstract partial class Session 
{
    volatile int _disconnected = 0;
    volatile int _refCount = 1;

    public int Disconnected => _disconnected;

    protected bool _isSending = false;

    protected object _lock = new();

    protected Socket? _socket;

    protected RecvBuffer _recvBuffer;
    protected SocketAsyncEventArgs _recvArgs = new();
    protected SocketAsyncEventArgs _sendArgs = new();

    protected List<ArraySegment<byte>> _sendingList;
    protected List<ArraySegment<byte>> _pendingList;

    protected static LingerOption closeOption = new(true, 0);

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

        try
        {
            _socket!.LingerState = closeOption;
            _socket.Close();
        }
        catch { }

        _socket = null;
        Release();
    }

    protected virtual void LogExceptionAndDisconnect(object log)
    {
        Console.WriteLine(log);
        Disconnect();
        Release();
    }

    protected virtual void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            OnDisconnect();
            return;
        }
    }

    protected virtual void Release(Action RegisterCall)
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            OnDisconnect();
            return;
        }

        RegisterCall();
    }

    public virtual void Reset()
    {
        _disconnected = 0;
        _refCount = 1;
        _isSending = false;
        _recvBuffer.Reset();
        _sendingList.Clear();
        _pendingList.Clear();
    }
}

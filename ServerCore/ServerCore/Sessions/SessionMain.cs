using ServerCore.Buffers;
using System.Net.Sockets;

namespace ServerCore.Sessions;

public abstract partial class Session 
{
    protected volatile int _disconnected = 0;
    protected volatile int _refCount = 1;
    protected bool _isSending = false;

    public int Disconnected => _disconnected;

    protected object _lock = new();

    protected Socket? _socket;

    protected RecvBuffer _recvBuffer;
    protected SocketAsyncEventArgs _recvArgs = new();
    protected SocketAsyncEventArgs _sendArgs = new();

    protected List<ArraySegment<byte>> _sendingList;
    protected List<ArraySegment<byte>> _pendingList;
    protected List<ArraySegment<byte>> _remainList;

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
        _remainList = new(pendingListSize);

        _recvArgs.Completed += OnRecvComplete;
        _sendArgs.Completed += OnSendComplete;
    }

    public virtual void Start(Socket socket)
    {
        _socket = socket;

        try
        {
            OnConnect();
        }
        catch(Exception e)
        {
            Console.WriteLine("OnConnect Error");
            LogExceptionAndDisconnectAndRelease(e);
            return;
        }

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
        
        Release();
    }

    protected virtual void LogExceptionAndDisconnectAndRelease(object? log)
    {
        if (_disconnected == 0 && log !=null)
            Console.WriteLine(log);
        Disconnect();
        Release();
    }

    protected virtual void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            try
            {
                OnDisconnect();
            }
            catch(Exception e)
            {
                Console.WriteLine("OnDisconnect Error");
                Console.WriteLine(e);
            }
            _socket = null;
        }
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

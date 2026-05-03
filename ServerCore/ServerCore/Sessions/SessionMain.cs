using ServerCore.Buffers;
using System.Net;
using System.Net.Sockets;

namespace ServerCore.Sessions;

public abstract partial class Session 
{
    protected volatile int _isDisconnected = 0;
    protected volatile int _refCount = 1;
    protected volatile int _isReleased = 0;
    protected volatile bool _isSending = false;

    public int Disconnected => _isDisconnected;

    protected object _lock = new();

    protected Socket? _socket;

    protected RecvBuffer _recvBuffer;
    protected SocketAsyncEventArgs _recvArgs = new();
    protected SocketAsyncEventArgs _sendArgs = new();

    protected SessionList<ArraySegment<byte>> _sendingList;
    protected SessionList<ArraySegment<byte>> _pendingList;
    protected SessionList<ArraySegment<byte>> _remainList;

    protected Queue<SendBuffer> _refBufferQueue;

    protected static LingerOption closeOption = new(true, 0);

    protected EndPoint? _remote;
    protected abstract int OnRecv(ArraySegment<byte> segment);
    protected abstract void OnConnect();
    protected abstract void OnDisconnect();


    public Session(int recvBufferSize = 1<<16, int sendBufferSize = 1<<16, int pendingListSize = 1024)
    {
        _recvBuffer = new(recvBufferSize);

        SendBufferHandler.BufferSize = sendBufferSize;

        _sendingList = new(pendingListSize);
        _pendingList = new(pendingListSize);
        _remainList = new(pendingListSize);
        _refBufferQueue = new(pendingListSize);

        _recvArgs.Completed += OnRecvComplete;
        _sendArgs.Completed += OnSendComplete;
    }

    public virtual void Start(Socket socket)
    {
        _socket = socket;

        try
        {
            _remote = _socket.RemoteEndPoint!;
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
        if (Interlocked.Exchange(ref _isDisconnected, 1) == 1)
            return;
        
        Release();
    }

    protected virtual void LogExceptionAndDisconnectAndRelease(object? log)
    {
        if (_isDisconnected == 0 && log !=null)
            Console.WriteLine(log);
        Disconnect();
        Release();
    }

    protected virtual void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0 && Interlocked.Exchange(ref _isReleased, 1) == 0)
        {
            while (_refBufferQueue.Count > 0)
                _refBufferQueue.Dequeue().DecreaseReference();

            try
            {
                _socket!.LingerState = closeOption;
                _socket.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            _socket = null;

            try
            {
                OnDisconnect();
            }
            catch(Exception e)
            {
                Console.WriteLine("OnDisconnect Error");
                Console.WriteLine(e);
            }
        }
    }
    public virtual void Reset()
    {
        _isDisconnected = 0;
        _refCount = 1;
        _isReleased = 0;
        _isSending = false;
        _remote = null;
        _recvBuffer.Reset();
        _sendingList.Clear();
        _pendingList.Clear();
        _remainList.Clear();
    }
}

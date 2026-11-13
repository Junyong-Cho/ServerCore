namespace ServerCore.Buffers;

public class SendBuffer
{
    public static int BufferSize => 1 << 16;

    byte[] _buffer;

    int _usedSize;
    int _reservedSize = -1;
    int _refCount;
    int _disposed;
    
    public int FreeSize => _buffer.Length - _usedSize;

    public SendBuffer()
    {
        _buffer = new byte[BufferSize];
        _usedSize = 0;
        _refCount = 1;
        _disposed = 0;
    }

    public ArraySegment<byte> Open(int reserveSize)
    {
        _reservedSize = reserveSize;
        return new(_buffer, _usedSize, reserveSize);
    }

    public ArraySegment<byte> Close(int usedSize)
    {
        if (_reservedSize < 0)
            throw new InvalidOperationException("Open must be called before Close.");

        if (usedSize > _reservedSize)
            throw new ArgumentOutOfRangeException(nameof(usedSize), "Used size exceeds the reserved size.");

        ArraySegment<byte> segment = new(_buffer, _usedSize, usedSize);

        _usedSize += usedSize;
        _reservedSize = -1;

        return segment;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            Release();
    }

    public void Hold()
    {
        Interlocked.Increment(ref _refCount);
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            SendBufferPool.Return(this);
        }
    }

    internal void Reset()
    {
        _usedSize = 0;
        _reservedSize = -1;
        _refCount = 1;
        _disposed = 0;
    }
}

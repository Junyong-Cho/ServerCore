using System.Buffers;

namespace ServerCore.Buffers;

public class SendBuffer
{
    byte[] _buffer;

    int _usedSize;
    int _refCount;
    int _disposed;
    
    public int FreeSize => _buffer.Length - _usedSize;

    public SendBuffer(int bufferSize)
    {
        _buffer = new byte[bufferSize];
        _usedSize = 0;
        _refCount = 1;
        _disposed = 0;
    }

    public ArraySegment<byte> Open(int reserveSize)
    {
        return new(_buffer, _usedSize, reserveSize);
    }

    public ArraySegment<byte> Close(int usedSize)
    {
        ArraySegment<byte> segment = new(_buffer, _usedSize, usedSize);

        _usedSize += usedSize;

        return segment;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            DecreaseReference();
    }

    public void IncreaseReference()
    {
        Interlocked.Increment(ref _refCount);
    }

    public void DecreaseReference()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            SendBufferPool.Return(this);
        }
    }

    internal void Reset()
    {
        _usedSize = 0;
        _refCount = 1;
        _disposed = 0;
    }
}

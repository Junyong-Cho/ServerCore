namespace ServerCore.Buffers;

public static class SendBufferHandler
{
    static ThreadLocal<SendBuffer> _current = new(() => SendBufferPool.Rent());

    public static int BufferSize => SendBuffer.BufferSize;

    public static ArraySegment<byte> Open(int reserveSize)
    {
        if (reserveSize < 0)
            throw new ArgumentOutOfRangeException(nameof(reserveSize), "Size must be non-negative.");

        if (reserveSize > BufferSize)
            throw new ArgumentOutOfRangeException(nameof(reserveSize), "Requested size exceeds the buffer capacity.");

        SendBuffer buffer = _current.Value!;

        if (buffer.FreeSize < reserveSize)
        {
            buffer.Dispose();
            _current.Value = buffer = SendBufferPool.Rent();
        }

        return buffer.Open(reserveSize);
    }

    public static SendBufferWrapper Close(int usedSize)
    {
        SendBuffer buffer = _current.Value!;

        return new(buffer, buffer.Close(usedSize));
    }
}

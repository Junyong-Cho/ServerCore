namespace ServerCore.Buffers;

public static class SendBufferHandler
{
    static ThreadLocal<SendBuffer?> _current = new(() => SendBufferPool.Rent(BufferSize));

    public static int BufferSize { get; set; } = 1 << 16;

    public static ArraySegment<byte> Open(int reserveSize)
    {
        SendBuffer buffer = _current.Value!;

        if (buffer.FreeSize < reserveSize)
        {
            if (BufferSize < reserveSize)
                throw new Exception("ReserveSize Over Than BufferSize");

            buffer.Dispose();
            _current.Value = buffer = SendBufferPool.Rent(BufferSize);
        }

        return buffer.Open(reserveSize);
    }

    public static SendBufferWrapper Close(int usedSize)
    {
        SendBuffer buffer = _current.Value!;

        if (buffer.FreeSize < usedSize)
            throw new Exception("Need Open Before Close");

        return new(buffer, buffer.Close(usedSize));
    }
}

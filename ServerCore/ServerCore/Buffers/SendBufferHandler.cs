namespace ServerCore.Buffers;

public static class SendBufferHandler
{
    static ThreadLocal<SendBuffer?> _current = new(() => null);

    public static int BufferSize { get; set; } = 1 << 16;
    public static ArraySegment<byte> Open(int reserveSize)
    {
        if (_current.Value == null)
            _current.Value = new(BufferSize);

        var segment = _current.Value.Open(reserveSize);

        if (segment == null)
        {
            if (BufferSize < reserveSize)
                throw new Exception("SendBuffer Open BufferOverFlow");
            _current.Value = new(BufferSize);

            segment = _current.Value.Open(reserveSize);
        }

        return segment!.Value;
    }

    public static ArraySegment<byte> Close(int usedSize)
    {
        if (_current.Value == null)
            throw new Exception("Try Close Null Buffer Exception");

        var segment = _current.Value.Close(usedSize);

        if (segment == null)
            throw new Exception("UnUsed Segment Close Exception");
        return segment.Value;
    }
}

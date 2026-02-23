internal class SendBuffer
{
    byte[] _buffer;
    int _usedSize = 0;

    public int FreeSize => _buffer!.Length - _usedSize;

    public SendBuffer(int chunkSize = 1 << 16)
    {
        _buffer = new byte[chunkSize];
    }

    public ArraySegment<byte>? Open(int reserveSize)
    {
        if (FreeSize < reserveSize)
            return null;

        return new(_buffer, _usedSize, reserveSize);
    }

    public ArraySegment<byte>? Close(int usedSize)
    {
        if (FreeSize < usedSize)
            return null;

        var segment = Open(usedSize);
        _usedSize += usedSize;

        return segment;
    }
}
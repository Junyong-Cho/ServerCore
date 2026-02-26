namespace ServerCore.Buffers;

internal class SendBuffer
{
    byte[] _buffer;

    int _usedSize;

    public int FreeSize => _buffer.Length - _usedSize;

    public SendBuffer(int bufferSize)
    {
        _buffer = new byte[bufferSize];
        _usedSize = 0;
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

        ArraySegment<byte> segment = new(_buffer, _usedSize, usedSize);

        _usedSize += usedSize;

        return segment;
    }
}

namespace ServerCore.Buffers;

public class RecvBuffer
{
    byte[] _buffer;
    int _readPos;
    int _writePos;

    public int DataSize => _writePos - _readPos;
    public int FreeSize => _buffer.Length - _writePos;

    public ArraySegment<byte> ReadSegment => new(_buffer, _readPos, DataSize);
    public ArraySegment<byte> WriteSegment => new(_buffer, _writePos, FreeSize);

    public RecvBuffer(int bufferSize)
    {
        _buffer = new byte[bufferSize];

        _readPos = _writePos = 0;
    }

    public bool OnRead(int readSize)
    {
        if (readSize < 0)
            throw new ArgumentOutOfRangeException(nameof(readSize), "Size must be non-negative");

        if (DataSize < readSize)
            return false;

        _readPos += readSize;
        return true;
    }

    public bool OnWrite(int writeSize)
    {
        if (writeSize < 0)
            throw new ArgumentOutOfRangeException(nameof(writeSize), "Size must be non-negative");

        if (FreeSize < writeSize)
            return false;

        _writePos += writeSize;
        return true;
    }

    public void Clean()
    {
        int dataSize = DataSize;

        if (dataSize == 0)
        {
            _readPos = _writePos = 0;
        }
        else
        {
            _buffer.AsSpan(_readPos, dataSize).CopyTo(_buffer.AsSpan());

            _readPos = 0;
            _writePos = dataSize;
        }
    }

    public void Reset()
    {
        _readPos = _writePos = 0;
    }
}

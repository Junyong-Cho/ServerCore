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
        if (DataSize < readSize)
            return false;

        _readPos += readSize;
        return true;
    }

    public bool OnWrite(int writeSize)
    {
        if (FreeSize < writeSize)
            return false;

        _writePos += writeSize;
        return true;
    }

    public void Clean()
    {
        int dataSize = DataSize;

        Buffer.BlockCopy(_buffer, _readPos, _buffer, 0, dataSize);

        _readPos = 0;
        _writePos = dataSize;
    }

    public void Reset()
    {
        _readPos = _writePos = 0;
    }
}

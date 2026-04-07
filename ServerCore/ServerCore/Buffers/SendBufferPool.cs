using System.Collections.Concurrent;

namespace ServerCore.Buffers;

internal static class SendBufferPool
{
    static internal ConcurrentStack<SendBuffer> _bufferPool = new();
    
    internal static SendBuffer Rent(int bufferSize)
    {
        if (_bufferPool.TryPop(out SendBuffer? buffer) == true)
        {
            buffer.Reset();
            return buffer;
        }

        return new(bufferSize);
    }

    internal static void Return(SendBuffer buffer)
    {
        _bufferPool.Push(buffer);
    }
}

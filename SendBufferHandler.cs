public static class SendBufferHandler
{
    static ThreadLocal<SendBuffer> CurrentBuffer = new();
    
    public static ArraySegment<byte> Open(int reserveSize)
    {
        if (CurrentBuffer.Value == null)
            CurrentBuffer.Value = new();

        var segment = CurrentBuffer.Value.Open(reserveSize);

        if (segment == null)
        {
            CurrentBuffer.Value = new();
            segment = CurrentBuffer.Value.Open(reserveSize);    
        }

        return segment!.Value;
    }

    public static ArraySegment<byte> Close(int usedSize)
    {
        return CurrentBuffer.Value!.Close(usedSize)!.Value;
    }
}

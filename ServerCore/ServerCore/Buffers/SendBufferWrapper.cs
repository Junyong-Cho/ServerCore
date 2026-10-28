namespace ServerCore.Buffers
{
    public struct SendBufferWrapper
    {
        internal SendBufferWrapper(SendBuffer buffer, ArraySegment<byte> segment)
        {
            Buffer = buffer;
            Segment = segment;
        }

        internal SendBuffer Buffer;
        public ArraySegment<byte> Segment;
    }
}

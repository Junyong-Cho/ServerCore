namespace ServerCore.Sessions;

abstract class PacketReadSession : Session
{
    protected ArraySegment<byte>? SlicePacket(ref ArraySegment<byte> segment)
    {
        if (segment.Count < 2)
            return null;

        ushort size = BitConverter.ToUInt16(segment.Slice(0, 2));

        if (segment.Count < size)
            return null;

        segment = segment.Slice(size);

        return segment.Slice(0, size);
    }
}

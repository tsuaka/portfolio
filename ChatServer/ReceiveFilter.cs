using SuperSocket.Common;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine.Protocol;

namespace ChatServer
{
    public class EFBinaryRequestInfo : BinaryRequestInfo
    {
        public Int16 Size { get; private set; }
        public Int16 PacketID { get; private set; }

        public EFBinaryRequestInfo(Int16 size, Int16 packetID, byte[] body)
            : base(null, body)
        {
            this.Size = size;
            this.PacketID = packetID;
        }
    }

    public class ReceiveFilter : FixedHeaderReceiveFilter<EFBinaryRequestInfo>
    {
        public ReceiveFilter() : base(ServerCommon.PacketDef.PACKET_HEADER_SIZE)
        {
        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            if( !BitConverter.IsLittleEndian )
            {
                Array.Reverse(header, offset, ServerCommon.PacketDef.PACKET_HEADER_SIZE);
            }

            var packetSize = BitConverter.ToInt16(header, offset);
            var bodySize = packetSize - ServerCommon.PacketDef.PACKET_HEADER_SIZE;
            return bodySize;
        }

        protected override EFBinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            if( !BitConverter.IsLittleEndian )
                Array.Reverse(header.Array, 0, ServerCommon.PacketDef.PACKET_HEADER_SIZE);

            return new EFBinaryRequestInfo(BitConverter.ToInt16(header.Array, 0),
                BitConverter.ToInt16(header.Array, 2),
                bodyBuffer.CloneRange(offset, length));
        }
    }
}

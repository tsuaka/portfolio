using MessagePack;
using ServerCommon;

namespace ChatServer
{
    public class ServerPacketData
    {
        public Int16 PacketSize;
        public string SessionID = string.Empty;
        public Int16 PacketID;
        public byte[] BodyData = null!;

        public void Assign(string sessionID, Int16 packetID, byte[] packetBodyData)
        {
            SessionID = sessionID;
            PacketID = packetID;

            if( packetBodyData.Length > 0 )
            {
                BodyData = packetBodyData;
            }
        }

        public static ServerPacketData MakeNTFInConnectOrDisConnectClientPacket(bool isConnect, string sessionID)
        {
            var packet = new ServerPacketData();

            if( isConnect )
            {
                packet.PacketID = (Int32)PACKETID.NTF_IN_CONNECT_CLIENT;
            }
            else
            {
                packet.PacketID = (Int32)PACKETID.NTF_IN_DISCONNECT_CLIENT;
            }

            packet.SessionID = sessionID;
            return packet;
        }

    }

    [MessagePackObject]
    public class PKTInternalReqRoomEnter
    {
        [Key(0)]
        public int RoomNumber { get; set; }

        [Key(1)]
        public string UserID { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class PKTInternalResRoomEnter
    {
        [Key(0)]
        public ERROR_CODE Result { get; set; }

        [Key(1)]
        public int RoomNumber { get; set; }

        [Key(2)]
        public string UserID { get; set; } = string.Empty;
    }


    [MessagePackObject]
    public class PKTInternalNtfRoomLeave
    {
        [Key(0)]
        public int RoomNumber { get; set; }

        [Key(1)]
        public string UserID { get; set; } = string.Empty;
    }

}

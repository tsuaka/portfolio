using System;
using System.Collections.Generic;
using MessagePack;

namespace ServerCommon
{
    public class PacketDef
    {
        public const Int16 PACKET_HEADER_SIZE = 4;
        public const int MAX_USER_ID_BYTE_LENGTH = 16;
        public const int MAX_USER_PW_BYTE_LENGTH = 16;

        public const int INVALID_ROOM_NUMBER = -1;
    }

    public class PacketToBytes
    {
        public static byte[] Make(PACKETID packetID, byte[] bodyData)
        {
            Int16 bodyDataSize = 0;
            if( bodyData != null )
            {
                bodyDataSize = (Int16)bodyData.Length;
            }
            var packetSize = (Int16)( bodyDataSize + PacketDef.PACKET_HEADER_SIZE );

            var dataSource = new byte[packetSize];
            Buffer.BlockCopy(BitConverter.GetBytes(packetSize), 0, dataSource, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((Int16)packetID), 0, dataSource, 2, 2);

            if( bodyData != null )
            {
                Buffer.BlockCopy(bodyData, 0, dataSource, PacketDef.PACKET_HEADER_SIZE, bodyDataSize);
            }

            return dataSource;
        }

        public static Tuple<int, byte[]> ClientReceiveData(int recvLength, byte[] recvData)
        {
            var packetSize = BitConverter.ToInt16(recvData, 0);
            var packetID = BitConverter.ToInt16(recvData, 2);
            var bodySize = packetSize - PacketDef.PACKET_HEADER_SIZE;

            var packetBody = new byte[bodySize];
            Buffer.BlockCopy(recvData, PacketDef.PACKET_HEADER_SIZE, packetBody, 0, bodySize);

            return new Tuple<int, byte[]>(packetID, packetBody);
        }
    }

    // 로그인 요청
    [MessagePackObject]
    public class PKTReqLogin
    {
        [Key(0)]
        public string UserID { get; set; } = string.Empty;
        [Key(1)]
        public string AuthToken { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class PKTResLogin
    {
        [Key(0)]
        public short Result { get; set; }
    }


    [MessagePackObject]
    public class PKNtfMustClose
    {
        [Key(0)]
        public short Result { get; set; }
    }

    [MessagePackObject]
    public class PKTReqRoomEnter
    {
        [Key(0)]
        public int RoomNumber { get; set; }
    }

    [MessagePackObject]
    public class PKTResRoomEnter
    {
        [Key(0)]
        public short Result { get; set; }
    }

    [MessagePackObject]
    public class PKTNtfRoomUserList
    {
        [Key(0)]
        public List<string> UserIDList { get; set; } = new List<string>();
    }

    [MessagePackObject]
    public class PKTNtfRoomNewUser
    {
        [Key(0)]
        public string UserID { get; set; }
    }


    [MessagePackObject]
    public class PKTReqRoomLeave
    {
    }

    [MessagePackObject]
    public class PKTResRoomLeave
    {
        [Key(0)]
        public short Result { get; set; }
    }

    [MessagePackObject]
    public class PKTNtfRoomLeaveUser
    {
        [Key(0)]
        public string UserID { get; set; }
    }


    [MessagePackObject]
    public class PKTReqRoomChat
    {
        [Key(0)]
        public string ChatMessage { get; set; }
    }


    [MessagePackObject]
    public class PKTNtfRoomChat
    {
        [Key(0)]
        public string UserID { get; set; } = string.Empty;

        [Key(1)]
        public string ChatMessage { get; set; } = string.Empty;
    }
}

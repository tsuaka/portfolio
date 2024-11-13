using MessagePack;
using ServerCommon;

namespace ChatServer
{
    public class Room
    {
        public int Index { get; private set; }
        public int Number { get; private set; }

        private int _maxUserCount = 0;

        private List<RoomUser> _userList = new ();

        public static Func<string, byte[], bool> NetSendFunc = null!;


        public void Init(int index, int number, int maxUserCount)
        {
            Index = index;
            Number = number;
            _maxUserCount = maxUserCount;
        }

        public bool AddUser(string userID, string netSessionID)
        {
            if( GetUser(userID) != null )
            {
                return false;
            }

            if( _userList.Count >= _maxUserCount )
            {
                MainServer.MainLogger.Debug($"{nameof(AddUser)}. Room User Count Max. UserCount : {_userList.Count}. MaxUserCount : {_maxUserCount}");
                return false;
            }

            var roomUser = new RoomUser();
            roomUser.Set(userID, netSessionID);
            _userList.Add(roomUser);

            return true;
        }

        public void RemoveUser(string netSessionID)
        {
            var index = _userList.FindIndex(x => x.NetSessionID == netSessionID);
            _userList.RemoveAt(index);
        }

        public bool RemoveUser(RoomUser user)
        {
            return _userList.Remove(user);
        }

        public RoomUser? GetUser(string userID)
        {
            return _userList.Find(x => x.UserID == userID);
        }

        public RoomUser? GetUserByNetSessionId(string netSessionID)
        {
            return _userList.Find(x => x.NetSessionID == netSessionID);
        }

        public int CurrentUserCount()
        {
            return _userList.Count();
        }

        public void NotifyPacketUserList(string userNetSessionID)
        {
            var packet = new PKTNtfRoomUserList();
            foreach( var user in _userList )
            {
                packet.UserIDList.Add(user.UserID);
            }

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_USER_LIST, bodyData);

            NetSendFunc(userNetSessionID, sendPacket);
        }

        public void NofifyPacketNewUser(string newUserNetSessionID, string newUserID)
        {
            var packet = new PKTNtfRoomNewUser();
            packet.UserID = newUserID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_NEW_USER, bodyData);

            Broadcast(newUserNetSessionID, sendPacket);
        }

        public void NotifyPacketLeaveUser(string userID)
        {
            if( CurrentUserCount() == 0 )
            {
                return;
            }

            var packet = new PKTNtfRoomLeaveUser();
            packet.UserID = userID;

            var bodyData = MessagePackSerializer.Serialize(packet);
            var sendPacket = PacketToBytes.Make(PACKETID.NTF_ROOM_LEAVE_USER, bodyData);

            Broadcast("", sendPacket);
        }

        public void Broadcast(string excludeNetSessionID, byte[] sendPacket)
        {
            foreach( var user in _userList )
            {
                if( user.NetSessionID == excludeNetSessionID )
                {
                    continue;
                }

                NetSendFunc(user.NetSessionID, sendPacket);
            }
        }
    }


    public class RoomUser
    {
        public string UserID { get; private set; } = string.Empty;
        public string NetSessionID { get; private set; } = string.Empty;

        public void Set(string userID, string netSessionID)
        {
            UserID = userID;
            NetSessionID = netSessionID;
        }
    }
}

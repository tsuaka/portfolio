using MessagePack;

using ServerCommon;


namespace ChatServer
{
    public class PKHRoom : PKHandler
    {
        private List<Room> _roomList = new();
        private int _startRoomNumber;

        public void SetRooomList(List<Room> roomList)
        {
            _roomList = roomList;
            _startRoomNumber = roomList[0].Number;
        }

        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
        {
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_ENTER, RequestRoomEnter);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_LEAVE, RequestLeave);
            packetHandlerMap.Add((int)PACKETID.NTF_IN_ROOM_LEAVE, NotifyLeaveInternal);
            packetHandlerMap.Add((int)PACKETID.REQ_ROOM_CHAT, RequestChat);
        }


        Room? GetRoom(int roomNumber)
        {
            var index = roomNumber - _startRoomNumber;

            if( index < 0 || index >= _roomList.Count() )
            {
                return null;
            }

            return _roomList[index];
        }

        (bool, Room?, RoomUser?) CheckRoomAndRoomUser(string userNetSessionID)
        {
            var user = UserMgr.GetUser(userNetSessionID);
            if( user == null )
            {
                MainServer.MainLogger.Debug($"{nameof(CheckRoomAndRoomUser)}: User Is Null. UserNetSessionID : {userNetSessionID}");
                return (false, null, null);
            }

            var roomNumber = user.RoomNumber;
            var room = GetRoom(roomNumber);

            if( room == null )
            {
                MainServer.MainLogger.Debug($"{nameof(CheckRoomAndRoomUser)}: Room Is Null. RoomNumber : {roomNumber}");
                return (false, null, null);
            }

            var roomUser = room.GetUserByNetSessionId(userNetSessionID);

            if( roomUser == null )
            {
                MainServer.MainLogger.Debug($"{nameof(CheckRoomAndRoomUser)}: roomUser Is Null. UserNetSessionID : {userNetSessionID}");
                return (false, room, null);
            }

            return (true, room, roomUser);
        }

        public void RequestRoomEnter(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            MainServer.MainLogger.Debug("RequestRoomEnter");

            try
            {
                var user = UserMgr.GetUser(sessionID);

                if( user == null || user.IsConfirm(sessionID) == false )
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                    return;
                }

                if( user.IsAlreadyEnterRoom() )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestRoomEnter)}: User Already Enter Room. RoomNumber : {user.GetRoomNumber()}");
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID);
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomEnter>(packetData.BodyData);

                var room = GetRoom(reqData.RoomNumber);

                if( room == null )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestRoomEnter)}: Room Is Null. RoomNumber : {reqData.RoomNumber}");
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID);
                    return;
                }

                if( room.AddUser(user.GetUserID(), sessionID) == false )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestRoomEnter)}: userAddUser Fail. UserID : {user.GetUserID()} SessionID : {sessionID}");
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID);
                    return;
                }


                user.EnteredRoom(reqData.RoomNumber);

                room.NotifyPacketUserList(sessionID);
                room.NofifyPacketNewUser(sessionID, user.GetUserID());

                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID);

                MainServer.MainLogger.Debug("RequestEnterInternal - Success");
            }
            catch( Exception ex )
            {
                MainServer.MainLogger.Error(ex.ToString());
            }
        }

        void ResponseEnterRoomToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resRoomEnter = new PKTResRoomEnter()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_ENTER, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void RequestLeave(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            MainServer.MainLogger.Debug("로그인 요청 받음");

            try
            {
                var user = UserMgr.GetUser(sessionID);
                if( user == null )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestLeave)}: User Is Null. SessionID : {sessionID}");
                    return;
                }

                if( LeaveRoomUser(sessionID, user.RoomNumber) == false )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestLeave)}: LeaveRoomUser Fail. RoomNumBer : {user.RoomNumber} SessionID : {sessionID}");
                    return;
                }

                user.LeaveRoom();

                ResponseLeaveRoomToClient(sessionID);

                MainServer.MainLogger.Debug("Room RequestLeave - Success");
            }
            catch( Exception ex )
            {
                MainServer.MainLogger.Error(ex.ToString());
            }
        }

        bool LeaveRoomUser(string sessionID, int roomNumber)
        {
            MainServer.MainLogger.Debug($"{nameof(LeaveRoomUser)}. SessionID:{sessionID}");

            var room = GetRoom(roomNumber);
            if( room == null )
            {
                MainServer.MainLogger.Debug($"{nameof(LeaveRoomUser)}. Room Is Null. RoomNumber: {roomNumber} SessionID:{sessionID}");
                return false;
            }

            var roomUser = room.GetUserByNetSessionId(sessionID);
            if( roomUser == null )
            {
                MainServer.MainLogger.Debug($"{nameof(LeaveRoomUser)}. RoomUser Is Null. SessionID:{sessionID}");
                return false;
            }

            var userID = roomUser.UserID;
            room.RemoveUser(roomUser);

            room.NotifyPacketLeaveUser(userID);
            return true;
        }

        void ResponseLeaveRoomToClient(string sessionID)
        {
            var resRoomLeave = new PKTResRoomLeave()
            {
                Result = (short)ERROR_CODE.NONE
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomLeave);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_LEAVE, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void NotifyLeaveInternal(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            MainServer.MainLogger.Debug($"NotifyLeaveInternal. SessionID: {sessionID}");

            var reqData = MessagePackSerializer.Deserialize<PKTInternalNtfRoomLeave>(packetData.BodyData);
            if( reqData == null )
            {
                MainServer.MainLogger.Debug($"{nameof(NotifyLeaveInternal)}: reqData Is Null. SessionID : {sessionID}");
                return;
            }

            LeaveRoomUser(sessionID, reqData.RoomNumber);
        }

        public void RequestChat(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            MainServer.MainLogger.Debug("Room RequestChat");

            try
            {
                var ( isSuccess, room, roomUser ) = CheckRoomAndRoomUser(sessionID);

                if( isSuccess == false )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestChat)}. CheckRoomAndRoomUser isSuccess : false. SessionID : {sessionID}");
                    return;
                }

                if( room == null )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestChat)}. CheckRoomAndRoomUser Room is Null. SessionID : {sessionID}");
                    return;
                }

                if( roomUser == null )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestChat)}. CheckRoomAndRoomUser RoomUser is Null. SessionID : {sessionID}");
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomChat>(packetData.BodyData);

                if( reqData == null )
                {
                    MainServer.MainLogger.Debug($"{nameof(RequestChat)}: reqData Is Null. SessionID : {sessionID}");
                    return;
                }

                var notifyPacket = new PKTNtfRoomChat()
                {
                    UserID = roomUser.UserID,
                    ChatMessage = reqData.ChatMessage
                };

                var Body = MessagePackSerializer.Serialize(notifyPacket);
                var sendData = PacketToBytes.Make(PACKETID.NTF_ROOM_CHAT, Body);

                room.Broadcast("", sendData);

                MainServer.MainLogger.Debug("Room RequestChat - Success");
            }
            catch( Exception ex )
            {
                MainServer.MainLogger.Error(ex.ToString());
            }
        }
    }
}

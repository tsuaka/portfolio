using ServerCommon;

namespace ChatServer
{
    public class UserManager
    {
        private int _maxUserCount;
        private UInt64 _userSequenceNumber = 0;

        private readonly Dictionary<string, User> _userMap = new ();
        private readonly List<string> _userIDList = new();

        public void Init(int maxUserCount)
        {
            _maxUserCount = maxUserCount;
        }

        public ERROR_CODE AddUser(string userID, string sessionID)
        {
            if( IsFullUserCount() )
            {
                return ERROR_CODE.LOGIN_FULL_USER_COUNT;
            }

            if( _userMap.ContainsKey(sessionID) )
            {
                return ERROR_CODE.ADD_USER_DUPLICATE_SESSION;
            }

            if( !CheckUserID(userID) )
            {
                return ERROR_CODE.ADD_USER_DUPLICATE_USERID;
            }


            ++_userSequenceNumber;

            var user = new User();
            user.Set(_userSequenceNumber, sessionID, userID);
            _userMap.Add(sessionID, user);
            _userIDList.Add(userID);

            return ERROR_CODE.NONE;
        }

        public ERROR_CODE RemoveUser(string sessionID, string userID)
        {
            if( _userMap.Remove(sessionID) == false )
            {
                return ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID;
            }

            _userIDList.Remove(userID);

            return ERROR_CODE.NONE;
        }

        public User? GetUser(string sessionID)
        {
            _userMap.TryGetValue(sessionID, out var user);
            return user;
        }

        public bool CheckUserID(string userID)
        {
            if( _userIDList.Contains(userID) )
            {
                return false;
            }

            return true;
        }

        public bool IsFullUserCount()
        {
            return _maxUserCount <= _userMap.Count;
        }

    }

    public class User
    {
        private UInt64 SequenceNumber = 0;
        private string SessionID = string.Empty;

        public int RoomNumber { get; private set; } = -1;
        private string UserID = string.Empty;

        public void Set(UInt64 sequence, string sessionID, string userID)
        {
            SequenceNumber = sequence;
            SessionID = sessionID;
            UserID = userID;
        }

        public bool IsConfirm(string netSessionID)
        {
            return SessionID == netSessionID;
        }

        public string GetUserID()
        {
            return UserID;
        }

        public void EnteredRoom(int roomNumber)
        {
            RoomNumber = roomNumber;
        }

        public void LeaveRoom()
        {
            RoomNumber = -1;
        }

        public bool IsStateLogin()
        {
            return SequenceNumber != 0;
        }

        public bool IsAlreadyEnterRoom()
        {
            return RoomNumber != -1;
        }

        public int GetRoomNumber()
        {
            return RoomNumber;
        }
    }

}

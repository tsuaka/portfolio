using ServerCommon;

namespace ChatServer
{
    public class UserManager
    {
        private int MaxUserCount;
        private UInt64 UserSequenceNumber = 0;

        private readonly Dictionary<string, User> UserMap = new ();

        public void Init(int maxUserCount)
        {
            MaxUserCount = maxUserCount;
        }

        public ERROR_CODE AddUser(string userID, string sessionID)
        {
            if( IsFullUserCount() )
            {
                return ERROR_CODE.LOGIN_FULL_USER_COUNT;
            }

            if( UserMap.ContainsKey(sessionID) )
            {
                return ERROR_CODE.ADD_USER_DUPLICATION;
            }


            ++UserSequenceNumber;

            var user = new User();
            user.Set(UserSequenceNumber, sessionID, userID);
            UserMap.Add(sessionID, user);

            return ERROR_CODE.NONE;
        }

        public ERROR_CODE RemoveUser(string sessionID)
        {
            if( UserMap.Remove(sessionID) == false )
            {
                return ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID;
            }

            return ERROR_CODE.NONE;
        }

        public User? GetUser(string sessionID)
        {
            UserMap.TryGetValue(sessionID, out var user);
            return user;
        }

        bool IsFullUserCount()
        {
            return MaxUserCount <= UserMap.Count;
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

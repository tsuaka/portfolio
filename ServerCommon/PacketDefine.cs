namespace ServerCommon
{
    public enum ERROR_CODE : short
    {
        NONE = 0, // 에러가 아니다
        // 로그인 
        ADD_USER_DUPLICATE_SESSION = 1001, 
        ADD_USER_DUPLICATE_USERID = 1002,
        REMOVE_USER_SEARCH_FAILURE_USER_ID = 1003,
        LOGIN_ALREADY_WORKING = 1004,
        LOGIN_FULL_USER_COUNT = 1005,

        ROOM_ENTER_INVALID_STATE = 1006,
        ROOM_ENTER_INVALID_USER = 1007,
        ROOM_ENTER_INVALID_ROOM_NUMBER = 1008,
        ROOM_ENTER_FAIL_ADD_USER = 1009,
    }

    public enum PACKETID : int
    {
        // 클라이언트
        CS_BEGIN = 1001,

        REQ_LOGIN = 1002,
        RES_LOGIN = 1003,
        NTF_MUST_CLOSE = 1004,
        REQ_ROOM_ENTER = 1005,
        RES_ROOM_ENTER = 1006,
        NTF_ROOM_USER_LIST = 1007,
        NTF_ROOM_NEW_USER = 1008,
        REQ_ROOM_LEAVE = 1009,
        RES_ROOM_LEAVE = 1010,
        NTF_ROOM_LEAVE_USER = 1011,

        REQ_ROOM_CHAT = 1012,
        NTF_ROOM_CHAT = 1013,

        CS_END = 1100,


        // 시스템, 서버 - 서버
        SS_START = 8001,

        NTF_IN_CONNECT_CLIENT = 8011,
        NTF_IN_DISCONNECT_CLIENT = 8012,
        NTF_IN_ROOM_LEAVE = 8013,

        SS_END = 8100,
    }



}

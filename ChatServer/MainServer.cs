using ServerCommon;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace ChatServer
{
    public class MainServer : AppServer<ClientSession, EFBinaryRequestInfo>
    {
        public static ChatServerOption ServerOption = null!;
        public static SuperSocket.SocketBase.Logging.ILog MainLogger = null!;

        private SuperSocket.SocketBase.Config.IServerConfig _config = null!;

        private PacketProcessor _mainPacketProcessor = new PacketProcessor();
        private RoomManager _roomMgr = new RoomManager();


        public MainServer()
            : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
        {
            NewSessionConnected += new SessionHandler<ClientSession>(OnConnected);
            SessionClosed += new SessionHandler<ClientSession, CloseReason>(OnClosed);
            NewRequestReceived += new RequestHandler<ClientSession, EFBinaryRequestInfo>(OnPacketReceived);
        }

        public void InitConfig(ChatServerOption option)
        {
            ServerOption = option;

            _config = new SuperSocket.SocketBase.Config.ServerConfig
            {
                Name = option.Name,
                Ip = "Any",
                Port = option.Port,
                Mode = SocketMode.Tcp,
                MaxConnectionNumber = option.MaxConnectionNumber,
                MaxRequestLength = option.MaxRequestLength,
                ReceiveBufferSize = option.ReceiveBufferSize,
                SendBufferSize = option.SendBufferSize
            };
        }

        public void CreateStartServer()
        {
            try
            {
                bool bResult = Setup(new SuperSocket.SocketBase.Config.RootConfig(), _config, logFactory: new NLogLogFactory());

                if( bResult == false )
                {
                    Console.WriteLine("[ERROR] 서버 네트워크 설정 실패");
                    return;
                }
                else
                {
                    MainLogger = base.Logger;
                    MainLogger.Info("서버 초기화 성공");
                }

                CreateComponent();

                Start();

                MainLogger.Info("서버 생성 성공");
            }
            catch( Exception ex )
            {
                Console.WriteLine($"[ERROR] 서버 생성 실패: {ex.ToString()}");
            }
        }


        public void StopServer()
        {
            Stop();

            _mainPacketProcessor.Destory();
        }

        public ERROR_CODE CreateComponent()
        {
            Room.NetSendFunc = this.SendData;
            _roomMgr.CreateRooms();

            _mainPacketProcessor = new PacketProcessor();
            _mainPacketProcessor.CreateAndStart(_roomMgr.GetRoomsList(), this);

            if( MainLogger != null )
            {
                MainLogger.Info("CreateComponent - Success");
            }

            return ERROR_CODE.NONE;
        }

        public bool SendData(string sessionID, byte[] sendData)
        {
            var session = GetSessionByID(sessionID);

            try
            {
                if( session == null )
                {
                    return false;
                }

                session.Send(sendData, 0, sendData.Length);
            }
            catch( Exception ex )
            {
                MainServer.MainLogger.Error($"{ex.ToString()},  {ex.StackTrace}");

                session.SendEndWhenSendingTimeOut();
                session.Close();
            }
            return true;
        }

        public void Distribute(ServerPacketData requestPacket)
        {
            _mainPacketProcessor.InsertPacket(requestPacket);
        }

        void OnConnected(ClientSession session)
        {
            MainLogger.Info(string.Format("세션 번호 {0} 접속", session.SessionID));

            var packet = ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(true, session.SessionID);
            Distribute(packet);
        }

        void OnClosed(ClientSession session, CloseReason reason)
        {
            MainLogger.Info(string.Format("세션 번호 {0} 접속해제: {1}", session.SessionID, reason.ToString()));

            var packet = ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(false, session.SessionID);
            Distribute(packet);
        }

        void OnPacketReceived(ClientSession session, EFBinaryRequestInfo reqInfo)
        {
            MainLogger.Debug(string.Format("세션 번호 {0} 받은 데이터 크기: {1}, ThreadId: {2}", session.SessionID, reqInfo.Body.Length, System.Threading.Thread.CurrentThread.ManagedThreadId));

            var packet = new ServerPacketData();
            packet.SessionID = session.SessionID;
            packet.PacketSize = reqInfo.Size;
            packet.PacketID = reqInfo.PacketID;
            packet.BodyData = reqInfo.Body;

            Distribute(packet);
        }
    }

    public class ClientSession : AppSession<ClientSession, EFBinaryRequestInfo>
    {
    }
}

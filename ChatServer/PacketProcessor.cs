using System.Threading.Tasks.Dataflow;

namespace ChatServer
{
    class PacketProcessor
    {
        bool IsThreadRunning = false;
        public Thread? ProcessThread = null;

        public BufferBlock<ServerPacketData> MsgBuffer = new();

        public UserManager UserMgr = new UserManager();

        public Tuple<int, int> RoomNumberRange = new Tuple<int, int>(-1, -1);
        public List<Room> RoomList = new List<Room>();

        public Dictionary<int, Action<ServerPacketData>> PacketHandlerMap = new Dictionary<int, Action<ServerPacketData>>();
        public PKHCommon CommonPacketHandler = new PKHCommon();
        public PKHRoom RoomPacketHandler = new PKHRoom();

        public void CreateAndStart(List<Room> roomList, MainServer mainServer)
        {
            var maxUserCount = MainServer.ServerOption.RoomMaxCount * MainServer.ServerOption.RoomMaxUserCount;
            UserMgr.Init(maxUserCount);

            RoomList = roomList;
            var minRoomNum = RoomList[0].Number;
            var maxRoomNum = RoomList[0].Number + RoomList.Count() - 1;
            RoomNumberRange = new Tuple<int, int>(minRoomNum, maxRoomNum);

            RegistPacketHandler(mainServer);

            IsThreadRunning = true;
            ProcessThread = new Thread(this.Process);
            ProcessThread.Start();
        }

        public void Destory()
        {
            IsThreadRunning = false;
            MsgBuffer.Complete();
        }

        public void InsertPacket(ServerPacketData data)
        {
            MsgBuffer.Post(data);
        }


        void RegistPacketHandler(MainServer serverNetwork)
        {
            CommonPacketHandler.Init(serverNetwork, UserMgr);
            CommonPacketHandler.RegistPacketHandler(PacketHandlerMap);

            RoomPacketHandler.Init(serverNetwork, UserMgr);
            RoomPacketHandler.SetRooomList(RoomList);
            RoomPacketHandler.RegistPacketHandler(PacketHandlerMap);
        }

        void Process()
        {
            while( IsThreadRunning )
            {
                try
                {
                    var packet = MsgBuffer.Receive();

                    if( PacketHandlerMap.ContainsKey(packet.PacketID) )
                    {
                        PacketHandlerMap[packet.PacketID](packet);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("세션 번호 {0}, PacketID {1}, 받은 데이터 크기: {2}", packet.SessionID, packet.PacketID, packet.BodyData.Length);
                    }
                }
                catch( Exception ex )
                {
                    IsThreadRunning.IfTrue(() => MainServer.MainLogger!.Error(ex.ToString()));
                }
            }
        }


    }
}

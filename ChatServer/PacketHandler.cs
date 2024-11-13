namespace ChatServer
{
    public class PacketHandler
    {
        protected MainServer ServerNetwork = null!;
        protected UserManager UserMgr = null!;

        public void Init(MainServer serverNetwork, UserManager userMgr)
        {
            ServerNetwork = serverNetwork;
            UserMgr = userMgr;
        }

    }
}

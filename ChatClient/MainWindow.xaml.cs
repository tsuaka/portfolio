using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using MessagePack;
using ServerCommon;

namespace ChatClient2
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private CLIENT_STATE _clientState = CLIENT_STATE.NONE;

        private ClientSimpleTcp _network = new ();

        private bool _isNetworkThreadRunning = false;
        private bool _isBackGroundProcessRunning = false;

        private System.Threading.Thread _networkReadThread = null;
        private System.Threading.Thread _networkSendThread = null;

        private PacketBufferManager _packetBuffer = new ();
        private Queue<PacketData> _recvPacketQueue = new ();
        private Queue<byte[]> _sendPacketQueue = new ();

        private DispatcherTimer _dispatcherUITimer = new ();

        enum CLIENT_STATE
        {
            NONE = 0,
            CONNECTED = 1,
            LOGIN = 2,
            ROOM = 3
        }

        struct PacketData
        {
            public Int16 DataSize;
            public Int16 PacketID;
            public byte[] BodyData;
        }

        public MainWindow()
        {
            InitializeComponent();

            _packetBuffer.Init(( 8096 * 10 ), PacketDef.PACKET_HEADER_SIZE, 1024);

            _isNetworkThreadRunning = true;
            _networkReadThread = new System.Threading.Thread(this.NetworkReadProcess);
            _networkReadThread.Start();
            _networkSendThread = new System.Threading.Thread(this.NetworkSendProcess);
            _networkSendThread.Start();

            _isBackGroundProcessRunning = true;
            _dispatcherUITimer.Tick += new EventHandler(BackGroundProcess);
            _dispatcherUITimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _dispatcherUITimer.Start();
        }

        void BackGroundProcess(object sender, EventArgs e)
        {
            ProcessLog();

            try
            {
                var packet = new PacketData();

                lock( ( (System.Collections.ICollection)_recvPacketQueue ).SyncRoot )
                {
                    if( _recvPacketQueue.Count() > 0 )
                    {
                        packet = _recvPacketQueue.Dequeue();
                    }
                }

                if( packet.PacketID != 0 )
                {
                    PacketProcess(packet);
                }
            }
            catch( Exception ex )
            {
                MessageBox.Show(string.Format("ReadPacketQueueProcess. error:{0}", ex.Message));
            }
        }

        private void ProcessLog()
        {
            int logWorkCount = 0;

            while( _isBackGroundProcessRunning )
            {
                System.Threading.Thread.Sleep(1);

                string msg;

                if( DevLog.GetLog(out msg) )
                {
                    ++logWorkCount;

                    if( listBoxLog.Items.Count > 512 )
                    {
                        listBoxLog.Items.Clear();
                    }

                    listBoxLog.Items.Add(msg);
                    var lastIndex = listBoxLog.Items.Count - 1;
                    listBoxLog.ScrollIntoView(listBoxLog.Items[lastIndex]);
                }
                else
                {
                    break;
                }

                if( logWorkCount > 8 )
                {
                    break;
                }
            }
        }

        void NetworkReadProcess()
        {
            const Int16 PacketHeaderSize = PacketDef.PACKET_HEADER_SIZE;

            while( _isNetworkThreadRunning )
            {
                if( _network.IsConnected() == false )
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                var recvData = _network.Receive();

                if( recvData != null )
                {
                    _packetBuffer.Write(recvData.Item2, 0, recvData.Item1);

                    while( true )
                    {
                        var data = _packetBuffer.Read();
                        if( data.Count < 1 )
                        {
                            break;
                        }

                        var packet = new PacketData();
                        packet.DataSize = (short)( data.Count - PacketHeaderSize );
                        packet.PacketID = BitConverter.ToInt16(data.Array, data.Offset + 2);
                        packet.BodyData = new byte[packet.DataSize];
                        Buffer.BlockCopy(data.Array, ( data.Offset + PacketHeaderSize ), packet.BodyData, 0, ( data.Count - PacketHeaderSize ));
                        lock( ( (System.Collections.ICollection)_recvPacketQueue ).SyncRoot )
                        {
                            _recvPacketQueue.Enqueue(packet);
                        }
                    }
                }
                else
                {
                    _network.Close();
                    SetDisconnectd();
                    DevLog.Write("서버와 접속 종료 !!!", LOG_LEVEL.INFO);
                }
            }
        }

        void NetworkSendProcess()
        {
            while( _isNetworkThreadRunning )
            {
                System.Threading.Thread.Sleep(1);

                if( _network.IsConnected() == false )
                {
                    continue;
                }

                lock( ( (System.Collections.ICollection)_sendPacketQueue ).SyncRoot )
                {
                    if( _sendPacketQueue.Count > 0 )
                    {
                        var packet = _sendPacketQueue.Dequeue();
                        _network.Send(packet);
                    }
                }
            }
        }

        public void SetDisconnectd()
        {
            _clientState = CLIENT_STATE.NONE;

            _sendPacketQueue.Clear();

            Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(delegate {
                    ClearUIRoomOut();
                }));

            Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(delegate
                {
                    labelConnState.Content = string.Format("{0}. 서버 접속이 끊어짐", DateTime.Now);
                }));

            _clientState = CLIENT_STATE.NONE;
        }

        void ClearUIRoomOut()
        {
            listBoxRoomUserList.Items.Clear();
            textBoxRoomNum.Text = "0";
            listBoxChat.Items.Clear();
        }

        void RequestLogin(string userID, string authToken)
        {
            DevLog.Write("서버에 로그인 요청", LOG_LEVEL.INFO);

            var reqLogin = new PKTReqLogin() { UserID = userID, AuthToken = authToken };

            var Body = MessagePackSerializer.Serialize(reqLogin);
            var sendData = PacketToBytes.Make(PACKETID.REQ_LOGIN, Body);
            PostSendPacket(sendData);
        }

        public void PostSendPacket(byte[] sendData)
        {
            if( _network.IsConnected() == false )
            {
                DevLog.Write("서버 연결이 되어 있지 않습니다", LOG_LEVEL.ERROR);
                return;
            }

            _sendPacketQueue.Enqueue(sendData);
        }


        void PacketProcess(PacketData packet)
        {
            switch( (PACKETID)packet.PacketID )
            {
                case PACKETID.RES_LOGIN:
                {
                    var resData = MessagePackSerializer.Deserialize<PKTResLogin>(packet.BodyData);

                    if( resData.Result == (short)ERROR_CODE.NONE )
                    {
                        _clientState = CLIENT_STATE.LOGIN;
                        DevLog.Write("로그인 성공", LOG_LEVEL.INFO);
                    }
                    else
                    {
                        DevLog.Write(string.Format("로그인 실패: {0} {1}", resData.Result, ( (ERROR_CODE)resData.Result ).ToString()), LOG_LEVEL.ERROR);
                    }
                }
                break;

                case PACKETID.RES_ROOM_ENTER:
                {
                    var resData = MessagePackSerializer.Deserialize<PKTResRoomEnter>(packet.BodyData);

                    if( resData.Result == (short)ERROR_CODE.NONE )
                    {
                        _clientState = CLIENT_STATE.ROOM;
                        DevLog.Write("방 입장 성공", LOG_LEVEL.INFO);
                    }
                    else
                    {
                        DevLog.Write(string.Format("방입장 실패: {0} {1}", resData.Result, ( (ERROR_CODE)resData.Result ).ToString()), LOG_LEVEL.INFO);
                    }
                }
                break;
                case PACKETID.NTF_ROOM_USER_LIST:
                {
                    var ntfData = MessagePackSerializer.Deserialize<PKTNtfRoomUserList>(packet.BodyData);

                    foreach( var user in ntfData.UserIDList )
                    {
                        listBoxRoomUserList.Items.Add(user);
                    }
                }
                break;
                case PACKETID.NTF_ROOM_NEW_USER:
                {
                    var ntfData = MessagePackSerializer.Deserialize<PKTNtfRoomNewUser>(packet.BodyData);
                    listBoxRoomUserList.Items.Add(ntfData.UserID);
                }
                break;

                case PACKETID.RES_ROOM_LEAVE:
                {
                    var resData = MessagePackSerializer.Deserialize<PKTResRoomLeave>(packet.BodyData);

                    if( resData.Result == (short)ERROR_CODE.NONE )
                    {
                        listBoxRoomUserList.Items.Remove(textBoxID.Text);
                        _clientState = CLIENT_STATE.LOGIN;
                        ClearUIRoomOut();
                        DevLog.Write("방 나가기 성공", LOG_LEVEL.INFO);
                    }
                    else
                    {
                        DevLog.Write(string.Format("방 나가기 실패: {0} {1}", resData.Result, ( (ERROR_CODE)resData.Result ).ToString()), LOG_LEVEL.ERROR);
                    }
                }
                break;
                case PACKETID.NTF_ROOM_LEAVE_USER:
                {
                    var ntfData = MessagePackSerializer.Deserialize<PKTNtfRoomLeaveUser>(packet.BodyData);
                    listBoxRoomUserList.Items.Remove(ntfData.UserID);
                }
                break;

                case PACKETID.NTF_ROOM_CHAT:
                {
                    textBoxSendChat.Text = "";

                    var ntfData = MessagePackSerializer.Deserialize<PKTNtfRoomChat>(packet.BodyData);
                    listBoxChat.Items.Add($"[{ntfData.UserID}]: {ntfData.ChatMessage}");
                }
                break;
            }
        }



        // 접속하기
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if( _clientState == CLIENT_STATE.CONNECTED )
            {
                DevLog.Write($"이미 서버에 접속중입니다.", LOG_LEVEL.INFO);
                return;
            }

            string address = textBoxIP.Text;

            if( checkBoxLocalHostIP.IsChecked == true )
            {
                address = "127.0.0.1";
            }

            int port = Convert.ToInt32(textBoxPort.Text);

            DevLog.Write($"서버에 접속 시도: ip:{address}, port:{port}", LOG_LEVEL.INFO);

            if( _network.Connect(address, port) )
            {
                labelConnState.Content = string.Format("{0}. 서버에 접속 중", DateTime.Now);
                _clientState = CLIENT_STATE.CONNECTED;
            }
            else
            {
                labelConnState.Content = string.Format("{0}. 서버에 접속 실패", DateTime.Now);
            }
        }

        // 로그인
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if( _clientState == CLIENT_STATE.CONNECTED )
            {
                RequestLogin(textBoxID.Text, textBoxPW.Text);
            }
            else if( _clientState == CLIENT_STATE.LOGIN )
            {
                DevLog.Write($"이미 로그인한 상태입니다.", LOG_LEVEL.INFO);
            }
        }

        // 접속 끊기
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DevLog.Write($"서버 접속 끊기", LOG_LEVEL.INFO);

            _clientState = CLIENT_STATE.NONE;
            SetDisconnectd();
            _network.Close();
        }

        // 방 입장
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if( _clientState == CLIENT_STATE.ROOM )
            {
                DevLog.Write($"이미 방에 입장한 상태 입니다.", LOG_LEVEL.INFO);
                return;
            }

            var roomNum = textBoxRoomNum.Text.ToInt32();

            DevLog.Write("서버에 방 입장 요청", LOG_LEVEL.INFO);

            var request = new PKTReqRoomEnter() { RoomNumber = roomNum };

            var Body = MessagePackSerializer.Serialize(request);
            var sendData = PacketToBytes.Make(PACKETID.REQ_ROOM_ENTER, Body);
            PostSendPacket(sendData);
        }

        // 방 나가기
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            DevLog.Write("서버에 방 나가기 요청", LOG_LEVEL.INFO);

            var sendData = PacketToBytes.Make(PACKETID.REQ_ROOM_LEAVE, null);
            PostSendPacket(sendData);
        }

        // 방 채팅
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var request = new PKTReqRoomChat() { ChatMessage = textBoxSendChat.Text };

            var Body = MessagePackSerializer.Serialize(request);
            var sendData = PacketToBytes.Make(PACKETID.REQ_ROOM_CHAT, Body);
            PostSendPacket(sendData);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _network.Close();

            _isNetworkThreadRunning = false;
            _isBackGroundProcessRunning = false;

            if( _networkReadThread.IsAlive )
            {
                _networkReadThread.Join();
            }

            if( _networkSendThread.IsAlive )
            {
                _networkSendThread.Join();
            }
        }
    }
}

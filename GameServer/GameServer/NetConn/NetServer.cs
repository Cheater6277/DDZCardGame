using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GameServer.NetConn
{
    public class NetServer
    {
        //保存多个客户端的通信套接字
        public static Dictionary<String, Socket> clientList = null;
        //申明一个监听TcpListener
        TcpListener serverListener = null;
        //设置一个监听标记
        Boolean isListen = true;
        //开启监听的线程
        Thread thStartListen;
        //默认一个主机监听的IP 
        IPAddress ipadr;
        //将endpoint设置为成员字段
        IPEndPoint endPoint;

        public delegate void UserLogin(string UserName);
        public delegate void UserLogout(string UserName);
        public delegate void RecievedMessage(string UserName, string msg);

        public event UserLogin UserLoggedIn;
        public event UserLogout UserLoggedOut;
        public event RecievedMessage MessageRecieved;

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>本机IP地址</returns>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public NetServer()
        {
            //ipadr = IPAddress.Parse( GetLocalIP());
            ipadr = IPAddress.Any;
            Log("本机ip：" + ipadr.ToString());
        }

        public static void Log(string a)
        {
            Console.WriteLine(DateTime.Now + " " + a);
        }

        public void StartService()
        {
            if (serverListener == null)
            {
                try
                {
                    isListen = true;
                    clientList = new Dictionary<string, Socket>();

                    //实例监听TcpListener
                    serverListener = new TcpListener(ipadr, 8080); // 使用 TcpListener 替代 Socket

                    try
                    {
                        serverListener.Start(); // 启动监听
                        thStartListen = new Thread(StartListen);
                        thStartListen.IsBackground = true;
                        thStartListen.Start();

                        Log("网络服务启动成功");
                    }
                    catch (Exception eg)
                    {
                        Log("服务启动失败，可能是IP地址有误");
                        if (serverListener != null)
                        {
                            serverListener.Stop();
                            thStartListen.Abort();  //将监听进程关掉

                            BroadCast.PushMessage("Server has closed", "", false, clientList);
                            foreach (var socket in clientList.Values)
                            {
                                socket.Close();
                            }
                            clientList.Clear();

                            serverListener = null;
                            isListen = false;
                        }
                    }

                }
                catch (SocketException ex)
                {
                    Log(ex.ToString());
                }
            }
        }

        //线程函数，封装一个建立连接的通信套接字
        public void StartListen()
        {
            isListen = true;
            //default()只是设置为一个初始值，这里应该为null
            Socket clientSocket = default(Socket);

            while (isListen)
            {
                try
                {
                    //使用 TcpListener.AcceptTcpClient() 来接受连接
                    if (serverListener == null)   //如果服务停止，即serverListener为空了，那就直接返回
                    {
                        return;
                    }
                    TcpClient tcpClient = serverListener.AcceptTcpClient();   // 采用 TcpListener 的 AcceptTcpClient
                    clientSocket = tcpClient.Client;  // 获取通信套接字
                }
                catch (SocketException e)
                {
                    Log(e.ToString() + "StartListen" + DateTime.Now.ToString() + "");
                }

                //TCP是面向字节流的
                Byte[] bytesFrom = new Byte[4096];
                String dataFromClient = null;

                if (clientSocket != null && clientSocket.Connected)
                {
                    try
                    {
                        //Socket.Receive()
                        Int32 len = clientSocket.Receive(bytesFrom);    //获取客户端发来的信息,返回的就是收到的字节数,并且把收到的信息都放在bytesForm里面

                        if (len > -1)
                        {
                            String tmp = Encoding.UTF8.GetString(bytesFrom, 0, len);  //将字节流转换成字符串
                            dataFromClient = tmp;
                            Int32 sublen = dataFromClient.LastIndexOf("$");
                            if (sublen > -1)
                            {
                                dataFromClient = dataFromClient.Substring(0, sublen);   //获取用户名

                                if (!clientList.ContainsKey(dataFromClient))
                                {
                                    clientList.Add(dataFromClient, clientSocket);   //如果用户名不存在，则添加用户名进去

                                    //BroadCast是下面自己定义的一个类，是用来将消息对所有用户进行推送的
                                    UserLoggedIn(dataFromClient);

                                    //HandleClient也是一个自己定义的类，用来负责接收客户端发来的消息并转发给所有的客户端
                                    HandleClient client = new HandleClient();
                                    client.MessageRecieved += RecMsg;
                                    client.UserLoggedOut += ULogout;

                                    client.StartClient(clientSocket, dataFromClient, clientList);

                                    Log(dataFromClient + "连接上了服务器");
                                }
                                else
                                {
                                    //用户名已经存在
                                    clientSocket.Send(Encoding.UTF8.GetBytes("#" + dataFromClient + "#"));
                                }
                            }
                        }
                    }
                    catch (Exception ep)
                    {
                        Log(ep.ToString() + "\t\t" + DateTime.Now.ToString() + "");
                    }
                }
            }
        }

        private void RecMsg(string clno, string msg)
        {
            MessageRecieved(clno, msg);
        }

        private void ULogout(string clno)
        {
            UserLoggedOut(clno);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (serverListener != null)
            {
                serverListener.Stop();
                thStartListen.Abort();  //将监听进程关掉

                BroadCast.PushMessage("Server has closed", "", false, clientList);
                foreach (var socket in clientList.Values)
                {
                    socket.Close();
                }
                clientList.Clear();

                serverListener = null;
                isListen = false;
                Log("服务停止，断开所有客户端连接\t" + DateTime.Now.ToString());
            }
        }

        private void Form1_Load()
        {
            try
            {
                clientList = new Dictionary<string, Socket>();
                serverListener = new TcpListener(ipadr, 8080); //实例化 TcpListener
                serverListener.Start(); // 启动监听
                thStartListen = new Thread(StartListen);
                thStartListen.IsBackground = true;
                thStartListen.Start();
                Log("服务启动成功");
                Log("当前IP：" + ipadr.ToString());
            }
            catch (SocketException ep)
            {
                Log(ep.ToString());
            }
        }

        public void StopService()
        {
            if (serverListener != null)
            {
                BroadCast.PushMessage("Server has closed", "", false, clientList);
                foreach (var socket in clientList.Values)
                {
                    socket.Close();
                }
                clientList.Clear();
                serverListener.Stop();
                serverListener = null;
                isListen = false;
                Log("服务停止");
            }
        }

        //重置监听的IP地址
        private void ResetIP(string txtIP)
        {
            if (!String.IsNullOrWhiteSpace(txtIP.Trim()))
            {
                try
                {
                    ipadr = IPAddress.Parse(txtIP.Trim());
                    StopService();
                    Log("服务器重启中，请稍候...");
                    StartService();
                    Log("当前IP：" + ipadr.ToString());
                }
                catch (Exception ep)
                {
                    Log("ip无效");
                }
            }
            else
            {
                Log("ip无效");
            }
        }

        public void Reset()
        {
            if (ipadr == IPAddress.Loopback)
            {
                Log("当前已经处于默认状态，无需修改");
            }
            else
            {
                ipadr = IPAddress.Loopback;
                StopService();
                Log("服务器重启中，请稍候...");
                StartService();
                Log("当前IP：" + ipadr.ToString());
            }
        }

        public void BroadCastToAll(string msg)
        {
            BroadCast.PushMessage(msg + "*", "", false, clientList);
        }

        public void SendTo(string clno, string msg)
        {
            clientList[clno].Send(Encoding.UTF8.GetBytes(msg + "*"));
        }
    }


    //该类专门负责接收客户端发来的消息，并转发给所有的客户端
    public class HandleClient
    {
        Socket mySocket = null;
        String clid = null;
        Dictionary<string, Socket> clientList = null;
        public delegate void RecievedMessage(string UserName, string msg);
        public delegate void UserLogout(string UserName);
        public event RecievedMessage MessageRecieved;
        public event UserLogout UserLoggedOut;

        public void StartClient(Socket socket, string clientid, Dictionary<string, Socket> clientlist)
        {
            this.mySocket = socket;
            this.clid = clientid;
            this.clientList = clientlist;

            Thread thRecv = new Thread(RecieveMsg);
            thRecv.IsBackground = true;
            thRecv.Start();
        }

        private void RecieveMsg()
        {
            Byte[] bytesFrom = new Byte[4096];
            String dataFromClient = null;

            while (true)
            {
                try
                {
                    Int32 len = mySocket.Receive(bytesFrom);

                    if (len > -1)
                    {
                        String tmp = Encoding.UTF8.GetString(bytesFrom, 0, len);  //将字节流转换成字符串
                        dataFromClient = tmp;

                        Int32 sublen = dataFromClient.LastIndexOf("*");
                        if (sublen > -1)
                        {
                            dataFromClient = dataFromClient.Substring(0, sublen);
                        }

                        if (!String.IsNullOrWhiteSpace(dataFromClient))
                        {
                            MessageRecieved(clid, dataFromClient);
                        }
                    }
                }
                catch (Exception ep)
                {
                    Log("接收消息失败：" + ep.ToString());
                    clientList.Remove(clid);
                    UserLoggedOut(clid);
                    mySocket.Close();
                    break;
                }
            }
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
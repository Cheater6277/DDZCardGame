using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameClient {
    public class NetworkClient {
        private TcpClient tcpClient = null;  // 替换为 TcpClient
        private NetworkStream networkStream = null;  // 用于数据读取
        private static Boolean isListen = true;
        private Thread thDataFromServer;
        private IPAddress ipadr;

        public delegate void MessageRecievedHandler(string msg);
        public delegate void FailureHandler(string msg);
        public event MessageRecievedHandler MessageRecieved;
        public event FailureHandler FailureCaused;

        public void SendMessage(string msg) {
            if (String.IsNullOrWhiteSpace(msg.Trim())) {
                FailureCaused("发送内容不能为空哦~");
                return;
            }
            if (tcpClient != null && tcpClient.Connected) {
                byte[] bytesSend = Encoding.UTF8.GetBytes(msg + "$");
                networkStream.Write(bytesSend, 0, bytesSend.Length);  // 使用 NetworkStream 写数据
            } else {
                FailureCaused("未连接服务器或者服务器已停止，请联系管理员~");
                return;
            }
        }

        // 每一个连接的客户端必须设置一个唯一的用户名，在服务器端是把用户名和套接字保存在Dictionary<userName,ClientSocket>中
        public bool Connect(string UserName, string ip) {
            if (String.IsNullOrWhiteSpace(UserName.Trim())) {
                FailureCaused("请设置个用户名哦亲");
                return false;
            }
            if (UserName.Length >= 17 && UserName.ToString().Trim().Substring(0, 17).Equals("Server has closed")) {
                FailureCaused("该用户名中包含敏感词，请更换用户名后重试");
                return false;
            }

            if (tcpClient == null || !tcpClient.Connected) {
                try {
                    tcpClient = new TcpClient();  // 创建一个 TcpClient 实例
                    // 如果txtIP里面有值，就选择填入的IP作为服务器IP，不填的话就默认是本机的
                    if (!String.IsNullOrWhiteSpace(ip.ToString().Trim())) {
                        try {
                            ipadr = IPAddress.Parse(ip.ToString().Trim());
                        } catch {
                            FailureCaused("请输入正确的IP后重试");
                            return false;
                        }
                    } else {
                        ipadr = IPAddress.Loopback;  // 默认为本机
                    }

                    tcpClient.BeginConnect(ipadr, 8080, (args) => {
                        if (args.IsCompleted)   // 判断该异步操作是否执行完毕
                        {
                            // 获取网络流并传输用户名
                            networkStream = tcpClient.GetStream();
                            byte[] bytesSend = Encoding.UTF8.GetBytes(UserName.Trim() + "$");
                            networkStream.Write(bytesSend, 0, bytesSend.Length); // 使用 NetworkStream 写数据
                            thDataFromServer = new Thread(DataFromServer);
                            thDataFromServer.IsBackground = true;
                            thDataFromServer.Start();
                        }
                    }, null);
                    return true;
                } catch (SocketException ex) {
                    FailureCaused(ex.ToString());
                    return false;
                }
            } else {
                FailureCaused("你已经连接上服务器了");
                return true;
            }
        }

        // 获取服务器端的消息
        private void DataFromServer() {
            MessageRecieved("SLOG|正在连接服务器");
            isListen = true;
            try {
                while (isListen) {
                    byte[] bytesFrom = new byte[4096];
                    int len = networkStream.Read(bytesFrom, 0, bytesFrom.Length);  // 使用 NetworkStream 读取数据

                    string dataFromClient = Encoding.UTF8.GetString(bytesFrom, 0, len);
                    if (!String.IsNullOrWhiteSpace(dataFromClient)) {
                        // 如果收到服务器已经关闭的消息，那么就把客户端接口关了，免得出错，并在客户端界面上显示出来
                        if (dataFromClient.Length >= 17 && dataFromClient.Substring(0, 17).Equals("Server has closed")) {
                            tcpClient.Close();
                            tcpClient = null;

                            MessageRecieved("SLOG|服务器已关闭");
                            MessageRecieved("SMSG|服务器已关闭");
                            thDataFromServer.Abort();   // 这一句必须放在最后，不然这个进程都关了后面的就不会执行了

                            return;
                        }

                        if (dataFromClient.StartsWith("#") && dataFromClient.EndsWith("#")) {
                            string userName = dataFromClient.Substring(1, dataFromClient.Length - 2);
                            FailureCaused("用户名：[" + userName + "]已经存在，请尝试其他用户名并重试");
                            isListen = false;
                            networkStream.Write(Encoding.UTF8.GetBytes("$"), 0, 1);  // 添加正确的偏移量和大小
                            tcpClient.Close();
                            tcpClient = null;
                        } else {
                            //txtName.Enabled = false;    //当用户名唯一时才禁止再次输入用户名
                            string[] vs = dataFromClient.Split('*');
                            foreach (string i in vs) {
                                MessageRecieved(i);
                            }
                        }
                    }
                }
            } catch (SocketException ex) {
                isListen = false;
                if (tcpClient != null && tcpClient.Connected) {
                    // 没有在客户端关闭连接，而是给服务器发送一个消息，在服务器端关闭连接
                    // 这样可以将异常的处理放到服务器。客户端关闭会让客户端和服务器都抛异常
                    networkStream.Write(Encoding.UTF8.GetBytes("$"), 0, 1);  // 添加正确的偏移量和大小
                    FailureCaused(ex.ToString());
                }
            }
        }

        public void Stop() {
            if (tcpClient != null && tcpClient.Connected) {
                thDataFromServer.Abort();
                networkStream.Write(Encoding.UTF8.GetBytes("$"), 0, 1);  // 添加正确的偏移量和大小

                tcpClient.Close();
                tcpClient = null;
                MessageRecieved("SMSG|已断开与服务器的连接");
                MessageRecieved("SLOG|已断开与服务器的连接");
            }
        }
    }
}

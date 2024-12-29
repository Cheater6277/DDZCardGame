using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.NetConn
{
    internal class BroadCast
    {
        public static void PushMessage(string message, string fromUser, bool includeSelf, Dictionary<string, Socket> clientList)
        {
            foreach (var client in clientList)
            {
                try
                {
                    // 仅当包含自己时或者排除自己
                    if (includeSelf || client.Key != fromUser)
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(message);
                        client.Value.Send(msg);
                    }
                }
                catch (Exception ex)
                {
                    // 如果发送消息失败，则移除该客户端
                    Console.WriteLine("Error sending message to client " + client.Key + ": " + ex.Message);
                }
            }
        }
    }
}

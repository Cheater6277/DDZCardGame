using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        public static void Log(string a)
        {
            Console.WriteLine(DateTime.Now + " " + a);
        }

        static GameruleHandler handler;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("DDZ Game Server Copyright (C) 2024 From Duanyll Edited by Lisushang and Wangshanghai");
                Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY; ");
                Console.WriteLine("This is free software, and you are welcome to redistribute it");
                Console.WriteLine("under certain conditions;");

                Log("当前服务器版本：" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                Log("服务器程序已启动");
                handler = new GameruleHandler();
                handler.StartGame();
                Log("按任意键退出服务器");
                Console.ReadKey();
                handler.StopAll();
                return;
            }
            catch (Exception ex)
            {
                Log("程序遭遇了不可恢复的异常，现将退出");
                Log("异常消息如下：");
                Log(ex.Message);
                Console.ReadKey();
                return;
            }
        }
    }
}

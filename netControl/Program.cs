using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using System.Windows;

namespace gui
{
    class Program
    {
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool HandlerRoutine(CtrlTypes ctrlTypes);
        private static HandlerRoutine del = new HandlerRoutine(Quit.QSave);
        private GCHandle handle = GCHandle.Alloc(del);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public static long today;
        public static DateTime time;
        public static int set = -1;

        //public static int bufwid = Console.BufferWidth;
        //public static int bufhei = Console.BufferHeight;
        //public static int winwid = Console.WindowWidth;
        //public static int winhei = Console.WindowHeight;

        private static void Main()
        {
            string[] t = new string[3];
            string mac = "{0}";
            try
            {
                using (var sr = new StreamReader(Convert.ToString(Directory.GetCurrentDirectory() + "\\data.txt"), true))
                {
                    string text;
                    int tset = 0;
                    while ((text = sr.ReadLine()) != null)
                    {
                        t = text.Split(' ');
                        if (int.TryParse(t[0], out tset))
                        {
                            set = tset;
                            mac = t[1];
                        }
                    }
                }
                if (DateTime.Compare(DateTime.Parse(t[0] + " " + t[1]).AddHours(-1).AddSeconds(-1).Date, DateTime.Now.AddHours(-1).Date) != 0)
                {
                    throw new Exception();
                }
                today = Convert.ToInt64(t[2]);
            }
            catch
            {
                today = 0;
            }
            long bef = today;
            NetworkInterface networkInterface;
            while (true)
            {
                NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                if (set == -1 || allNetworkInterfaces[set].Id != mac)
                {
                    Console.SetWindowSize(70, allNetworkInterfaces.Length + 2);
                    Console.SetBufferSize(70, allNetworkInterfaces.Length + 2);
                    int tmp;
                    do {
                        Console.SetCursorPosition(0, 0);
                        Console.Clear();
                        int len = allNetworkInterfaces.Length;
                        while (len-- != 0)
                        {
                            Console.WriteLine("{0}: {1}", allNetworkInterfaces.Length-len, allNetworkInterfaces[allNetworkInterfaces.Length-len-1].Name);
                        }
                        Console.WriteLine("アダプタを選択してください。(1～{0}, アダプタの再読込:0)", allNetworkInterfaces.Length);
                        tmp = Convert.ToInt16(Console.ReadLine()) - 1;
                    } while (tmp < -1 || tmp > allNetworkInterfaces.Length - 1);
                    if (tmp == -1) continue;

                    set = tmp;
                    mac = allNetworkInterfaces[set].Id;

                    using (var sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\data.txt", true))
                    {
                        sw.WriteLine("{0} {1}", Convert.ToString(set), mac);
                    }
                }
                networkInterface = NetworkInterface.GetAllNetworkInterfaces()[set];
                if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    break;
                }
                else
                {
                    set = -1;
                }
            }

            SetConsoleCtrlHandler(del, true);
            Console.SetWindowSize(80, 1);
            Console.SetBufferSize(80, 1);
            Console.CursorVisible = false;
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.Clear();
                long ras = networkInterface.GetIPv4Statistics().BytesReceived + networkInterface.GetIPv4Statistics().BytesSent; //read and send

                time = DateTime.Now;

                if (time.TimeOfDay.ToString().Split('.')[0] == "01:00:00")  //日付変更
                {
                    Quit.Save();
                    today = 0;
                    bef = 0;
                }

                if (ras == 0)
                {
                    Console.Write("通信していない、またはネットワークが切断されています。");
                    bef = 0;
                }
                else
                {
                    long dif;
                    if (bef != today)
                    {
                        dif = ras - bef;
                    }
                    else
                    {
                        dif = 0;
                    }
                    bef = ras;
                    today += dif;
                    if (today / Math.Pow(1024.0, 3.0) < 10.0)
                    {
                        Console.Write("{0} {1} 送受信バイト数:{2:f2}GB 速度:{3:f2}MB/s {4:f2}Mbps", 
                                        time, 
                                        networkInterface.Name, 
                                        today / Math.Pow(1024, 3), 
                                        dif / Math.Pow(1024, 2), 
                                        dif * 8 / Math.Pow(1024, 2)
                                      );
                    }
                    else
                    {
                        Console.Write("通信量が10GBを超えた可能性があります。ネットワークアダプタを切断します。");
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.Arguments = "/c netsh int set int " + networkInterface.Name + " dis";
                            process.Start();
                            process.WaitForExit();
                            break;
                        }
                    }
                }
                Thread.Sleep(1000);
            }
            Quit.Save();
            Console.Read();
            Quit.QSave(CtrlTypes.CTRL_C_EVENT);
        }

        class Quit
        {
            public static bool QSave(CtrlTypes ctrlTypes)
            {
                using (var sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\data.txt", true))
                {
                    sw.WriteLine(ctrlTypes);
                }
                try
                {
                    if (set >= 0) Save();
                }
                catch
                {
                    return false;
                }
                //Console.SetBufferSize(bufwid, bufhei);
                //Console.SetWindowSize(winwid, winhei);
                //Console.Clear();
                SetConsoleCtrlHandler(del, false);
                Environment.Exit(0);
                return true;
            }

            public static void Save()
            {
                using (var sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\data.txt", true))
                {
                    sw.WriteLine(time + " " + today);
                }
            }
        }
    }
}
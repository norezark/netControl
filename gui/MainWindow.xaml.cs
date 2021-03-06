﻿using Gma.System.MouseKeyHook;
using netControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using static netControl.Graphical;

namespace gui
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>

    public partial class MainWindow : Window, INotifyPropertyChanged, ICommand
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private const string database = "data.db";
        private const string ConnectionString = @"Data Source=" + database;
        private string context = "";
        private string sum = "";
        private static long today = 0;
        private static DateTime time;
        private static int set = -1;
        private static PhysicalAddress mac = new PhysicalAddress(new byte[] { 0 });
        private static NetworkInterface netint;
        private static System.Timers.Timer timer;
        private static double windowPointX = 0;
        private static double windowPointY = 0;
        private static bool flag_topmost = false;
        private static bool flag_transport = false;
        private static bool flag_control = false;
        private static bool set_control = false;
        private static bool flag_limit = true;
        private static int set_limit = 10;
        private static int date_h = 1;
        private static int date_m = 0;
        private static double set_opacity = 1;
        private static IKeyboardMouseEvents globalhook;

        public string Context { get => context; set => context = value; }
        public static long Today { get => today; set => today = value; }
        public static DateTime Time { get => time; set => time = value; }
        public static int Set { get => set; set => set = value; }
        public static PhysicalAddress Mac { get => mac; set => mac = value; }
        public static NetworkInterface Netint { get => netint; set => netint = value; }
        public static System.Timers.Timer Timer { get => timer; set => timer = value; }
        public static double WindowPointX { get => windowPointX; set => windowPointX = value; }
        public static double WindowPointY { get => windowPointY; set => windowPointY = value; }
        public bool Flag_topmost
        {
            get => flag_topmost;
            set
            {
                flag_topmost = value;
                OnPropertyChanged("Flag_topmost");
                if (value)
                {
                    globalhook.MouseMove += GlobalhookMouseMoveEvent;
                }
                else
                {
                    globalhook.MouseMove -= GlobalhookMouseMoveEvent;
                }
            }
        }
        public string Sum { get => sum; set => sum = value; }
        public double Set_opacity
        {
            get
            {
                return set_opacity;
            }
            set
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    set_opacity = value;
                    this.Opacity = set_opacity;
                    OnPropertyChanged("Set_opacity");
                }));
            }
        }
        public bool Flag_transport { get => flag_transport; set => flag_transport = value; }
        public ICommand Leftclickcommand { get; private set; }
        public int Set_limit { get => set_limit; set => set_limit = value; }
        public bool Flag_limit { get => flag_limit; set => flag_limit = value; }
        public int Date_h { get => date_h; set => date_h = value; }
        public int Date_m { get => date_m; set => date_m = value; }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            globalhook = Hook.GlobalEvents();

            DbInit();
            while (Set == -1 || !IsActiveAdapter(Set) || Netint.GetPhysicalAddress().ToString() != Mac.ToString())
            {
                var dialog = new Dialog();
                dialog.ShowDialog();
                if (dialog.f)
                {
                    Environment.Exit(0);
                }
            }

            Window owner = new Window
            {
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false,
                Left = -1,
                Top = -1,
                Width = 0,
                Height = 0,
                Visibility = Visibility.Collapsed
            };
            owner.Show();
            this.Owner = owner;
            owner.Hide();

            DataContext = this;
            if (Flag_transport)
            {
                OnPropertyChanged("Set_opacity");
            }
            else
            {
                Opacity = 1;
            }
            time_label1.DataContext = this;
            TaskberIcon.DataContext = this;
            TaskberIcon.ContextMenu.DataContext = this;
            this.Leftclickcommand = this;
            this.WindowState = WindowState.Normal;
            this.Left = WindowPointX;
            this.Top = WindowPointY;

            Timer = new System.Timers.Timer();
            Timer.Elapsed += new ElapsedEventHandler(S_content);
            Timer.Interval = 1000;
            Timer.AutoReset = true;
            Timer.Enabled = true;

            setting = new Setting
            {
                Parent = this
            };
            setting.Show();
            setting.Hide();
            
            graph.Show();
        }

        private void GlobalhookMouseMoveEvent(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (flag_control || !Flag_transport) return;
            if (e.X >= Left && e.X <= Left + this.Width && e.Y >= Top && e.Y <= Top + this.Height)
            {
                Opacity = 0;
            }
            else if (Opacity != Set_opacity)
            {
                Set_opacity = Set_opacity;
            }
        }


        public bool Set_control
        {
            get
            {
                return set_control;
            }
            set
            {
                set_control = value;
                if (set_control)
                {
                    globalhook.KeyDown += GlobalhookKeyDownEvent;
                    globalhook.KeyUp += GlobalhookKeyUpEvent;
                }
                else
                {
                    globalhook.KeyDown -= GlobalhookKeyDownEvent;
                    globalhook.KeyUp -= GlobalhookKeyUpEvent;
                }
            }
        }

        private void GlobalhookKeyDownEvent(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                Opacity = 1;
                flag_control = true;
            }
        }
        private void GlobalhookKeyUpEvent(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (flag_control)
            {
                Set_opacity = Set_opacity;
                flag_control = false;
                GlobalhookMouseMoveEvent(this, new System.Windows.Forms.MouseEventArgs(MouseButtons.None, 0, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 0));
            }
        }

        private void DbInit()
        {
            bool flag = false;
            using (var con = new SQLiteConnection(ConnectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = @"CREATE TABLE adapter(a_set INTEGER DEFAULT -1)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"CREATE TABLE traffic(t_num INTEGER PRIMARY KEY AUTOINCREMENT)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"CREATE TABLE setting(s_topmost INTEGER DEFAULT 0)";
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        flag = true;
                    }
                }
            }
            using (var con = new SQLiteConnection(ConnectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    var columns = new List<string[]>() {
                            new string[] { "adapter", "a_set INTEGER DEFAULT -1", "a_mac TEXT DEFAULT '0000000000000000'" },
                            new string[] { "traffic", "t_num INTEGER PRIMARY KEY AUTOINCREMENT", "t_date TEXT", "t_bytes INTEGER" },
                            new string[] { "setting",   "s_topmost INTEGER DEFAULT 0",
                                                        "s_x REAL DEFAULT 0",
                                                        "s_y REAL DEFAULT 0",
                                                        "s_trans INTEFGER DEFAULT 1",
                                                        "s_opacity REAL DEFAULT 1",
                                                        "s_control INTEGER DEFAULT 0",
                                                        "s_limit INTEGER DEFAULT 1",
                                                        "s_limiting INTEGER DEFAULT 1",
                                                        "s_date_h INTEGER DEFAULT 1",
                                                        "s_date_m INTEGER DEFAULT 0"
                                            }
                        };
                    foreach (var column in columns)
                    {
                        int i = 1;
                        while (i < column.Length)
                        {
                            try
                            {
                                cmd.CommandText = @"ALTER TABLE " + column[0] + " ADD COLUMN " + column[i];
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                if (!e.Message.Contains("SQL logic error\r\n"))
                                {
                                    throw e;
                                }
                            }
                            i++;
                        }
                    }

                    if (flag)
                    {
                        cmd.CommandText = @"SELECT * FROM adapter";
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            Set = reader.GetInt16(0);
                            Mac = PhysicalAddress.Parse(reader.GetString(1));
                        }
                        cmd.CommandText = @"SELECT t_num FROM traffic";
                        var tmp = new List<int>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tmp.Add(reader.GetInt16(0));
                            }
                        }
                        cmd.CommandText = @"SELECT t_date FROM traffic WHERE t_num=@num";
                        cmd.Parameters.Add(new SQLiteParameter("num", tmp.Max()));
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            Time = DateTime.Parse(reader.GetString(0).Replace('_', ' '));
                        }
                        if (DateTime.Compare(Time.AddHours(-1).AddSeconds(-1).Date, DateTime.Now.AddHours(-1).Date) == 0)
                        {
                            cmd.CommandText = @"SELECT t_bytes FROM traffic WHERE t_date='" + Time.ToString().Replace(' ', '_') + "'";
                            using (var reader = cmd.ExecuteReader())
                            {
                                reader.Read();
                                Today = reader.GetInt64(0);
                            }
                        }
                        cmd.CommandText = @"SELECT * FROM setting";
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            int i = 0;
                            Flag_topmost = (reader.GetInt16(i++) == 1);
                            WindowPointX = reader.GetDouble(i++);
                            WindowPointY = reader.GetDouble(i++);
                            Flag_transport = (reader.GetInt16(i++) == 1);
                            Set_opacity = reader.GetDouble(i++);
                            Set_control = (reader.GetInt16(i++) == 1);
                            Set_limit = reader.GetInt16(i++);
                            Flag_limit = (reader.GetInt16(i++) == 1);
                            Date_h = reader.GetInt16(i++);
                            Date_m = reader.GetInt16(i++);
                        }
                    }
                    else
                    {
                        cmd.CommandText = @"INSERT INTO adapter DEFAULT VALUES";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"INSERT INTO setting DEFAULT VALUES";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static bool IsActiveAdapter(int tmp)
        {
            if (tmp < 0) return false;
            Netint = NetworkInterface.GetAllNetworkInterfaces()[tmp];
            if (Netint.OperationalStatus == OperationalStatus.Up &&
                Netint.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                Netint.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        class Contents
        {
            public long bef = 0;
            public long ras;
            public DateTime p;
            public string tmpcont;
            public bool tmpflag1, tmpflag2;
            public long dif;
            public double MBps;
        }
        private Contents con = new Contents();
        private void S_content(object sender, ElapsedEventArgs e)
        {
            con.ras = Netint.GetIPv4Statistics().BytesReceived + Netint.GetIPv4Statistics().BytesSent;

            Time = DateTime.Now;
            con.p = Time.AddHours(-Date_h).AddMinutes(-Date_m);

            if (con.p.Date != con.p.AddSeconds(-1).Date)
            {
                Save();
                Today = 0;
                con.bef = 0;
            }

            con.tmpcont = "";
            con.tmpflag1 = true;
            con.tmpflag2 = false;
            if (con.ras == 0)
            {
                con.bef = 0;
                con.tmpcont = "アダプタが切断されています。";
                con.tmpflag1 = false;
            }
            if (con.bef == 0) con.bef = con.ras;
            con.dif = con.ras - con.bef;
            con.bef = con.ras;
            Today += con.dif;
            if (Today / Math.Pow(1024.0, 3.0) < Set_limit)
            {
                if (con.tmpflag1) con.tmpcont = Netint.Name;
            }
            else
            {
                if (Flag_limit)
                {
                    con.tmpcont = "アダプタを切断します。";
                    con.tmpflag2 = true;
                }
            }
            con.MBps = con.dif / Math.Pow(1024, 2);
            Sum = $"送受信バイト数:{Today / Math.Pow(1024, 3):f2}GB";
            Context = $"{Time}\n{con.tmpcont}\n{Sum}\n速度:{con.MBps:f2}MB/s {con.MBps * 8:f2}Mbps";
            OnPropertyChanged("Sum");
            OnPropertyChanged("Context");
            if (graph.IsHandleCreated) graph.Invoke((MethodInvoker)delegate { graph.AddData(con.MBps); });
            if (con.tmpflag2)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Arguments = "/c netsh int set int " + Netint.Name + " dis";
                    process.Start();
                    process.WaitForExit();
                }
                Timer.Close();
            }
        }


        private void Save()
        {
            if (Set != -1)
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE adapter SET a_set=@set, a_mac='" + Mac.ToString() + "'";
                        cmd.Parameters.Add(new SQLiteParameter("set", Set));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"INSERT INTO traffic (t_date, t_bytes) VALUES (@date, @bytes)";
                        cmd.Parameters.Add(new SQLiteParameter("date", Time.ToString().Replace(' ', '_')));
                        cmd.Parameters.Add(new SQLiteParameter("bytes", Today));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"UPDATE setting SET s_topmost=@topmost, s_x=@x, s_y=@y, s_trans=@trans, s_opacity=@opacity, s_control=@control, s_limit=@limit, s_limiting=@limiting, s_date_h=@date_h, s_date_m=@date_m";
                        cmd.Parameters.Add(new SQLiteParameter("topmost", flag_topmost ? 1 : 0));
                        cmd.Parameters.Add(new SQLiteParameter("x", WindowPointX));
                        cmd.Parameters.Add(new SQLiteParameter("y", WindowPointY));
                        cmd.Parameters.Add(new SQLiteParameter("trans", Flag_transport ? 1 : 0));
                        cmd.Parameters.Add(new SQLiteParameter("opacity", Set_opacity));
                        cmd.Parameters.Add(new SQLiteParameter("control", Set_control ? 1 : 0));
                        cmd.Parameters.Add(new SQLiteParameter("limit", Set_limit));
                        cmd.Parameters.Add(new SQLiteParameter("limiting", Flag_limit ? 1 : 0));
                        cmd.Parameters.Add(new SQLiteParameter("date_h", Date_h));
                        cmd.Parameters.Add(new SQLiteParameter("date_m", Date_m));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            setting.Close();
            Save();
            graph.Close();
            Flag_topmost = false;
            Set_control = false;
            TaskberIcon.Dispose();
            globalhook.Dispose();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowPointX = this.Left;
            WindowPointY = this.Top;
        }

        public void Open(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        private Setting setting;
        private void Setting(object sender, RoutedEventArgs e)
        {
            setting.Show();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TaskberIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Open(sender, e);

        public bool CanExecute(object parameter) => true;
        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter) => Open(this, null);

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner.Close();
            Environment.Exit(0);
        }

        private Graphical graph = new Graphical();
    }
}
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
using System.Windows.Input;
//using HotKey;

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

        private const string ConnectionString = @"Data Source=data.db";
        private string context = "";
        private string sum = "";
        private static long today = 0;
        private static DateTime time;
        private static int set = -1;
        private static string id = "{0}";
        private static NetworkInterface netint;
        private static Timer timer;
        private static double windowPointX = 0;
        private static double windowPointY = 0;
        private static bool topmostflag = false;

        public string Context { get => context; set => context = value; }
        public static long Today { get => today; set => today = value; }
        public static DateTime Time { get => time; set => time = value; }
        public static int Set { get => set; set => set = value; }
        public static string Id { get => id; set => id = value; }
        public static NetworkInterface Netint { get => netint; set => netint = value; }
        public static Timer Timer { get => timer; set => timer = value; }
        public static double WindowPointX { get => windowPointX; set => windowPointX = value; }
        public static double WindowPointY { get => windowPointY; set => windowPointY = value; }
        public static bool Topmostflag { get => topmostflag; set => topmostflag = value; }
        public string Sum { get => sum; set => sum = value; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DbInit();

            while (Set == -1 || !IsActiveAdapter(Set) || Netint.Id != Id)
            {
                var dialog = new Dialog();
                dialog.ShowDialog();
                if (dialog.f)
                {
                    Environment.Exit(0);
                }
            }

            time_label1.DataContext = this;
            TaskberIcon.DataContext = this;
            TaskberIcon.ContextMenu.DataContext = this;
            this.Leftclickcommand = this;
            this.WindowState = WindowState.Normal;
            this.Left = WindowPointX;
            this.Top = WindowPointY;

            //KeyboardHook.AddEvent(Keyboardhook);
            //try
            //{
            //    KeyboardHook.Start();
            //}
            //catch(Exception e1)
            //{
            //    Debug.WriteLine(e1);
            //}
            //HotKeyRegister hotKey = new HotKeyRegister(MOD_KEY.CONTROL, System.Windows.Forms.Keys.None, this);
            //hotKey.HotKeyPressed += (s) =>
            //{

            //};

            Timer = new Timer();
            Timer.Elapsed += new ElapsedEventHandler(Content);
            Timer.Interval = 1000;
            Timer.AutoReset = true;
            Timer.Enabled = true;


        }

        //private void Keyboardhook(ref KeyboardHook.StateKeyboard stateKeyboard)
        //{
        //    if (stateKeyboard.Stroke == KeyboardHook.Stroke.KEY_DOWN)
        //    {
        //        if (stateKeyboard.Key == System.Windows.Forms.Keys.LControlKey || stateKeyboard.Key == System.Windows.Forms.Keys.RControlKey)
        //        {
        //            Opacity = 100;
        //        }
        //    }
        //    else if (stateKeyboard.Stroke == KeyboardHook.Stroke.KEY_UP)
        //    {
        //        if (stateKeyboard.Key == System.Windows.Forms.Keys.LControlKey || stateKeyboard.Key == System.Windows.Forms.Keys.RControlKey)
        //        {
        //            Opacity = 30;
        //        }
        //    }
        //}

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
                        cmd.CommandText = @"CREATE TABLE adapter(a_set INTEGER, a_id TEXT)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"CREATE TABLE traffic(t_num INTEGER PRIMARY KEY AUTOINCREMENT, t_date TEXT, t_bytes INTEGER)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"CREATE TABLE setting(s_topmost INTEGER, s_x REAL, s_y REAL)";
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        flag = true;
                    }
                }
            }
            if (flag)
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT * FROM adapter";
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            Set = reader.GetInt16(0);
                            Id = reader.GetString(1);
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
                            Topmostflag = (reader.GetInt16(0) == 1);
                            WindowPointX = reader.GetDouble(1);
                            WindowPointY = reader.GetDouble(2);
                        }
                    }
                }
            }
            else
            {
                using (var con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = @"INSERT INTO adapter VALUES (-1, '{0}')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"INSERT INTO traffic VALUES (0, '1970/01/01_00:00:00', 0)";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"INSERT INTO setting VALUES (0, 0, 0)";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static bool IsActiveAdapter(int tmp)
        {
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
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        
        private long bef = 0;
        private new void Content(object sender, ElapsedEventArgs e)
        {
            long ras = Netint.GetIPv4Statistics().BytesReceived + Netint.GetIPv4Statistics().BytesSent;

            Time = DateTime.Now;
            
            if (Time.AddHours(-1).AddSeconds(-1).ToShortDateString() != Time.AddHours(-1).ToShortDateString())
            {
                Save();
                Today = 0;
                bef = 0;
            }

            string tmpcont = "";
            bool tmpflag1 = true;
            bool tmpflag2 = false;
            if (ras == 0)
            {
                bef = 0;
                tmpcont = "アダプタが切断されています。";
                tmpflag1 = false;
            }
            if (bef == 0) bef = ras;
            long dif = ras - bef;
            bef = ras;
            Today += dif;
            if (Today / Math.Pow(1024.0, 3.0) < 10.0)
            {
                if (tmpflag1) tmpcont = Netint.Name;
            }
            else
            {
                tmpcont = "アダプタを切断します。";
                tmpflag2 = true;
            }
            Sum = $"送受信バイト数:{Today / Math.Pow(1024, 3):f2}GB";
            Context = $"{Time}\n{tmpcont}\n{Sum}\n速度:{dif / Math.Pow(1024, 2):f2}MB/s {dif * 8 / Math.Pow(1024, 2):f2}Mbps";
            OnPropertyChanged("Sum");
            OnPropertyChanged("Context");
            if (tmpflag2)
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
                        cmd.CommandText = @"UPDATE adapter SET a_set=@set, a_id='" + Id + "'";
                        cmd.Parameters.Add(new SQLiteParameter("set", Set));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"INSERT INTO traffic (t_date, t_bytes) VALUES (@date, @bytes)";
                        cmd.Parameters.Add(new SQLiteParameter("date", Time.ToString().Replace(' ', '_')));
                        cmd.Parameters.Add(new SQLiteParameter("bytes", Today));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"UPDATE setting SET s_topmost=@topmost, s_x=@x, s_y=@y";
                        cmd.Parameters.Add(new SQLiteParameter("topmost", topmostflag ? 1 : 0));
                        cmd.Parameters.Add(new SQLiteParameter("x", WindowPointX));
                        cmd.Parameters.Add(new SQLiteParameter("y", WindowPointY));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Save();
            TaskberIcon.Dispose();
            //KeyboardHook.Stop();
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

        //private void MenuItem_Checked(object sender, RoutedEventArgs e)
        //{
        //    this.Topmost = true;
        //}

        //private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    this.Topmost = false;
        //}

        private void Setting(object sender, RoutedEventArgs e)
        {
            var setting = new setting();
            setting.Parent = this;
            setting.Show();
        }
        
        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TaskberIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Open(sender, e);

        public ICommand Leftclickcommand { get; private set; }
        public bool CanExecute(object parameter) => true;
        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter) => Open(null, null);
    }
}

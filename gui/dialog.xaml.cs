using System;
using System.Net.NetworkInformation;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace gui
{
    /// <summary>
    /// dialog.xaml の相互作用ロジック
    /// </summary>
    public partial class Dialog : Window
    {
        public Dialog()
        {
            InitializeComponent();
        }

        public NetworkInterface[] allNetworkInterfaces;

        private Timer Timer;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var netint in allNetworkInterfaces)
            {
                combo1.Items.Add(netint.Name);
            }

            Timer = new Timer();
            Timer.Elapsed += new ElapsedEventHandler(AdapterCheck);
            Timer.Interval = 1000;
            Timer.AutoReset = true;
            Timer.Start();
        }

        private void AdapterCheck(object sender, ElapsedEventArgs e)
        {
            allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (MainWindow.IsActiveAdapter(MainWindow.Set) && MainWindow.Mac.ToString() == allNetworkInterfaces[MainWindow.Set].GetPhysicalAddress().ToString())
            {
                Timer.Dispose();
                this.Dispatcher.Invoke(new Action(() => { Close(); }));
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo1.SelectedIndex != -1)
            {
                button1.IsEnabled = true;
            }
            else
            {
                button1.IsEnabled = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.IsActiveAdapter(combo1.SelectedIndex))
            {
                MainWindow.Set = combo1.SelectedIndex;
                MainWindow.Mac = allNetworkInterfaces[MainWindow.Set].GetPhysicalAddress();
                Close();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            combo1.Items.Clear();
            allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var netint in allNetworkInterfaces)
            {
                combo1.Items.Add(netint.Name);
            }
        }

        public bool f = false;

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            f = true;
            Close();
        }
    }
}

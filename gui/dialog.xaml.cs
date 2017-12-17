using System.Net.NetworkInformation;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var netint in allNetworkInterfaces)
            {
                combo1.Items.Add(netint.Name);
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
                MainWindow.Id = allNetworkInterfaces[MainWindow.Set].Id;
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

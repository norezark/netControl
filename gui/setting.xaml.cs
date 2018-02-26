using System.Windows;
using System;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace gui
{
    /// <summary>
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class Setting : Window, INotifyPropertyChanged
    {
        public Setting()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private MainWindow parent;
        public new MainWindow Parent { get => parent; set => parent = value as MainWindow; }
        private static int t_limit;
        private static int s_date_h;
        private static int s_date_m;

        public bool Flag_transport
        {
            get
            {
                return Parent.Flag_transport;
            }
            set
            {
                Parent.Flag_transport = value;
                OnPropertyChanged("Flag_transport");
            }
        }
        public bool Flag_control
        {
            get
            {
                return Parent.Set_control;
            }
            set
            {
                Parent.Set_control = value;
                OnPropertyChanged("Flag_control");
            }
        }

        public double Set_opacity { get => Parent.Set_opacity; }
        public static int T_limit { get => t_limit; set => t_limit = value; }
        public static int S_date_h { get => s_date_h; set => s_date_h = value; }
        public static int S_date_m { get => s_date_m; set => s_date_m = value; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            set_topmost.DataContext = Parent;
            set_transport.DataContext = this;
            label_transport.DataContext = this;
            slid_opacity.DataContext = this;
            set_control.DataContext = this;
            OnPropertyChanged("Flag_transport");
            T_limit = Parent.Set_limit;
            set_limit.DataContext = this;
            OnPropertyChanged("T_limit");
            set_limiting.DataContext = Parent;
            S_date_h = Parent.Date_h;
            S_date_m = Parent.Date_m;
            date_h.DataContext = this;
            date_m.DataContext = this;
            OnPropertyChanged("S_Date_h");
            OnPropertyChanged("S_Date_m");
        }

        private void Set_topmost_Checked(object sender, RoutedEventArgs e)
        {
            Parent.Flag_topmost = true;
        }

        private void Set_topmost_Unchecked(object sender, RoutedEventArgs e)
        {
            Parent.Flag_topmost = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Parent.Flag_transport = true;
            Parent.Set_opacity = Parent.Set_opacity;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Parent.Flag_transport = false;
            Parent.Dispatcher.Invoke(new Action(() =>
            {
                Parent.Opacity = 1;
            }));
        }

        private void Slid_opacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(Parent.Flag_transport)
            {
                Parent.Dispatcher.Invoke(new Action(() => Parent.Set_opacity = e.NewValue));
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Parent.Set_limit = T_limit;
            Parent.Date_h = S_date_h;
            Parent.Date_m = S_date_m;
            e.Cancel = true;
            Hide();
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            Parent.Flag_limit = true;
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            Parent.Flag_limit = false;
        }
    }
}

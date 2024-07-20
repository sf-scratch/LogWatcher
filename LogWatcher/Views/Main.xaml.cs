using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogWatcher.Views
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Window
    {

        public Main()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void ShowLogWatcher_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private void ExitLogWatcher_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void DialogHost_DialogOpened(object sender, MaterialDesignThemes.Wpf.DialogOpenedEventArgs eventArgs)
        {
            this.MouseDown += CheckClickOutsideDialog;
        }

        private void dialogHost_DialogClosed(object sender, DialogClosedEventArgs eventArgs)
        {
            this.MouseDown -= CheckClickOutsideDialog;
        }

        private void CheckClickOutsideDialog(object sender, MouseButtonEventArgs e)
        {
            // 检查点击位置是否在当前窗体内，对话框之外
            if (Mouse.Captured is null && this.IsMouseOver && !this.dialogContent.IsMouseOver)
            {
                //关闭对话框
                DialogHost.CloseDialogCommand.Execute(null, this.dialogHost);
            }
        }
    }
}

using LogWatcher.Utils;
using Prism.DryIoc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LogWatcher.ViewModels
{
    internal class MainViewModel : BindableBase
    {
        private LogListenerServer logServer;

        private ObservableCollection<string> messageList;

        public ObservableCollection<string> MessageList
        {
            get { return messageList; }
            set { messageList = value; }
        }

        private bool isAutoReconnection;

        public bool IsAutoReconnection
        {
            get { return this.logServer.IsAutoReconnection; }
            set
            {
                this.logServer.IsAutoReconnection = value;
                RaisePropertyChanged();
            }
        }

        public MainViewModel()
        {
            this.messageList = new ObservableCollection<string>();
            OpenLogServer();
        }

        private void OpenLogServer()
        {
            //配置文件的NG文件夹路径
            string ngLogFolder = ConfigurationManager.AppSettings["NgLogFolder"];
            if (!Directory.Exists(ngLogFolder))
            {
                MessageBox.Show("config文件 NgLogFolder 文件夹路径不存在");
                Environment.Exit(0);
            }
            //配置文件的NG文件夹路径
            string passLogFolder = ConfigurationManager.AppSettings["PassLogFolder"];
            if (!Directory.Exists(ngLogFolder))
            {
                MessageBox.Show("config文件 PassLogFolder 文件夹路径不存在");
                Environment.Exit(0);
            }
            //配置文件的主控监听ip地址
            string masterIPAddressStr = ConfigurationManager.AppSettings["MasterIPAddress"];
            IPAddress masterIPAddress;
            if (!IPAddress.TryParse(masterIPAddressStr, out masterIPAddress))
            {
                MessageBox.Show("config文件 MasterIPAddress 格式错误");
                Environment.Exit(0);
            }
            //配置文件的主控监听端口
            string masterPortStr = ConfigurationManager.AppSettings["MasterPort"];
            int masterPort;
            if (!int.TryParse(masterPortStr, out masterPort))
            {
                MessageBox.Show("config文件 MasterPort 格式错误");
                Environment.Exit(0);
            }
            this.logServer = new LogListenerServer(ngLogFolder, passLogFolder, masterIPAddress, masterPort);
            this.logServer.PrintMessage += PrintMessage;
            this.logServer.Start();
        }

        private void PrintMessage(string message)
        {
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                this.MessageList.Add(message);
            });
        }
    }
}

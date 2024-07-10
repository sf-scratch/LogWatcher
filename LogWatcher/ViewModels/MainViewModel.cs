using LogWatcher.Utils;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LogWatcher.ViewModels
{
    internal class MainViewModel : BindableBase
    {
        private ObservableCollection<string> messageList;

        public ObservableCollection<string> MessageList
        {
            get { return messageList; }
            set { messageList = value; }
        }

        public MainViewModel()
        {
            this.messageList = new ObservableCollection<string>();
            OpenServer();
        }

        private void OpenServer()
        {
            string ngLogFolder = ConfigurationManager.AppSettings["NgLogFolder"];
            string passLogFolder = ConfigurationManager.AppSettings["PassLogFolder"];
            //配置文件的主控监听ip地址
            string masterIPAddressStr = ConfigurationManager.AppSettings["MasterIPAddress"];
            IPAddress masterIPAddress;
            if (!IPAddress.TryParse(masterIPAddressStr, out masterIPAddress))
            {
                this.messageList.Add("配置文件 MasterIPAddress 格式错误");
                return;
            }
            //配置文件的主控监听端口
            string masterPortStr = ConfigurationManager.AppSettings["MasterPort"];
            int masterPort;
            if (!int.TryParse(masterPortStr, out masterPort))
            {
                this.messageList.Add("配置文件 MasterPort 格式错误");
                return;
            }
            LogListenerServer server = new LogListenerServer(ngLogFolder, passLogFolder, masterIPAddress, masterPort, this.messageList);
            server.Start();
        }
    }
}

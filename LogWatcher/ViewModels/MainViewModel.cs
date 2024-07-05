using LogWatcher.Utils;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            LogListenerServer server = new LogListenerServer(@"C:\Users\17632\Desktop\TestDateSummary\product1\TempLOG\NG", this.messageList);
            server.Start();
        }
    }
}

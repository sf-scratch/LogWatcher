using LogWatcher.Parsers;
using Prism.DryIoc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace LogWatcher.Utils
{
    public class LogListenerServer
    {
        private FileSystemWatcher watcher;

        private ObservableCollection<string> messageList;

        public LogListenerServer(string path, ObservableCollection<string> messageList)
        {
            this.messageList = messageList;
            this.watcher = new FileSystemWatcher();
            try
            {
                this.watcher.Path = path;
                this.watcher.IncludeSubdirectories = false;
                this.watcher.Created += new FileSystemEventHandler(Created);
            }
            catch (Exception ex)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
                this.watcher = null;
                PrintMessage($"Error: {ex.Message}");
            }
            ConnectToServer();
        }

        public void Start()
        {
            PrintMessage($"{this.watcher.Path} 文件监控已经启动...");
            this.watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            PrintMessage($"{this.watcher.Path} 文件监控已经停止...");
            this.watcher.EnableRaisingEvents = false;
            this.watcher.Dispose();
            this.watcher = null;
        }

        private void Created(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                StringBuilder builder = new StringBuilder();
                using (StringWriter writer = new StringWriter(builder))
                {
                    PrintMessage(string.Empty);
                    PrintMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [新增] {e.Name}");
                    LogContentParser logParser = new LogContentParser();
                    logParser.LoadLogFile(e.FullPath);
                    List<string> list = logParser.ParseToList();
                    foreach (string line in list)
                    {
                        writer.WriteLine(line);
                        PrintMessage(line);
                    }
                }
                Send(builder.ToString());

            }
        }

        private TcpClient client;

        private void ConnectToServer()
        {
            client = new TcpClient(AddressFamily.InterNetwork);
            client.Connect(IPAddress.Parse("192.168.0.14"), 8100);
            Send("Hello from client");
        }

        private void Send(string message)
        {
            try
            {
                if (client.Connected)
                {
                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        //发送消息给主控
                        writer.Write(message);
                        //消息发送完毕主控会回复消息
                        string receiveMessage = reader.ReadString();
                        PrintMessage(receiveMessage);
                    }
                }
                else
                {
                    PrintMessage("与主控的连接已断开");
                }
            }
            catch (Exception e)
            {
                PrintMessage(e.Message);
            }
        }

        private void PrintMessage(string message)
        {
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                this.messageList.Add(message);
            });
        }
    }
}

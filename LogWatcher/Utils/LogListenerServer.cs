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
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices.ComTypes;
using System.Configuration;

namespace LogWatcher.Utils
{
    public class LogListenerServer
    {
        private FileSystemWatcher watcher;

        private ObservableCollection<string> messageList;

        public LogListenerServer(string path, IPAddress masterIPAddress, int masterPort, ObservableCollection<string> messageList)
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
            ConnectToServer(masterIPAddress, masterPort);
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
                SendMsgToMaster(builder.ToString());
            }
        }

        private TcpClient client;

        private void ConnectToServer(IPAddress masterIPAddress, int masterPort)
        {
            client = new TcpClient(AddressFamily.InterNetwork);
            client.Connect(masterIPAddress, masterPort);
            SendMsgToMaster("Hello from client");
        }

        private void SendMsgToMaster(string msg)
        {
            try
            {
                if (this.client.Connected)
                {
                    NetworkStream stream = this.client.GetStream();
                    byte[] msgData = Encoding.UTF8.GetBytes(msg);
                    long totalBytes = sizeof(long) + msgData.Length;
                    byte[] totalBytesData = BitConverter.GetBytes(totalBytes);
                    stream.Write(totalBytesData, 0, totalBytesData.Length);//发送总字节数
                    stream.Write(msgData, 0, msgData.Length);//发送消息给主控

                    string receiveMsg = ReceiveMsgByMaster(stream);//阻塞，接收主控的回复消息
                    PrintMessage(receiveMsg);
                }
                else
                {
                    PrintMessage("与主控的连接已断开，无法发送消息！");
                }
            }
            catch (Exception e)
            {
                PrintMessage(e.Message);
            }
        }

        private string ReceiveMsgByMaster(NetworkStream stream)
        {
            string msg = string.Empty;
            byte[] longBuffer = new byte[sizeof(long)];
            long totalBytes = 0;
            if (stream.Read(longBuffer, 0, longBuffer.Length) == sizeof(long))
            {
                totalBytes = BitConverter.ToInt64(longBuffer, 0);
                byte[] msgData = new byte[totalBytes - longBuffer.Length];
                long numBytesRead = stream.Read(msgData, 0, msgData.Length);
                if (numBytesRead == msgData.Length)
                {
                    msg = Encoding.UTF8.GetString(msgData);
                }
                else
                {
                    msg = "接收的消息异常";
                }
            }
            else
            {
                msg = string.Empty;
            }

            return msg;
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

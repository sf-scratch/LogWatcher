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
using LogWatcher.DTOs;
using Newtonsoft.Json;

namespace LogWatcher.Utils
{
    public class LogListenerServer
    {
        private FileSystemWatcher ngWatcher;

        private FileSystemWatcher passWatcher;

        private ObservableCollection<string> messageList;

        public LogListenerServer(string ngPath, string passPath, IPAddress masterIPAddress, int masterPort, ObservableCollection<string> messageList)
        {
            this.messageList = messageList;
            this.ngWatcher = new FileSystemWatcher();
            InitWatcher(this.ngWatcher, ngPath);
            this.ngWatcher.Created += new FileSystemEventHandler(NgCreated);
            this.passWatcher = new FileSystemWatcher();
            InitWatcher(this.passWatcher, passPath);
            this.passWatcher.Created += new FileSystemEventHandler(PassCreated);
            ConnectToServer(masterIPAddress, masterPort);
        }

        public void Start()
        {
            PrintMessage($"{this.ngWatcher.Path} 文件监控已经启动...");
            this.ngWatcher.EnableRaisingEvents = true;
            PrintMessage($"{this.passWatcher.Path} 文件监控已经启动...");
            this.passWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            PrintMessage($"{this.ngWatcher.Path} 文件监控已经停止...");
            this.ngWatcher.EnableRaisingEvents = false;
            this.ngWatcher.Dispose();
            this.ngWatcher = null;
            PrintMessage($"{this.passWatcher.Path} 文件监控已经停止...");
            this.passWatcher.EnableRaisingEvents = false;
            this.passWatcher.Dispose();
            this.passWatcher = null;
        }

        private void InitWatcher(FileSystemWatcher watcher, string path)
        {
            try
            {
                watcher.Path = path;
                watcher.IncludeSubdirectories = false;
            }
            catch (Exception ex)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
                PrintMessage($"Error: {ex.Message}");
            }
        }

        private void NgCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string fileSuffix = e.Name.Split('.').Last();
                if (fileSuffix == "log")
                {
                    StringBuilder builder = new StringBuilder();
                    using (StringWriter writer = new StringWriter(builder))
                    {
                        PrintMessage(string.Empty);
                        PrintMessage($"{DateUtil.Now}   [NG]    {e.Name}");
                        LogContentParser logParser = new LogContentParser();
                        logParser.LoadLogFile(e.FullPath);
                        List<string> list = logParser.ParseToList();
                        foreach (string line in list)
                        {
                            writer.WriteLine(line);
                            PrintMessage(line);
                        }
                    }
                    if (SendMsgDTOToMaster(builder.ToString().Trim()))
                    {
                        string receiveMsg;
                        ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                        PrintMessage(receiveMsg);
                    }
                }
            }
        }

        private void PassCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string fileSuffix = e.Name.Split('.').Last();
                if (fileSuffix == "log")
                {
                    PrintMessage(string.Empty);
                    PrintMessage($"{DateUtil.Now}   [PASS]  {e.Name}");
                    if (SendMsgDTOToMaster("PASS"))
                    {
                        string receiveMsg;
                        ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                        PrintMessage(receiveMsg);
                    }
                }
            }
        }

        private TcpClient client;

        private async void ConnectToServer(IPAddress masterIPAddress, int masterPort)
        {
            this.client = new TcpClient(AddressFamily.InterNetwork);
            try
            {
                await this.client.ConnectAsync(masterIPAddress, masterPort);
            }
            catch (Exception e)
            {
                PrintMessage(e.Message);
            }

            if (SendMsgDTOToMaster("Hello from client"))
            {
                string receiveMsg;
                ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                PrintMessage(receiveMsg);
            }
        }

        private bool SendMsgDTOToMaster(string msg)
        {
            bool send = false;
            try
            {
                if (this.client.Connected)
                {
                    MsgDTO msgDTO = new MsgDTO(msg);
                    IPEndPoint iPEndPoint = this.client.Client.LocalEndPoint as IPEndPoint;
                    if (iPEndPoint != null)
                    {
                        msgDTO.SenderAddress = iPEndPoint.Address.ToString();
                        msgDTO.SenderPort = iPEndPoint.Port;
                    }
                    NetworkStream stream = this.client.GetStream();
                    byte[] msgData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgDTO));//序列化为Json格式
                    long totalBytes = sizeof(long) + msgData.Length;
                    byte[] totalBytesData = BitConverter.GetBytes(totalBytes);
                    stream.Write(totalBytesData, 0, totalBytesData.Length);//发送总字节数
                    stream.Write(msgData, 0, msgData.Length);//发送消息给主控
                    send = true;
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
            return send;
        }

        private bool ReceiveMsgByMaster(out string msg)
        {
            bool result = false;
            try
            {
                if (this.client.Connected)
                {
                    NetworkStream stream = this.client.GetStream();
                    byte[] longBuffer = new byte[sizeof(long)];
                    long totalBytes = 0;
                    if (stream.Read(longBuffer, 0, longBuffer.Length) == sizeof(long))//读取总字节数
                    {
                        totalBytes = BitConverter.ToInt64(longBuffer, 0);
                        byte[] msgData = new byte[totalBytes - longBuffer.Length];
                        if (stream.Read(msgData, 0, msgData.Length) == msgData.Length)//读取消息
                        {
                            msg = Encoding.UTF8.GetString(msgData);
                            result = true;
                        }
                        else
                        {
                            msg = "消息读取异常";
                        }
                    }
                    else
                    {
                        msg = "总字节数读取异常";
                    }
                }
                else
                {
                    msg = "与主控的连接已断开，无法发送消息！";
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
            }
            return result;
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

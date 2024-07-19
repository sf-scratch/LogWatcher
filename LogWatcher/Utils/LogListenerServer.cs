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
using LogWatcher.ViewModels;
using System.Threading;
using System.Net.Http;
using LogWatcher.Extensions;

namespace LogWatcher.Utils
{
    internal class LogListenerServer
    {
        public event Action<string> PrintMessage;//打印消息事件

        private FileSystemWatcher ngWatcher;//NG文件夹监控

        private FileSystemWatcher passWatcher;//PASS文件夹监控

        private IPAddress masterIPAddress;

        private int masterPort;

        private TcpClient client;

        private bool isAutoReconnection;//是否开启自动重连

        private bool isConnecting;//是否正在创建TCP连接中

        public bool IsAutoReconnection
        {
            get { return isAutoReconnection; }
            set
            {
                bool closeToOpen = !isAutoReconnection && value;
                isAutoReconnection = value;
                if (closeToOpen)
                {
                    PrintMessage?.Invoke("已开启自动重连");
                    if (!this.client.IsOnline())
                    {
                        ConnectToServer("重新连接");
                    }
                }
            }
        }


        public LogListenerServer(string ngPath, string passPath, IPAddress masterIPAddress, int masterPort)
        {
            this.isAutoReconnection = true;
            this.isConnecting = false;
            this.masterIPAddress = masterIPAddress;
            this.masterPort = masterPort;
            this.ngWatcher = new FileSystemWatcher();
            InitWatcher(this.ngWatcher, ngPath);
            this.ngWatcher.Created += new FileSystemEventHandler(NgCreated);
            this.passWatcher = new FileSystemWatcher();
            InitWatcher(this.passWatcher, passPath);
            this.passWatcher.Created += new FileSystemEventHandler(PassCreated);
        }

        public void Start()
        {
            PrintMessage?.Invoke("已开启自动重连");
            ConnectToServer("Hello from Slave");
            PrintMessage?.Invoke($"{this.ngWatcher.Path} 文件监控已经启动...");
            this.ngWatcher.EnableRaisingEvents = true;
            PrintMessage?.Invoke($"{this.passWatcher.Path} 文件监控已经启动...");
            this.passWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            PrintMessage?.Invoke($"{this.ngWatcher.Path} 文件监控已经停止...");
            this.ngWatcher.EnableRaisingEvents = false;
            this.ngWatcher.Dispose();
            this.ngWatcher = null;
            PrintMessage?.Invoke($"{this.passWatcher.Path} 文件监控已经停止...");
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
                PrintMessage?.Invoke($"Error: {ex.Message}");
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
                        PrintMessage?.Invoke(string.Empty);
                        PrintMessage?.Invoke($"{DateUtil.Now}   [NG]    {e.Name}");
                        LogContentParser logParser = new LogContentParser();
                        logParser.LoadLogFile(e.FullPath);
                        List<string> list = logParser.ParseToList();
                        foreach (string line in list)
                        {
                            writer.WriteLine(line);
                            PrintMessage?.Invoke(line);
                        }
                    }
                    if (SendMsgDTOToMaster(builder.ToString().Trim(), MsgType.NG))
                    {
                        string receiveMsg;
                        ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                        PrintMessage?.Invoke(receiveMsg);
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
                    PrintMessage?.Invoke(string.Empty);
                    PrintMessage?.Invoke($"{DateUtil.Now}   [PASS]  {e.Name}");
                    if (SendMsgDTOToMaster("PASS", MsgType.PASS))
                    {
                        string receiveMsg;
                        ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                        PrintMessage?.Invoke(receiveMsg);
                    }
                }
            }
        }

        private async void ConnectToServer(string msg)
        {
            if (!this.isConnecting && this.isAutoReconnection)
            {
                this.isConnecting = true;//是否正在连接中
                do
                {
                    try
                    {
                        this.client?.Close();//关闭之前的TcpClient对象
                        this.client = new TcpClient(AddressFamily.InterNetwork);
                        await this.client.ConnectAsync(this.masterIPAddress, this.masterPort);
                        if (SendMsgDTOToMaster(msg))
                        {
                            string receiveMsg;
                            ReceiveMsgByMaster(out receiveMsg);//阻塞，接收主控的回复消息
                            PrintMessage?.Invoke(receiveMsg);
                            this.isConnecting = false;
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        PrintMessage?.Invoke(e.Message);
                        PrintMessage?.Invoke($"连接主控 {this.masterIPAddress.ToString()}:{this.masterPort} 失败");
                        // 等待一段时间然后重试
                        await Task.Delay(3000);
                    }
                } while (this.isAutoReconnection);

                //走出循环且仍在连接中，则表示关闭自动重连
                if (this.isConnecting)
                {
                    PrintMessage?.Invoke("已关闭自动重连");
                    this.isConnecting = false;
                }
            }
        }

        private bool SendMsgDTOToMaster(string msg)
        {
            return SendMsgDTOToMaster(msg, MsgType.None);
        }

        private bool SendMsgDTOToMaster(string msg, MsgType type)
        {
            bool send = false;
            try
            {
                if (this.client.Connected)
                {
                    MsgDTO msgDTO = new MsgDTO(msg);
                    msgDTO.Type = type;
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
                    PrintMessage?.Invoke("与主控的连接已断开，无法发送消息！");
                    ConnectToServer("重新连接");
                }
            }
            catch (Exception e)
            {
                PrintMessage?.Invoke(e.Message);
                ConnectToServer("重新连接");
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
                    ConnectToServer("重新连接");
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
                ConnectToServer("重新连接");
            }
            return result;
        }
    }
}

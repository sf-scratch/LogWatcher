using LogWatcher.Parsers;
using Prism.DryIoc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                PrintMessage($"新增: {e.Name}");
                LogContentParser logParser = new LogContentParser();
                logParser.LoadLogFile(e.FullPath);
                List<string> list = logParser.ParseToList();
                foreach (string line in list)
                {
                    PrintMessage(line);
                }
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

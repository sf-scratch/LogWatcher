using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace LogWatcher.Parsers
{
    internal class LogContentParser
    {
        private const string FAIL_SIGN = "FAIL";

        private List<DataBlock> blocks;

        public LogContentParser()
        {
            this.blocks = new List<DataBlock>();
        }

        public void LoadLogFile(string filePath)
        {
            // 等待文件可访问
            int numTries = 0;
            while (true)
            {
                try
                {
                    using (FileStream fileStream = File.Open(filePath, FileMode.Open))
                    {
                        LogStructureParser structureParser = new LogStructureParser(fileStream);
                        this.blocks.Clear();
                        this.blocks.AddRange(structureParser.Parse());
                    }
                    break;
                }
                catch (IOException)
                {
                    // 文件正在被写入，等待一段时间然后重试
                    if (++numTries >= 10) // 最多尝试10次
                    {
                        Console.WriteLine($"读取文件 {filePath} 失败 {numTries} 次.");
                        break;
                    }
                    System.Threading.Thread.Sleep(500); // 等待500毫秒
                }
            }
        }

        public List<string> ParseToList()
        {
            List<string> parsedList = new List<string>();
            foreach (DataBlock block in this.blocks)
            {
                int endRowIndex;
                if (CheckFail(block, out endRowIndex))
                {
                    parsedList.Add(block.Title);
                    parsedList.AddRange(GetFailPropertyList(block, endRowIndex));
                }
            }
            return parsedList;
        }

        private List<string> GetFailPropertyList(DataBlock block, int endRowIndex)
        {
            List<string> failList = new List<string>();
            string lastNotEmptyLine = string.Empty;
            using (StringReader reader = new StringReader(block.Content))
            {
                string line = reader.ReadLine();
                for (int i = 0; i < endRowIndex; i++)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        lastNotEmptyLine = line;
                        if (line.Contains(FAIL_SIGN))
                        {
                            failList.Add(line);
                        }
                        line = reader.ReadLine();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //如果匹配不到带有 FAIL_SIGN 的行，则添加最后一个不为空字符串的行
            if (failList.Count == 0)
            {
                failList.Add(lastNotEmptyLine);
            }
            return failList;
        }

        private bool CheckFail(DataBlock block, out int endRowIndex)
        {
            bool isFail = false;
            endRowIndex = -1;
            using (StringReader reader = new StringReader(block.Content))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    endRowIndex++;
                    if (line.Trim() == FAIL_SIGN)
                    {
                        isFail = true;
                        break;
                    }
                    line = reader.ReadLine();
                }
            }
            return isFail;
        }
    }
}

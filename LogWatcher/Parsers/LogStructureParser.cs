using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace LogWatcher.Parsers
{
    internal class LogStructureParser
    {
        public LogStructureParser(FileStream logStream)
        {
            this.logStream = logStream;
        }

        private static string NegativeSignSplit = "-------------------------------------------------------------------------------";

        private FileStream logStream;

        public FileStream LogStream
        {
            get { return logStream; }
            set { logStream = value; }
        }

        /// <summary>
        /// 解析fileContent的内容
        /// </summary>
        /// <returns></returns>
        public List<DataBlock> Parse()
        {
            List<DataBlock> dataBlocks = new List<DataBlock>();
            using (StreamReader reader = new StreamReader(logStream))
            {
                string line = reader.ReadLine();
                string preLine = string.Empty;
                int curNumber = 0;

                while (line != null)
                {
                    if (CheckIsHead(curNumber, preLine, line))
                    {
                        DataBlock block = new DataBlock();
                        int pointIndex = preLine.IndexOf('.');
                        block.Title = preLine.Substring(pointIndex + 1, preLine.Length - (pointIndex + 1)).Trim();
                        SetNumber(block, preLine.Substring(0, pointIndex));
                        curNumber = block.Number;
                        SetContentAndStatus(block, reader, curNumber);
                        dataBlocks.Add(block);
                    }
                    preLine = line;
                    line = reader.ReadLine();
                }
            }

            return dataBlocks;
        }

        private void SetContentAndStatus(DataBlock block, StreamReader reader, int curNumber)
        {
            if (reader != null)
            {
                StringBuilder builder = new StringBuilder();
                string line = reader.ReadLine();
                while (line != null && !CheckIsTail(line))
                {
                    SetStatus(block, line);
                    builder.AppendLine(line);
                    line = reader.ReadLine();
                }
                if (line != null)//如果line不为null，则表示line为行尾，需要添加进Content中
                {
                    builder.AppendLine(line);
                }
                string content = content = builder.ToString();
                ASCIIEncoding encoding = new ASCIIEncoding();//解决乱码问题
                content = encoding.GetString(encoding.GetBytes(content));
                block.Content = content;
            }
        }

        private void SetStatus(DataBlock block, string line)
        {
            if (line.Trim() == (TestStatus.PASS.ToString()))
            {
                block.Status = TestStatus.PASS;
            }
            else if (line.Trim() == (TestStatus.FAIL.ToString()))
            {
                block.Status = TestStatus.FAIL;
            }
        }

        private bool CheckIsHead(int curNumber, string preLine, string line)
        {
            bool isHead = false;
            if (line.Equals(NegativeSignSplit))
            {
                int number;
                if (preLine.Length > 3 && int.TryParse(preLine.Substring(0, 2).Trim(), out number))
                {
                    isHead = number == curNumber + 1 && preLine[2] == '.';
                }
            }
            return isHead;
        }

        private bool CheckIsTail(string line)
        {
            string pattern = @"Test Time: \d+\.\d+? sec";
            Match match = Regex.Match(line, pattern);
            return match.Success;
        }

        private void SetNumber(DataBlock block, string numString)
        {
            int number;
            if (!int.TryParse(numString, out number))
            {
                Console.WriteLine("ParseNumber出错");
            }
            block.Number = number;
        }
    }
}

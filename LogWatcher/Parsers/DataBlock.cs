using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogWatcher.Parsers
{
    internal class DataBlock
    {
        public DataBlock()
        {
            number = 0;
            title = string.Empty;
            content = string.Empty;
            status = TestStatus.None;
        }
        
        private int number;

        /// <summary>
        /// 序号
        /// </summary>
        public int Number
        {
            get { return number; }
            set { number = value; }
        }


        
        private string title;

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string content;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        private TestStatus status;

        /// <summary>
        /// 测试结果状态
        /// </summary>
        public TestStatus Status
        {
            get { return status; }
            set { status = value; }
        }

    }
}

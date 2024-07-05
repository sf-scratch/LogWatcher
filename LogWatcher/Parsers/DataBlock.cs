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
        }

        /// <summary>
        /// 序号
        /// </summary>
        private int number;

        public int Number
        {
            get { return number; }
            set { number = value; }
        }


        /// <summary>
        /// 标题
        /// </summary>
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; }
        }


        /// <summary>
        /// 内容
        /// </summary>
        private string content;

        public string Content
        {
            get { return content; }
            set { content = value; }
        }
    }
}

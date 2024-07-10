using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogWatcher.DTOs
{
    public class MsgDTO
    {
        public static readonly string DefaultHostNumber = ConfigurationManager.AppSettings["DefaultHostNumber"];

        public MsgDTO(string msg)
        {
            this.Msg = msg;
        }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 主机号
        /// </summary>
        public string HostNumber { get; set; } = DefaultHostNumber;

        /// <summary>
        /// 发送方IP地址
        /// </summary>
        public string SenderAddress { get; set; }

        /// <summary>
        /// 发送方端口号
        /// </summary>
        public int SenderPort { get; set; }
    }
}

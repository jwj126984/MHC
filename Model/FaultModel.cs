using System;
using System.Collections.Generic;

namespace MHC.Model
{
    /// <summary>
    /// 故障数据模型
    /// </summary>
    public class FaultModel
    {
        /// <summary>
        /// 故障发生时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 故障名称位置
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 故障种类类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 采样故障具体值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 故障发生时所有信号的值
        /// </summary>
        public Dictionary<string, double> SignalValues { get; set; }

        /// <summary>
        /// 故障信号映射（信号名 -> 故障描述）
        /// </summary>
        public Dictionary<string, string> FaultSignals { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="time">故障发生时间</param>
        /// <param name="location">故障名称位置</param>
        /// <param name="type">故障种类类型</param>
        /// <param name="value">采样故障具体值</param>
        public FaultModel(DateTime time, string location, string type, string value = "")
        {
            Time = time;
            Location = location;
            Type = type;
            Value = value;
            SignalValues = new Dictionary<string, double>();
            FaultSignals = new Dictionary<string, string>();
        }

        /// <summary>
        /// 构造函数（带信号值和故障信号）
        /// </summary>
        /// <param name="time">故障发生时间</param>
        /// <param name="location">故障名称位置</param>
        /// <param name="type">故障种类类型</param>
        /// <param name="value">采样故障具体值</param>
        /// <param name="signalValues">所有信号值字典</param>
        /// <param name="faultSignals">故障信号映射</param>
        public FaultModel(DateTime time, string location, string type, string value, Dictionary<string, double> signalValues, Dictionary<string, string> faultSignals = null)
        {
            Time = time;
            Location = location;
            Type = type;
            Value = value;
            SignalValues = signalValues ?? new Dictionary<string, double>();
            FaultSignals = faultSignals ?? new Dictionary<string, string>();
        }
    }
}
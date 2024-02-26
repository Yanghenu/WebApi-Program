using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzExtensions
{
    public enum QuartzJobType
    {
        /// <summary>
        ///通过间隔时间来运行
        /// </summary>
        WithIntenal = 1,

        /// <summary> 
        /// 通过cron公式来计算运行时间
        /// </summary>
        WithCron = 2
    }

    public enum JobState
    {
        Start = 1,
        Stop = 0,
    }
}

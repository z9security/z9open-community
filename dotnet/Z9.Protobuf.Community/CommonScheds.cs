using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class CommonScheds
    {
        public static Sched EXPLICIT_24_7()
        {
            Sched sched = new Sched();
            SpCoreProtoUtil.InitRequired(sched);
            SchedElement se1 = new SchedElement();
            SpCoreProtoUtil.InitRequired(se1);
            se1.SchedDays.Add(SchedDay.Sun);
            se1.SchedDays.Add(SchedDay.Mon);
            se1.SchedDays.Add(SchedDay.Tues);
            se1.SchedDays.Add(SchedDay.Wed);
            se1.SchedDays.Add(SchedDay.Thur);
            se1.SchedDays.Add(SchedDay.Fri);
            se1.SchedDays.Add(SchedDay.Sat);
            se1.Holidays = true;
            se1.Start = SpCoreProtoUtil.ToSqlTimeData(0, 0, 0);
            se1.Stop = SpCoreProtoUtil.ToSqlTimeData(23, 59, 59);
            se1.PlusDays = 0;
            sched.Name = "24/7";
            sched.Elements.Add(se1);
            
            return sched;
        }
    }
}

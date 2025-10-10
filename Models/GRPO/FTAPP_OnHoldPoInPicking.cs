using System;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class FTAPP_OnHoldPoInPicking
    {
        public int id { get; set; }
        public int HoldPONum { get; set; }
        public string HoldByUserCode { get; set; }
        public string HoldByUserName { get; set; }
        public DateTime HoldStartDt { get; set; }
        public string HoldReason { get; set; }
    }
}

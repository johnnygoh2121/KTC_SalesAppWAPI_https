using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class FTAPP_OnHoldSoInPicking
    {
        public int id { get; set; }
        public int HoldDocEntry { get; set; }
        public string HoldByUserCode { get; set; }
        public string HoldByUserName { get; set; }
        public DateTime HoldStartDt { get; set; }
        public string HoldReason { get; set; }
    }
}

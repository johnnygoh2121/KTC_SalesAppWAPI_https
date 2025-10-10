using System;

namespace KTC_SalesAppWAPI.Models.Pick_IBT
{
    public class FTAPP_OnHold_IBT_InPicking
    {
        public int id { get; set; }
        public int HoldDocEntry { get; set; }
        public string HoldByUserCode { get; set; }
        public string HoldByUserName { get; set; }
        public DateTime HoldStartDt { get; set; }
        public string HoldReason { get; set; }
    }
}

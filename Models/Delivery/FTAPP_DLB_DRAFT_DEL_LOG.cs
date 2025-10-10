using System;


namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DLB_DRAFT_DEL_LOG
    {
        public int id { get; set; }
        public Guid HeadGuid { get; set; }
        public DateTime TransDt { get; set; }
        public string DriverName { get; set; }
        public string TruckNo { get; set; }
    }
}

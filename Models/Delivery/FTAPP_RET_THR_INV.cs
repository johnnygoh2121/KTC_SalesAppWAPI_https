using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_RET_THR_INV
    {
        public string SubSi { get; set; }
        public int id { get; set; }
        public string DocStatus { get; set; }
        public int DlbEntry { get; set; }
        public int InvNum { get; set; }
        public int InvEntry { get; set; }
        public DateTime TransDt { get; set; }
        public int ItemCount { get; set; }
        public int BoxCount { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public string TruckNo { get; set; }
        public string DriverName { get; set; }
        public string Transporter { get; set; }

        public string Reason { get; set; }

    }
}

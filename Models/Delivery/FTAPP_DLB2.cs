using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DLB2
    {
        public int id { get; set; }
        public int InvDocNum { get; set; }
        public string BoxId { get; set; }
        public DateTime OutTransDt { get; set; }
        public DateTime InTransDt { get; set; }
        public int SoDocEntry { get; set; }
        public int DlbEntry { get; set; }
        public Guid HeadGuid { get; set; }

    }
}

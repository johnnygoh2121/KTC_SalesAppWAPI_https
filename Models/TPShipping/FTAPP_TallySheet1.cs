using System;

namespace KTC_SalesAppWAPI.Models.TPShipping
{
    public class FTAPP_TallySheet1
    {
        public int id { get; set; }
        public Guid HeadGuid { get; set; }
        public Guid LineGuid { get; set; }
        public int CtnNo { get; set; }
        public int OrderNo { get; set; }
        public int SoDocEntry { get; set; }
        public string OrderDate { get; set; }
        public string Studio { get; set; }
        public string ShippingCartonNo { get; set; }
        public string PackedId { get; set; }

        public int InvNo { get; set; }
        public string ScanInCode { get; set; }

        // add on property
        public string BoxId { get; set; }
        public string OrigOrderNo { get; set; }
        public string OrderType { get; set; }
        public string ItemCode { get; set; }

    }
}

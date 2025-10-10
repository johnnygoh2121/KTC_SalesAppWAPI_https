using System;

namespace KTC_SalesAppWAPI.Models.Pack
{
    public class TP_PackedBoxInfo
    {
        public string REFORDER { get; set; }
        public DateTime OrderDate { get; set; }
        public string StudioCode { get; set; }
        public string OrderNo { get; set; }
        public int SoDocEntry { get; set; }
        public string BizCenterCode { get; set; }
        public int CartonCount { get; set; }
        public string PackedId { get; set; }
        public int InvNo { get; set; }
        public string ScanInCode { get; set; }

        public Guid LineGuid { get; set; } = Guid.NewGuid();
        public Guid HeadGuid { get; set; }
        public string ShippingCartonNo { get; set; }

        public string ODRTYPE { get; set; }

    }
}

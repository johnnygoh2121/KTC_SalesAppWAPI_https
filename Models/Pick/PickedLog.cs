using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class PickedLog
    {
        public int Id { get; set; }
        public DateTime TransDt { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string AuthUserCode { get; set; }
        public string AuthUserName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string CodeBars { get; set; }
        public decimal NeededQty { get; set; }
        public decimal NeededQtyInPc { get; set; }
        public decimal NeededQtyInCs { get; set; }
        public decimal PickedQty { get; set; }
        public decimal PickedQtyInPcs { get; set; }
        public decimal PickedQtyInCs { get; set; }
        public string Uom { get; set; }
        public string ReportAs { get; set; }
        public string WhsName { get; set; }
        public string Subsi { get; set; }
        public string Branch { get; set; }
        public string BranchCode { get; set; }
        public string AgencyName { get; set; }
        public string AgencyCode { get; set; }
        public int BaseEntry { get; set; }
        public int BaseLine { get; set; }
        public int DocNum { get; set; }
        public string StickerNum { get; set; }
        public string AppVersion { get; set; }

        public int LabelConsistTotalBoxes { get; set; } // 20230414
    }
}

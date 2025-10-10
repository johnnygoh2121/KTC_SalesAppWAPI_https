using System;

namespace KTC_SalesAppWAPI.Models.TPWhsRet
{
    public class FTAPP_COG1
    {
        public int id { get; set; }
        public int CogDocEntry { get; set; }
        public int CogBaseLine { get; set; }
        public int LineNum { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string ReasonCode { get; set; }
     
        public decimal Quantity { get; set; }
        public decimal QuantityCs { get; set; }
        public decimal QuantityPc { get; set; }
        public string Remarks { get; set; }
        public DateTime? ExpDate { get; set; }
        public DateTime ?MfrDate { get; set; }
        public string LotNo { get; set; }
        public string Batch { get; set; }
        public Guid LineGuid { get; set; }
        public string ScanInCode { get; set; }
        public int UomQty { get; set; }
        public string BarcodeStr { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal VarianceQty { get; set; }
        public decimal CogIssueQty { get; set; }
        public string RecWhsCode { get; set; }
        public string RecWhsName { get; set; }
        public string RecReasonCode { get; set; }
    }
}

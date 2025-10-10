using System;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class TrCNTracking
    {
        public int DocEntry { get; set; }
        public int DocNum  { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public int Ret_Duration_Day { get; set; }
        public string CardName { get; set; }
        public string CardCode { get; set; }        
        public string ItemCode { get; set; }
        public string CodeBars { get; set; }
        public string MfrCode { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal RtnQty { get; set; }
        public decimal Varient { get; set; }
        public string IssuerCode { get; set; }
        public string IssuerName { get; set; }
        public string SenderCode { get; set; }
        public string SenderName { get; set; }
        public string ReceiverCode { get; set; }
        public string ReceiverName { get; set; }
        public string Reason { get; set; }
        public string WhsCode { get; set; }
        public string Remarks { get; set; }
        public string Informed_HR { get; set; }
        public DateTime Informed_HR_Date { get; set; }

        public int GIDocNum { get; set; }
        public int GILineNum { get; set; }
        public decimal GIQty { get; set; }
    }
}

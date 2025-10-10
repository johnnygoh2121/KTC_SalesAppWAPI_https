using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class AgedDoc
    {
        public string DocBarCode { get; set; }
        public string DocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal DocTotal { get; set; }
        public int DayAged { get; set; }
        public string WhsCode { get; set; }
        public DateTime  DocDate { get; set; }
        public string BranchCode { get; set; }
        public string BrachName { get; set; }
        public int TerritryID { get; set; }
        public string TerritryName { get; set; }
        public string DLBStatus { get; set; }
        public string DocType { get; set; } // i for invoice, T for ibt 

        public int TransferNum { get; set; }
        public int RequestNum { get; set; }
        public string LastStatus { get; set; }
    }
}

using System;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class PortalDocument
    {
        public int transId { get; set; }
        public int transLine { get; set; }
        public DateTime documentDate { get; set; }
        public string documentTypeDesc { get; set; }
        public string docNum { get; set; }
        public double balanceAmount { get; set; }
        public int docEntry { get; set; }
        public double balanceAmountFC { get; set; }
        public string numatCard { get; set; }
        public string collector { get; set; }
        public string transType { get; set; }
        public string objectcode { get; set; }
        public string currency { get; set; }

        // for app 
        public decimal PartialAmt { get; set; } // user enter the amount
        public bool IsPaidPartial { get; set; }
    }
}

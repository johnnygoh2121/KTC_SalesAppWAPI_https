using System;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class Document
    {
        public string SubSi { get; set; }

        // original
        public int docentry { get; set; }
        public int linenum { get; set; }
        public int transid { get; set; }
        public int transline { get; set; }
        public int sourceid { get; set; }
        public string sourcetype { get; set; }
        public DateTime sourcedate { get; set; }
        public int sourcedoc { get; set; }
        public string sourceref { get; set; }
        public double sourceamt { get; set; }
        public double appliedamt { get; set; }
        public double sourceamtfc { get; set; }
        public string objectcode { get; set; }

        public string basedocnum { get; set; } // for non ktc store

        // for app
        public string headGuid { get; set; }
        public string lineGuid { get; set; }
        public decimal PartialAmt { get; set; } // user enter the amount
        public bool IsPaidPartial { get; set; }
    }
}

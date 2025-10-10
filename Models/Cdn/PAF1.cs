using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class PAF1 // for PAF with charge code
    {
        public int DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public int CHRGENTRY { get; set; }
        public string BRAND { get; set; }
        public string CHRGCODE { get; set; }
        public string PAFTYPE { get; set; }
        public string FUNDTYPE { get; set; }
        public decimal BALANCE { get; set; }
        public decimal AMOUNT { get; set; }
        public decimal SUPP { get; set; }
        public decimal OWN { get; set; }
        public string REMARKS { get; set; }
        public string TAXCODE { get; set; }
        public string SKUNAME { get; set; } //SKU Name
        public decimal REBATE { get; set; } //Rebate
        public decimal QTY { get; set; } //Quantity

        public string CHARGECODECATEGORY { get; set; }
        public DateTime? FROMDATE { get; set; }
        public DateTime? TODATE { get; set; }

        //public string  FROMDATEDISPLAY
        //{
        //    get
        //    {
        //        if (FROMDATE != null) return $"{FROMDATE:dd-MMM-yy}";
        //        return "";
        //    }
        //}
        //public string TODATEDISPLAY
        //{
        //    get
        //    {
        //        if (TODATE != null) return $"{TODATE:dd-MMM-yy}";
        //        return "";
        //    }
        //}


        // CHARGECODECATEGORY, FROMDATE, TODATE  
    }
}

using System;

namespace KTC_SalesAppWAPI.Models.Cdn
{
    public class Gondola
    {
        // app usage
        public string Subsi { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }
        // orginal
        public int DOCENTRY { get; set; }
        public int DOCNUM { get; set; }
        public string DOCSTATUS { get; set; }
        public DateTime DOCDATE { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string AGENCY { get; set; }
        public string GONTYPE { get; set; }
        public string GONNO { get; set; }
        public string REFNO { get; set; }
        public string REMARKS { get; set; }
        public string GONFILE { get; set; }
        public string REFFILE { get; set; }
        public string BRAND { get; set; }
        public decimal AMOUNT { get; set; }
        public decimal SUPP { get; set; }
        public decimal OWN { get; set; }
        public int CNNO { get; set; }
        public int APCNNO { get; set; }
        public string APPRREM { get; set; }
        public DateTime STARTDATE { get; set; }
        public DateTime ENDDATE { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DCREATED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string REQT { get; set; }
        public string REQU { get; set; }
        public DateTime REQDATE { get; set; }
        public string REQREM { get; set; }
        public DateTime POSTDATE { get; set; }
        public string INVNO { get; set; }
        public DateTime INVDATE { get; set; }
    }
}

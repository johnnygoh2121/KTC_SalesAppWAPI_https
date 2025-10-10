using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_OINV_Ext
    {
        public string Subsi { get; set; }
        public string SubsiId { get; set; }

        public long DOCENTRY { get; set; }
        public string DOCSTATUS { get; set; }
        public string DOCNUM { get; set; }
        public string COMPANYID { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public DateTime DOCDATE { get; set; }
        public string CURRENCY { get; set; }
        public decimal DOCRATE { get; set; }
        public string CUSTREF { get; set; }
        public string BILLADD1 { get; set; }
        public string BILLADD2 { get; set; }
        public string BILLADD3 { get; set; }
        public string BILLADD4 { get; set; }
        public string BILLADD5 { get; set; }
        public string TEL { get; set; }
        public string FAX { get; set; }
        public string CONTACT { get; set; }
        public decimal TOTALBD { get; set; }
        public decimal TAXSUM { get; set; }
        public decimal ROUNDING { get; set; }
        public decimal DOWNPAYMENT { get; set; }
        public decimal DOCTOTAL { get; set; }
        public int PRICEID { get; set; }
        public string REMARKS { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public decimal PAIDTODATE { get; set; }
        public int INVENTRY { get; set; }
        public int CNENTRY { get; set; }
        public string SAPINV { get; set; }
        public string APPR { get; set; }
        public string APPRUSER { get; set; }
        public DateTime APPRDATE { get; set; }
        public string APPRREM { get; set; }
        public string HOLREM { get; set; }
        public string HOLDREM { get; set; }

        public int LinesCount { get; set; }
        public int SAPInvDocNum { get; set; }

        public List<Bread_INV1_Ext> Lines { get; set; }
        public List<Bread_Batch> Batches { get; set; }

        // for app use
        
        public int SapCnDocNum { get; set; }

        public string FILES { get; set; }
    }
}

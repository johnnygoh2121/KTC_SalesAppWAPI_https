using KTC_SalesAppWAPI.Models.BreadReturn;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_CN_Ext
    {
        // for app
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        // for cn return
        public string OwnerCode { get; set; }
        public string OwnerName { get; set; }

        public int LinesCount { get; set; }
        public int SapCnDocNum { get; set; }
        public int SAPInvDocNum { get; set; }
        public int SAPGIDocNum { get; set; }
        public string IsKTCStore { get; set; } // doc level

        // orig
        public long DOCENTRY { get; set; }
        public string DOCSTATUS { get; set; }
        public string BASEDOCNUM { get; set; }
        public string DOCNUM { get; set; }
        public string COMPANYID { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public DateTime DOCDATE { get; set; }
        public DateTime BASEDOCDATE { get; set; }
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
        public string REASON { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public decimal PAIDTODATE { get; set; }
        public int INVENTRY { get; set; }
        public int CNENTRY { get; set; }
        public string SAPINV { get; set; }
        public string FILES { get; set; }

        public List<Bread_CN1_Ext> Lines { get; set; } // for cn lines
        public List<Bread_Batch> Batches { get; set; }
        public List<TrcnLineDetails> LineDetails { get; set; }

        public string TransporterCode { get; set; }
        // for whs return         
        public string ReceiverCode { get; set; } // for whs app to do received 
        public string ReceiverName { get; set; }
        public DateTime ReceivedDt { get; set; }

        public string IsTrcnReturned { get; set; } // indicate this cn is returned Y or not N

    }
}

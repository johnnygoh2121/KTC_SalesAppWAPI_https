using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class DLB1
    {
        // for app 
        public DateTime CheckinDt { get; set; }
        public string Subsi { get; set; }
        public string SubsiId { get; set; }

        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string DOCTYPE { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public decimal DOCTOTAL { get; set; }
        public string TERRITORY { get; set; }
        public string GEOCODE { get; set; }
        public int TOTALPAGES { get; set; }
        public int CARTONNO { get; set; }
        public string REFNO { get; set; }
        public string STATUS { get; set; }
        public DateTime RETDATE { get; set; }
        public string PAGES { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public DateTime RECDATE { get; set; }
        public string CONSIGNMENTNO { get; set; }

        public string ReportReturnThruInvStatus { get; set; }
        public string OutTransitStatus { get; set; }

        public string SignedFiles { get; set; }

        // for indicate this is rescan entry
        public bool IsReScan { get; set; } // for add in rescan feature indicator

        public string IsAllowDlbReScan { get; set; } // for dlb rescan
    }
}

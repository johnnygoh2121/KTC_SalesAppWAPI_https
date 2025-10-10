using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Pick_IBT
{
    public class IBT
    {
        // for app usage 
        public string Subsi { get; set; }
        public int LineCount { get; set; }

        // for app posting 
        public List<IBT1> Lines { get; set; }

        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCTYPE { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string WHSCODE { get; set; }
        public int BRAND { get; set; }
        public string REMARKS { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string APPRREM { get; set; }
        public int PONO { get; set; }
        public string UCREATED { get; set; }
        public DateTime DCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string FROMWHS { get; set; }
        public string WHSTRANSIT { get; set; }
        public int TRANSITNO { get; set; }
        public int TRANSITNO2 { get; set; }
        public string TRFBY { get; set; }
        public DateTime TRFDATE { get; set; }
        public string RECBY { get; set; }
        public DateTime RECDATE { get; set; }

      

    }
}

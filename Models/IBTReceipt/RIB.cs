using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.IBTReceipt
{
    public class RIB
    {


        // for app display 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public int LineCount { get; set; }
        public string DocStatusDisplay { get; set; }
        public List<RIB1> Lines { get; set; }

        public int IBTDocEntry { get; set; }

        // orig portal table properties
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string REFNO { get; set; }
        public string WHSCODE { get; set; }
        public string TOWHS { get; set; }
        public string REMARKS { get; set; }
        public int POSTENTRY { get; set; }
        public DateTime POSTDATE { get; set; }
        public string APPRREM { get; set; }
        public int APPRLEVEL { get; set; }
        public int CURRLEVEL { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DCREATED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string BOP { get; set; }
    }
}

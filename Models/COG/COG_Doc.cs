using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class COG_Doc
    {
        // for app display
        public string SubSi { get; set; }
        public int CnDocNum { get; set; } // after post
        public int CnEntry { get; set; }// after post


        // added for cog linking to delivery app 
        
        public DateTime CnTransDt { get; set; }
        public int RtnEntry { get; set; }

        public string OwnerName { get; set; }
        public string OwnerCode { get; set; }
        public int LinesCnt { get; set; }
        public decimal DocTotal { get; set; }
        public string AgencyName { get; set; }

        public int IsReturned { get; set; }


        // orig cog field
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public DateTime? DOCDATE { get; set; } = null;// collection date
        public string DOCSTATUS { get; set; }
        public string COLTYPE { get; set; } // collection type 
        public string CARDCODE { get; set; } // custimer code
        public string CARDNAME { get; set; }
        public string SHIPCODE { get; set; }
        public string SHIPADD { get; set; }
        public string COGNO { get; set; } // require when coltyp is personal
        public string REFNO { get; set; }
        public string REMARKS { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime? DCREATED { get; set; } = null;
        public DateTime? DMODIFIED { get; set; } = null;
        public string DELUSER { get; set; }
        public DateTime? DELDATE { get; set; } = null;
        public string LASTINVREM { get; set; }
        public string GSTREM { get; set; }
        public string UPLD { get; set; }
        public string REFTYPE { get; set; }
        public string REFCARD { get; set; }
        public string AVONACK { get; set; }
        public DateTime AVACKDATE { get; set; }
        public string TPTYPE { get; set; }


        // cog lines load separately
        public List<COG_Line> LINES { get; set; }

        public string LorryNo { get; set; }
        public string Driver { get; set; }
        public string Transporter { get; set; }

        public int LastDLBEntry { get; set; } // use for rescan dlb

    }
}

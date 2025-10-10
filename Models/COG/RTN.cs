using System;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class RTN
    {
        // app
        public int IS_WHS_RECEIPT { get; set; } = 0; // default as yet receipt 
        public DateTime WHS_RECEIPT_DT { get; set; } // indicate the date of whs receipt 
        public string WHS_USER_CODE { get; set; } = ""; // indicate the whs user 
        public long CNDOCNUM { get; set; } // for the cn docnum 
        public long CNENTRY { get; set; }

        public long ITDOCNUM { get;  set; }


        // orig portal column
        public long DOCENTRY { get; set; }
        public long DOCNUM { get; set; }
        public long BASEDOCNUM { get; set; }
        public DateTime DOCDATE { get; set; }
        public string DOCSTATUS { get; set; }
        public string DOCTYPE { get; set; }
        public string CARDCODE { get; set; }
        public string CARDNAME { get; set; }
        public string SHIPADD { get; set; }
        public string COLTYPE { get; set; }
        public string COGNO { get; set; }
        public string REFNO { get; set; }
        public string REMARKS { get; set; }
        public int CMENTRY { get; set; }
        public string UCREATED { get; set; }
        public string UMODIFIED { get; set; }
        public DateTime DCREATED { get; set; }
        public DateTime DMODIFIED { get; set; }
        public string LASTINVREM { get; set; }
        public string GSTREM { get; set; }
        public string SIGNDOC { get; set; }
        public string ITEMDOC { get; set; }
        public string SALESPERSON { get; set; }
        public int GRACEPERIOD { get; set; }
        public string DRIVER { get; set; }
        public string LORRYNO { get; set; }
        public string TRANSPORTER { get; set; }

    }
}

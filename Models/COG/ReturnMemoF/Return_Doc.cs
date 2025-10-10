using KTC_SalesAppWAPI.Models.Batches;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.COG.ReturnMemoF
{
    public class Return_Doc
    {
        public int Id { get; set; }
        // for app display
        public string SubSi { get; set; }
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

        // for ease access 
        public string CN_DocNum { get; set; }
        public decimal CN_Total { get; set; }
        public string Currency { get; set; }
        public string AgencyName { get; set; }

        public string ITEMDOC { get; set; }
        public string SIGNDOC { get; set; }
        public int Graceperiod { get; set; }
        public int IsReturned { get; set; } // for determined the trcn is returned 

        public int IsCharged { get; set; } // for indicating is charged , 20220527

        public decimal HrChargedAmt { get; set; }

        // added 20220223
        public List<Return_Line> Lines { get; set; }
        public List<FTAPP_Batch> Batches { get; set; }

        // 20220614
        public string BaseOnDocType { get; set; }

        public int DLBEntry { get; set; } // 20220615

        public bool IsStandAloneCn { get; set; } // 20230614
        public string DriverName { get; set; }  // 20230614

        // 20250919
        public decimal DiscPrcnt { get; set; }

        public decimal DiscSumSy { get; set; }

    }
}

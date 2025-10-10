using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_DLB
    {
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public int id { get; set; }
        public string WhsUserCode { get; set; }
        public string WhsUserName { get; set; }
        public DateTime OutTransDt { get; set; }
        public string TruckNo { get; set; }
        public string TruckCardCode { get; set; }
        public string TruckCardName { get; set; }
        public Guid HeadGuid { get; set; }
        public string Remarks { get; set; }
        public string DriverName { get; set; }
        public int DLBEntry { get; set; }

        public string DLBStatus { get; set; }

        public string SaveAs { get; set; } // indicate the svr to save, N = new / delete insert, E/ U = Update, D = Delete 

        public List<FTAPP_DLB1> Docs { get; set; }

        public string SignedFile { get; set; }
        public string NRIC { get; set; }
        public string SiteId { get; set; } // BOP from the DLB table 
        public DateTime PostedDt { get; set; }

        public string NRICFile { get; set; }

        // for dlb draft / multiple save drraft 
        public bool IsReScan { get; set; } // true = indicate this reco is rescan record 

        public int DocCount { get; set; }  // 20230516, for display the line count 

        // 20250808
        public bool IsInterbranch { get; set; }

    }
}

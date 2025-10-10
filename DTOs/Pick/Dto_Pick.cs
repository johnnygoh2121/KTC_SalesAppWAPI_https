using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Pick
{
    public class Dto_Pick
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        //public string Warehouse { get; set; }
        //public DateTime StartDate { get; set; }
        //public DateTime EndDate { get; set; }
        public string DocStatus { get; set; }
        public int DocEntry { get; set; }
        public string WildCardDocEntry { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserPCode { get; set; }
        
        // for picked doc posting 
        public SO PickedDoc { get; set; }
        public string CompanyId { get; set; }
        public string DocUpdateType { get; set; }
        public string RequestName { get; set; }
        public string QueryKeys { get; set; }
        public string SaveAsDraft { get; set; }

        public string WhsCode { get; set; } // for so line query 
        public string ItemCode { get; set; }

        // boxes and box content
        public List<FTAPP_Box> Boxes { get; set; }
        public List<FTAPP_Batch> Batches { get; set; }  // 20250417

        public PickedLog Log { get; set; }

        public string HoldReason { get; set; }

        public string CardCode { get; set; }
        public int LineNum { get; set; }
        public string Packaging  { get; set; }

        public string InvoiceNo { get; set; }

        public string AppVersion { get; set; }

        public string IsAutoStartSOTestDb { get; set; }  
        
        public string UserToken { get; set; } // 20240521

        public int [] ResetSoDocEntries { get; set; } // 20241023

        public string PickingMode { get; set; } // 20250418
    }
}

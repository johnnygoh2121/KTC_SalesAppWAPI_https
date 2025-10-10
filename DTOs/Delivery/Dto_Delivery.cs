using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Delivery
{
    public class Dto_Delivery
    {
        public string Request { get; set; }
        public string Plate { get; set; }
        public string Company { get; set; }

        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public string InvNum { get; set; }
        public string IbtNum { get; set; }
        public string DocNum { get; set; }
        public bool IsReset { get; set; }

        // save out box - single line 
        public FTAPP_DLB2 Dbl2 { get; set; } // for box
        public List<FTAPP_DLB1> Dlb1 { get; set; } //  
        public FTAPP_DLB Dlb { get; set; } // for invoice  head

        public string DriverName { get; set; }
        public string PlateNo { get; set; }

        public Guid DraftHeadGuid { get; set; }
        public Guid SaveHeadGuid { get; set; }

        public string BoxId { get; set; }

        public List<OINV> DriverInvoice { get; set; }
        
        public string DocType { get; set; }

        public Guid HeadGuid { get; set; }

        public FTAPP_DLB1 SaveDLB1Line { get; set; }

        public string Remarks { get; set; }
        public string Nric { get; set; }

        public string Password { get; set; }

        public string TruckNo { get; set; }

        //20240131
        // for force pick up 
        public string WhsCode { get; set; } 

        // 20240202
        public bool IsAgedInvoice { get; set; }

        // 20240405 
        public string UserToken { get; set; } // to prevent duplicated posting

        // 20250808
        public bool IsInterbranch { get; set; }
 
    }
}

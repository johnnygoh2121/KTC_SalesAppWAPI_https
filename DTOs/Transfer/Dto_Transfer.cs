using KTC_SalesAppWAPI.Models.Transfer;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Transfer
{
    public class Dto_Transfer
    {
        public string Request { get; set; }
        public string ScanInCode { get; set; }   
        
        public string Subsi { get; set; }

        public FTAPP_Transfer2 Box { get; set; }
        public List<FTAPP_Transfer1> TransferInvoices { get; set; }
        public FTAPP_Transfer TransferHead { get; set; }
        public Guid SaveGuid { get; set; }
        public int InvNum { get; set; }

        public string LocationName { get; set; }
        public string ReceiverCode { get; set; }
        
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
    }
}

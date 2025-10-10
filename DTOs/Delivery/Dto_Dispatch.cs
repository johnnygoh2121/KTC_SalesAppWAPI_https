using KTC_SalesAppWAPI.Models.Delivery;
using System;

namespace KTC_SalesAppWAPI.DTOs.Delivery
{
    public class Dto_Dispatch
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string TruckNo { get; set; }
        public string UserCode { get; set; }
        public DateTime DlvryDate { get; set; }

        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public string CardName { get; set; }
        public string InvNum { get; set; }
        public string TransferNum { get; set; }
        public string TransferDocNum { get; set; }
        public long InvDocEntry { get; set; }
        public long DlbEntry { get; set; }
        public string BoxId { get; set; }
        public string DriverName { get; set; }
        public string SignFiles { get; set; }

        public FTAPP_DriverLog Log { get; set; }
        public FTAPP_RET_THR_INV RetInv { get; set; }

        public Guid HeadGuid { get; set; }
        public int InvDocNum { get; set; }

        public int SoDocEntry { get; set; }

        // for signed files 
        public string SignedFiles { get; set; }
        public long DocNum { get; set; }
        public string DocType { get; set; }

        public long CogNum { get; set; }
        public long CogDocEntry { get; set; }

        public bool IsCreateTransfer { get; set ; }

        public string AttchmentStatus { get; set; }



    }
}

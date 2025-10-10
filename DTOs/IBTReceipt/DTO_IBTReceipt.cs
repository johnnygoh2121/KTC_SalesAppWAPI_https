using KTC_SalesAppWAPI.Models.IBTReceipt;
using System;

namespace KTC_SalesAppWAPI.DTOs.IBTReceipt
{
    public class DTO_IBTReceipt
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public int TransferDocNum { get; set; }
        public int DocEntry { get; set; }

        public RIB IBTReceipt { get; set; }
        public string QueryKeys { get; set; }

        public int ReqDocNum { get; set; }

        public int RibDocEntry { get; set; }
        public int IbtEntry { get; set; }
    }
}

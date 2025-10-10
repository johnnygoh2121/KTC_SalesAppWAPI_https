using KTC_SalesAppWAPI.Models.GRPO;
using System;

namespace KTC_SalesAppWAPI.DTOs.Grpo
{
    public class Grpo_Dto
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public string WhsCode { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string AppVersion { get; set; }
        public string Token { get; set; }
        public string CompanyId { get; set; }
        public long GrnDocEntry { get; set; }

        public string QueryCode { get; set; }
        public string AgencyCode { get; set; }

        public long PoDocEntry { get; set; }
        public string PoDocEntries { get; set; }
        public string SaveDocType { get; set; }
        public Grn Doc { get; set; }
        public object Doc1 { get; set; }
        public int PoDocNum { get; set; }

        public DateTime GrnStartDt { get; set; }
        public DateTime GrnEndDt { get; set; }

        // for grn draft retrieve
        public string DeliveryOrderNo { get; set; }
        public string Files { get; set; }

        public FTAPP_GRN GrnDoc { get; set; }

        public string CardCode { get; set; }

        public string Source { get; set; } = ""; // 20230111



    }

}

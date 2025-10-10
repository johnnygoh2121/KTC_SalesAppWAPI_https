using KTC_SalesAppWAPI.Models.COG.ReturnMemoF;
using KTC_SalesAppWAPI.Models.WhsReturn;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.WhsReturn
{
    public class WhsRet_Dto
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string CnDocNum { get; set; }
        public int CnDocEntry { get; set; }
        public int InvDocEntry { get; set; } // for whs app invoice doc entry
        public List<WhsRtn1> RtnLines { get; set; } 
        public List<WhsRtn1> RtnLines_Draft { get; set; }  // for saving the draft
        public List<WhsRtn1_Inv> RtnLines_Inv { get; set; }  // for saving the draft
        public List<WhsRtn1_Inv> RtnLines_Inv_Draft { get; set; }  // for saving the draft
        public FTAPP_WRTN Head { get; set; }
        public string SignFiles { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public string UserCode { get; set; }

        public string CompanyId { get; set; }
        public string Token { get; set; }
        public ReturnReceive Doc1 { get; set; }

        public string CardCode { get; set; }
        public string AppVersion { get; set; }

        public string ReturnSenderCode { get; set; }
        public string ReturnSenderName { get; set; }
        public string Operation { get; set; }

        // for trcn query 
        public string WhsCode { get; set; }

        // for remove the draft
        public int RetDocEntry { get; set; }

        public string InvDocNum { get; set; }

        public ReturnMemo RetMemo_ByInv { get; set; }

        // for posting create return memo 
        public string QueryCompanyID { get; set; }
        public string QueryKeys { get; set; }
        public string UpdateType { get; set; }

        public FTAPP_WRTN_INV InvHead { get; set; }
        public List<WhsRtn1_Inv> InvDetails { get; set; }

    }
}

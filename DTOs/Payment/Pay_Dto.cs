using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Payment.Log;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class Pay_Dto
    {
        public string Request { get; set; }
        public string WebPortalRequest { get; set; } = "Payment";
        public string CompanyId { get; set; }
        public string DocType { get; set; }
        public string Code { get; set; }
        public string QueryKey { get; set; }

        // for doc create / update {draft and submit}
        public PortalPayment PayHead { get; set; }
        public string UpdateType { get; set; }
        public string CompanyName { get; set; }

        // for aging 
        public string QueryCardCode { get; set; }   
        public string UserCode { get; set; }

        // cash summary 
        public DateTime QuerySummaryDate { get; set; }      
        
        // cash summary udate 
        public FTApp_SellerCashLog CashLog { get; set; }
        public FTApp_SellerChequeLog ChequeLog { get; set; }

        public DateTime ChequeStartDt { get; set; }
        public DateTime ChequeEndDt { get; set; }

        public List<FTApp_SellerChequeLog> ChequeLogs { get; set; }
        public List<FTApp_SellerCashLog> CashLogs { get; set; }

        public string DocStatus { get; set; }
        public int DocEntry { get; set; }

        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public FTAPP_AppPostLog Line { get; set; }

        // for signed receipt photo upload 
        public string SignedReceipt { get; set; }

    }
}

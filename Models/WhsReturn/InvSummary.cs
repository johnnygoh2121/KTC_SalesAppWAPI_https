using System;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class InvSummary
    {
        public string SubSi { get; set; }
        public int InvDocEntry { get; set; }
        public int InvNO { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime GraceExpiredDate { get; set; }
        public string UserCode { get; set; } // also the employee code for hr
        public string UserName { get; set; }
        public decimal ChargeAmt { get; set; } // also the amount for hr
        public string Remarks { get; set; }

        // for hr future report 
        // indicate the source of charges
        // trcn       
        // cdn
        // gondola
        public string ChargeSource { get; set; } // 20211102

        // for hr report 
        public string EmployeeNo { get; set; }
        public string CompanyCode { get; set; }
        public string PayItem { get; set; }
        public string Currency { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public decimal Amount { get; set; }
        public int Fixed { get; set; } = default;
        public int Run1 { get; set; } = default;

        // for auditing purpose and linking
        public DateTime ReportedDate { get; set; }
    }
}

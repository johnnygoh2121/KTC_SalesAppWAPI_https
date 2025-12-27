using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Refund
{
    public class Refund
    {
        // app 
        public string SubSiId { get; set; }
        public string SubSiName { get; set; }


        // portal table 
        public long DocEntry { get; set; }

        public long? DocNum { get; set; }

        public string DocStatus { get; set; }

        public DateTime? DocDate { get; set; }

        public string CardCode { get; set; }

        public string CardName { get; set; }

        public string RefNo { get; set; }

        public int? BankAbs { get; set; }

        public string BankCode { get; set; }

        public string AccountName { get; set; }

        public string AccountNo { get; set; }

        public string Email { get; set; }

        public decimal? DocTotal { get; set; }

        public string Reason { get; set; }

        public decimal TransferSum { get; set; } = 0;

        public decimal CashSum { get; set; } = 0;

        public DateTime? TransferDate { get; set; }

        public string TransferrefNo { get; set; }

        public string CashGl { get; set; }

        public string Transfergl { get; set; }

        public int? ApprLevel { get; set; }

        public int? CurrLevel { get; set; }

        public string UCreated { get; set; }

        public DateTime? DCreated { get; set; }

        public string UModified { get; set; }

        public DateTime? DmMdified { get; set; }
        public string ApprrEm { get; set; }

        public string RefundType { get; set; }

        public List<Refund1> Cheques { get; set; }
        public List<Refund2> Documents { get; set; }


        public string DocStatusDisplay
        {
            get
            {
                switch (DocStatus)
                {
                    case "A": return "Approved";
                    case "D": return "Draft";
                    case "R": return "Rejected";
                    case "S": return "Submitted";
                    default: return "";
                }
            }
        }

        public string PaymentDesc
        {
            get
            {
                if (DocTotal >= 0) 
                {
                    return "Refund";
                }

                return "Collecting";
            }
        }
    }

}

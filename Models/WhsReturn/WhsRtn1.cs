using System;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class WhsRtn1
    {
        public int Id { get; set; }
        public int CnEntry { get; set; }
        public int CnLine { get; set; }
        public int CnDocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal CnPrice { get; set; }
        public decimal CnQty { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime RtnDt { get; set; }
        public decimal RtnQty { get; set; }
        public string Subsi { get; set; }
        public string SubsiID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ScanInCode { get; set; }

        // from sales app data 
        public Guid LineGuid { get; set; } // for later cn search link

        public string LotNo { get; set; }
        public DateTime ExpDate { get; set; } = default;
        public DateTime MfrDate { get; set; } = default;

        public string Reason { get; set; }
        public string WhsCode { get; set; }

        // for app display 
        public string BarcodeStr { get; set; }

        public decimal CnIssueQty { get; set; }

        public decimal ReceivedQty { get; set; }


        public decimal VarianceQty { get; set; }


        public string Remarks { get; set; }

        // app usage 
        //public Guid lineGuid { get; set; }
        public string Suppcatnum { get; set; }
        public int UOMQTY { get; set; }
        public string CodeBars { get; set; }

        public decimal QtyInPcs { get; set; }


        public decimal QtyPc { get; set; }

        public decimal QtyCs { get; set; }

        public string ManBtchNum { get; set; }

        public string Remark { get; set; } // RTN line remark 

        // 20231130
        // for sold reseller sr bin 
        public int U_SRQTY { get; set; }
        public int ActAvailReturnQty { get; set; }


    }
}

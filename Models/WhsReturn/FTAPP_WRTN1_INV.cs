using System;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class FTAPP_WRTN1_INV
    {
        public int Id { get; set; }
        public int InvEntry { get; set; }
        public int InvLine { get; set; }
        public int InvDocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal InvPrice { get; set; }
        public decimal InvQty { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime RtnDt { get; set; }
        public decimal RtnQty { get; set; }
        public string Subsi { get; set; }
        public string SubsiID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Remarks { get; set; }
        public int UomQty { get; set; }
        public string ScanInCode { get; set; }
        public string Reason { get; set; }
        public string WhsCode { get; set; }
        public string LotNo { get; set; }
        public DateTime ExpDate { get; set; }
        public DateTime MfrDate { get; set; }
        public int GIDocEntry { get; set; }
        public int GILineNum { get; set; }
        public decimal GIQty { get; set; }
        public string ManBtchNum { get; set; }
        public string Remark { get; set; }

        public double VarientQty { get; set; } // for ease good issue, calculate by sql script 
    }
}

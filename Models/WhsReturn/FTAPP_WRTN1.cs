using System;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class FTAPP_WRTN1
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
    }

}


using System;

namespace KTC_SalesAppWAPI.Models.COG.ReturnMemoF
{
    public class Return_Line
    {
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string ITEMNAME { get; set; }
        public string CODEBARS { get; set; }
        public string FROZENFOR { get; set; }
        public decimal UOMQTY { get; set; }
        public decimal INVPRICE { get; set; }
        public decimal PRICE { get; set; }
        public decimal QUANTITYCS { get; set; }
        public decimal QUANTITY { get; set; }
        public string REASON { get; set; }
        public decimal LINETOTAL { get; set; }
        public string OLDITEM { get; set; }
        public int NOOFPAGES { get; set; }
        public string PAGES { get; set; }
        public string DEL { get; set; }
        public string WHSCODE { get; set; }
        public string GLCODE { get; set; }
        public int BASEENTRY { get; set; }
        public int BASELINE { get; set; }
        public decimal QUANTITYPC { get; set; }
        public decimal TOPPRICE { get; set; }
        public decimal DISC { get; set; }
        public decimal NUMPERMSR { get; set; }
        public string LASTDOCNUM { get; set; }
        public DateTime LASTINVDATE { get; set; }
        public string NOGST { get; set; }
        public decimal GSTAMT { get; set; }
        public string LOTNO { get; set; }
        public DateTime EXPDATE { get; set; }
        public DateTime MFRDT { get; set; }

        public string AGENCYCODE { get; set; }
        public string REMARK { get; set; }

    }
}

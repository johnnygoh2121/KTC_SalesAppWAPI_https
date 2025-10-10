using KTC_SalesAppWAPI.Models.Payment;
using KTC_SalesAppWAPI.Models.Pick;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Pick_IBT
{
    public class IBT1
    {
        // for app usage

        public string ManBtchNum { get; set; }
        public string ManSerNum { get; set; }

        public int OrigLineNum { get; set; }

        public int UOMQTY { get; set; }
        public List<IBT2> Batches { get; set; }
        public List<OBCD_Ext> BarCodes { get; set; }

        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string SUPPCATNUM { get; set; }
        public string ITEMNAME { get; set; }
        public string CODEBARS { get; set; }
        public decimal INDAYS { get; set; }
        public decimal STOCKQTY { get; set; }
        public decimal POQTY { get; set; }
        public decimal IBTQTY { get; set; }
        public decimal TOTALQTY { get; set; }
        public decimal UNSERVEDQTY { get; set; }
        public decimal AMSQTY { get; set; }
        public decimal CMQTY { get; set; }
        public decimal SSLQTY { get; set; }
        public decimal TSLQTY { get; set; }
        public decimal SUGGESTQTY1 { get; set; }
        public decimal SUGGESTQTY2 { get; set; }
        public decimal M3 { get; set; }
        public decimal PROPOSEQTY { get; set; }
        public decimal DISCOUNT { get; set; }
        public decimal FOC { get; set; }
        public string REMARKS { get; set; }
        public decimal QTY1 { get; set; }
        public decimal DISC1 { get; set; }
        public decimal FOC1 { get; set; }
        public decimal WHSQTY { get; set; }
        public decimal PRICE { get; set; }
        public decimal LMSQTY { get; set; }
        public decimal PICKEDQTY { get; set; }

        // for posting usage
        
    }
}

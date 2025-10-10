using KTC_SalesAppWAPI.Models.Pick;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class COG_Line
    {
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string ITEMNAME { get; set; }
        public string OLDCODE { get; set; }
        public string WHSCODE { get; set; }
        public string FROZENFOR { get; set; }
        public string CODEBARS { get; set; }
        public decimal UOMQTY { get; set; }
        public decimal PRICE { get; set; }
        public decimal QUANTITYCS { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal LINETOTAL { get; set; }
        public string REASON { get; set; }
        public string GLCODE { get; set; }
        public string REMARK { get; set; }
        public decimal QUANTITYPC { get; set; }
        public decimal TOPPRICE { get; set; }
        public decimal INVPRICE { get; set; }
        public string LASTDOCNUM { get; set; }
        public DateTime? LASTINVDATE { get; set; } = null;
        public string NOGST { get; set; }
        public decimal GSTAMT { get; set; }
        public string LOTNO { get; set; }
        public DateTime? EXPDATE { get; set; } = null;
        public DateTime? MFRDATE { get; set; } = null;
        public string REFITEM { get; set; }
        public int REFLINE { get; set; }

        public string REFREASON { get; set; }
        public DateTime AVACKDATE { get; set; }
        public string REFUOM { get; set; }

        public List<OBCD_Ext> BarCodes { get; set; } // the barcodes list
        // for app usage 
        // 20211111                
       
        public string SuppCatNum { get; set; }
        public string WhsName { get; set; }

        public string REASONDesc { get; set; } // for reason code desc

        public string BarcodeStr
        {
            get
            {
                if (BarCodes != null && BarCodes.Count > 0)
                {
                    var str = string.Join("\n", BarCodes.Select(x => x.BcdCode).Distinct().ToList());
                    return str;
                }
                return string.Empty;
            }
        }
    }
}
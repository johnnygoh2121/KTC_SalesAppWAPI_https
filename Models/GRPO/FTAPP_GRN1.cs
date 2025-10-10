using KTC_SalesAppWAPI.Models.Pick;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class FTAPP_GRN1
    {
        // for app 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public string UOM { get; set; }
        public Guid LineGuid { get; set; }
        public decimal ReceiptQtyPc { get; set; }
        public decimal ReceiptQtyCs { get; set; }
        public decimal ReceiptQty { get; set; }
        public string BarCodesStr { get; set; }

        public List<OBCD_Ext> BarCodes { get; set; }

        public DateTime InDt { get; set; } // for capture the line in date
        public string ScanUser { get; set; } // 20230117

        // orig
        public int ID { get; set; }
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string ITEMNAME { get; set; }
        public string CODEBARS { get; set; }
        public decimal UOMQTY { get; set; }
        public decimal STOCKQTY { get; set; }
        public decimal QUANTITYCS { get; set; }
        public decimal QUANTITYPC { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal PRICE { get; set; }
        public decimal LINETOTAL { get; set; }
        public int BASEDOCNUM { get; set; }
        public string SUPPCATNUM { get; set; }
        public decimal DISCOUNT { get; set; }
        public string FOC { get; set; }
        public int BASELINE { get; set; }
        public int BASEENTRY { get; set; }
        public int PONO { get; set; }
        public string REASON { get; set; }
        public string FROZENFOR { get; set; }
        public string OLDCODE { get; set; }
        public string LOTNO { get; set; }
        public DateTime EXPDATE { get; set; }
        public DateTime MFRDATE { get; set; }
        public string REMARKS { get; set; }
        public Guid HEADGUID { get; set; }
        public string USERCODE { get; set; }
        public string USERNAME { get; set; }
        public string DELIVERY_ORDER { get; set; } // for saving the draft

        // for batch management 
        public string ManBtchNum { get; set; }

        
    }
}

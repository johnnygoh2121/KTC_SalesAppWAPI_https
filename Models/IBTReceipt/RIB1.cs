using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.IBTReceipt
{
    public class RIB1 : ICloneable
    {
        // for app app usage 
        public List<RIB2> Batches { get; set; }
        public string ManBtchNum { get; set; }
        public string ManSerNum { get; set; }
        public decimal ReceiptQty { get; set; }
        public decimal ReceiptQtyPc { get; set; }
        public decimal ReceiptQtyCs { get; set; }
        public string LOTNO { get; set; }
        public string REMARKS { get; set; }
        public DateTime EXPDATE { get; set; }
        public DateTime MFRDATE { get; set; }

        // orig data
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

        public object Clone()
        {
            return new RIB1
            {
                Batches = this.Batches,
                ManBtchNum = this.ManBtchNum,
                ManSerNum = this.ManSerNum,
                ReceiptQty = this.ReceiptQty,
                ReceiptQtyPc = this.ReceiptQtyPc,
                ReceiptQtyCs = this.ReceiptQtyCs,
                LOTNO = this.LOTNO,
                REMARKS = this.REMARKS,
                EXPDATE = this.EXPDATE,
                MFRDATE = this.MFRDATE,
                DOCENTRY = this.DOCENTRY,
                LINENUM = this.LINENUM,
                ITEMCODE = this.ITEMCODE,
                ITEMNAME = this.ITEMNAME,
                CODEBARS = this.CODEBARS,
                UOMQTY = this.UOMQTY,
                STOCKQTY = this.STOCKQTY,
                QUANTITYCS = this.QUANTITYCS,
                QUANTITYPC = this.QUANTITYPC,
                QUANTITY = this.QUANTITY,
                PRICE = this.PRICE,
                LINETOTAL = this.LINETOTAL,
                BASEDOCNUM = this.BASEDOCNUM,
                SUPPCATNUM = this.SUPPCATNUM,
                DISCOUNT = this.DISCOUNT,
                FOC = this.FOC, 
                BASELINE = this.BASELINE,
                BASEENTRY = this.BASEENTRY,
                PONO = this.PONO,
                REASON = this.REASON,
                FROZENFOR = this.FROZENFOR,
                OLDCODE = this.OLDCODE
            };
        }
    }
}

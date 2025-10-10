
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WebPortal
{
    public class WSO1 
    {
        public List<WFTAPP_BOX> Boxes { get; set; }
        public List<WSO2> Batches { get; set; } // doe line batch 

        
        public long DOCENTRY { get; set; }

        
        public int LINENUM { get; set; }
        
        public string ITEMCODE { get; set; }

        public string ITEMNAME { get; set; }

        public string CODEBARS { get; set; }

        public decimal? UOMQTY { get; set; }

        public decimal? STOCKQTY { get; set; }

        public decimal? PRICE { get; set; }

        public decimal? QUANTITY { get; set; }

        public decimal? QUANTITYCS { get; set; }

        public decimal? QTY { get; set; }

        public decimal? DISC { get; set; }

        public decimal? SUPP { get; set; }

        public decimal? DISCSUM { get; set; }

        public decimal? LINETOTAL { get; set; }

        public long? PENTRY { get; set; }

        public int? PLINE { get; set; }

        public string PTYPE { get; set; }

        public decimal? SUGGESTQTY { get; set; }

        public long? DOCNUM { get; set; }

        public string BORNE { get; set; }

        public decimal? SUPPSUM { get; set; }

        public decimal? INVQTY { get; set; }

        public decimal? INVPRICE { get; set; }

        public decimal? INVTOTAL { get; set; }

        public decimal? ITEMCOST { get; set; }

        public string DIM1 { get; set; }

        public string DIM2 { get; set; }

        public string DIM3 { get; set; }

        public int? MBID { get; set; }

        public string SUPPCODE { get; set; }

        public decimal? QUANTITYPC { get; set; }

        public string REFNO { get; set; }

        public string REFITEM { get; set; }

        public string UOM { get; set; }

        public int? BATCHID { get; set; }

        public string COKEPROMO { get; set; }

        public string SUPPCATNUM { get; set; }

        public string TAXCODE { get; set; }

        public decimal? PRICE2 { get; set; }

        public string NONIM { get; set; }

        public int? PROMOCOUNT { get; set; }

        public long? NPENTRY { get; set; }

        public int? NPID { get; set; }

        public int? NPLINE { get; set; }

        public string PROMOPACKAGE { get; set; }

        public decimal? PICKEDQTY { get; set; }

        public int? REFLINE { get; set; }

        public string REFORDER { get; set; }

        public string REFUOM { get; set; }

        public string TPBRANCH { get; set; }

        public string TPREF { get; set; }

        public string AVLINE { get; set; }

        public string LINEREMARK { get; set; }

        
    }
}

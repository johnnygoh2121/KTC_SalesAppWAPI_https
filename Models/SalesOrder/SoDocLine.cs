using Newtonsoft.Json;

namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class SoDocLine
    {
        [JsonIgnore]
        public string Subsi { get; set; }

        [JsonIgnore]
        public string Warehouse { get; set; }

        #region Original property
        public int linenum { get; set; }
        public string itemcode { get; set; }
        public string itemname { get; set; }
        public string codebars { get; set; }
        public double uomqty { get; set; }
        public double stockqty { get; set; }
        public double price { get; set; }
        public double quantity { get; set; }
        public double quantitycs { get; set; }
        public double qty { get; set; }
        public double disc { get; set; } // total line discount
        public double supp { get; set; } // suppier born how many percent 
        public double discsum { get; set; }
        public double linetotal { get; set; }
        public int? pentry { get; set; } // 
        public int? pline { get; set; }
        public string ptype { get; set; }
        public double suggestqty { get; set; }
        public int docnum { get; set; }
        public string borne { get; set; }
        public double suppsum { get; set; }
        public double invqty { get; set; }
        public double invprice { get; set; }
        public double invtotal { get; set; }
        public double itemcost { get; set; }
        public string dim1 { get; set; }
        public string dim2 { get; set; }
        public string dim3 { get; set; }
        public int mbid { get; set; }
        public string suppcode { get; set; }
        public double? quantitypc { get; set; }
        public string refno { get; set; }
        public string refitem { get; set; }
        public string uom { get; set; }
        public int? batchid { get; set; }
        public string cokepromo { get; set; }
        public string suppcatnum { get; set; }
        public string taxcode { get; set; }
        public double? price2 { get; set; }
        public string nonim { get; set; }
        public int? promocount { get; set; }
        public int? npentry { get; set; }
        public int? npid { get; set; }
        public int? npline { get; set; }
        public string promopackage { get; set; }
        public int refLine { get; set; }
        public string LineRemark { get; set; }

        #endregion
    }
}

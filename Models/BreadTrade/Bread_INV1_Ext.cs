using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_INV1_Ext
    {
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public string ITEMCODE { get; set; }
        public string ITEMNAME { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal PRICE { get; set; }
        public string TAXCODE { get; set; }
        public decimal TAXPERC { get; set; }
        public decimal TAXSUM { get; set; }
        public decimal LINETOTAL { get; set; }
        public string LINETYPE { get; set; }
        public string Batch { get; set; }
        public List<Bread_Batch> Batches { get; set; }
    }
}

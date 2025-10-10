using KTC_SalesAppWAPI.Models.Batches;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.COG.ReturnMemoF
{

    public class ReturnMemoLine
    {
        public long Docentry { get; set; }
        public int Linenum { get; set; }
        public string Itemcode { get; set; }
        public string Itemname { get; set; }
        public string Codebars { get; set; }
        public string Frozenfor { get; set; }
        public decimal? Uomqty { get; set; }
        public decimal? Invprice { get; set; }

        public decimal DiscPrcnt { get; set; }

        /// <summary>
        /// Unit price for CN
        /// </summary>
        public decimal? Price { get; set; }
        public decimal? Quantitycs { get; set; }
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Return reason (Selection)
        /// </summary>
        public string Reason { get; set; }
        public decimal? Linetotal { get; set; }
        public string Olditem { get; set; }
        public int? Noofpages { get; set; }
        public string Pages { get; set; }
        public string Del { get; set; }

        /// <summary>
        /// Wareshouse (Selection)
        /// </summary>
        public string Whscode { get; set; }

        /// <summary>
        /// GL Account (Auto base on reason selected)
        /// </summary>
        public string Glcode { get; set; }

        /// <summary>
        /// Wareshouse (Selection)
        /// </summary>
        public int? Baseentry { get; set; }
        public int? Baseline { get; set; }
        public decimal? Quantitypc { get; set; }

        /// <summary>
        /// Original price from invoice
        /// </summary>
        public decimal? Topprice { get; set; }

        /// <summary>
        /// discount % from invoice
        /// </summary>
        public decimal? Disc { get; set; }
        public decimal? Numpermsr { get; set; }
        public string Lastdocnum { get; set; }
        public DateTime? Lastinvdate { get; set; } = null;
        public string Nogst { get; set; }
        public decimal? Gstamt { get; set; }

        /// <summary>
        /// Lot No
        /// </summary>
        public string Lotno { get; set; }

        /// <summary>
        /// Expired Date
        /// </summary>
        public DateTime? Expdate { get; set; } = null;
        public DateTime? Mfrdt { get; set; } = null;

        public string Remark { get; set; } // line remark

        // for batch 
        //20211203T1046
        public List<BatchNo> Batches { get; set; }

        // AgencyCode
        // 20240718
        public string AgencyCode { get; set; }
    }
}


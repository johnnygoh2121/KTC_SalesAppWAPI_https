using KTC_SalesAppWAPI.Models.Pick;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class POR1_Ext
    {
        // for app 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public long Docentry { get; set; }
        public  int U_CSUS_UOM { get; set; }
        public decimal PRICEBEFDI { get; set; }
        public decimal DISCPRCNT { get; set; }

        public string U_CSUS_FOC { get; set; }
        public decimal OPENSUM { get; set; }

        public decimal OpenQty { get; set; }

        /// <summary>
        /// Line Number
        /// </summary>
        public int Linenum { get; set; }
        /// <summary>
        /// Item Code
        /// </summary>
        public string Itemcode { get; set; }

        /// <summary>
        /// Item Name
        /// </summary>
        public string Itemname { get; set; }

        /// <summary>
        /// Item Barcode
        /// </summary>
        public string Codebars { get; set; }

        /// <summary>
        /// No of Pcs per carton
        /// </summary>
        public int Uomqty { get; set; }

        public int Stockqty { get; set; }

        /// <summary>
        /// Quantity in Carton (User entry)
        /// </summary>
        public int Quantitycs { get; set; }

        /// <summary>
        /// Quantity in Pcs (User entry)
        /// </summary>
        public int Quantitypc { get; set; }

        /// <summary>
        /// Total quantity in PCS
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit Price
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Line Total
        /// </summary>
        public decimal? Linetotal { get; set; }

        /// <summary>
        /// SAP PO DocNum 
        /// </summary>
        public int? Basedocnum { get; set; }

        /// <summary>
        /// Mfr. Cat No
        /// </summary>
        public string Suppcatnum { get; set; }

        /// <summary>
        /// Discount (From PO)
        /// </summary>
        public decimal? Discount { get; set; }

        /// <summary>
        /// Determine if item received is FOC (Y/N)
        /// </summary>
        public string Foc { get; set; }

        /// <summary>
        /// SAP PO LineNum 
        /// </summary>
        public int? Baseline { get; set; }

        /// <summary>
        /// SAP PO DocEntry 
        /// </summary>
        public int? Baseentry { get; set; }

        /// <summary>
        /// SAP PO DocNum 
        /// </summary>
        public int? Pono { get; set; }

        /// <summary>
        /// Not using 
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// Not using 
        /// </summary>
        public string Frozenfor { get; set; }

        /// <summary>
        /// Not using 
        /// </summary>
        public string Oldcode { get; set; }

        public string INV { get; set; }
        public string UOM { get; set; }

        public List<OBCD_Ext> BarCodes { get; set; }
    }
}

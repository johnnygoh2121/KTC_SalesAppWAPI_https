using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.Pick;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class Grn1
    {
        // for app 
        public string Subsi { get; set; }
        public string SubsiId { get; set; }

        public List<OBCD_Ext> BarCodes { get; set; }
        public string UOM { get; set; }

        /// <summary>
        /// Document entry link to header
        /// </summary>
        public long Docentry { get; set; } = 0;

        /// <summary>
        /// Line Number
        /// </summary>
        public int Linenum { get; set; } = 0;
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
        public decimal Uomqty { get; set; }

        /// <summary>
        /// unuse
        /// </summary>
        public decimal Stockqty { get; set; }

        /// <summary>
        /// Quantity in Carton (User entry)
        /// </summary>
        public decimal Quantitycs { get; set; }

        /// <summary>
        /// Quantity in Pcs (User entry)
        /// </summary>
        public decimal Quantitypc { get; set; }

        /// <summary>
        /// Total quantity in PCS
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Line Total
        /// </summary>
        public decimal Linetotal { get; set; }

        /// <summary>
        /// SAP PO DocNum 
        /// </summary>
        public int Basedocnum { get; set; }

        /// <summary>
        /// Mfr. Cat No
        /// </summary>
        public string Suppcatnum { get; set; }

        /// <summary>
        /// Discount (From PO)
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Determine if item received is FOC (Y/N)
        /// </summary>
        public string Foc { get; set; }

        /// <summary>
        /// SAP PO LineNum 
        /// </summary>
        public int Baseline { get; set; }

        /// <summary>
        /// SAP PO DocEntry 
        /// </summary>
        public int Baseentry { get; set; }

        /// <summary>
        /// SAP PO DocNum 
        /// </summary>
        public int Pono { get; set; }

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
        public string LOTNO { get; set; }
        public DateTime? EXPDATE { get; set; } 
        public DateTime? MFRDATE { get; set; } 
        public string lineRemarks { get; set; }

        // for batch manage 
        // 20211118
        public List<BatchNo> Batches { get; set; }

        public DateTime? InDt { get; set; }

        public string ScanUser { get; set; }
    }
}

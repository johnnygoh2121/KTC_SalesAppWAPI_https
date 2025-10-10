using System;

namespace KTC_SalesAppWAPI.Models.Batches
{
    public class FTAPP_Batch
    {
        public int id { get; set; }
        public int DocEntry { get; set; }
        public int BaseLine { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BatchNo { get; set; }
        public decimal BatchQty { get; set; }
        public int OBTQ_Abs { get; set; }
        public int OBTN_Abs { get; set; }

        public DateTime ExpDate { get; set; }
        public DateTime MnfDate { get; set; }

        // for app isage 
        public decimal PickedQty { get; set; }

        // for batch selection display       
        public decimal UomQty { get; set; }

        // for app 
        public decimal CsQty { get; set; }
        public decimal PcQty { get; set; }

        public decimal PickedCsQty { get; set; }
        public decimal PickedPcQty { get; set; }

        public string BoxId { get; set; }
        public string AppVersion { get; set; }

        // 20250418
        // saving the batch seelction picking mode 
        public string PickingMode { get; set; }

    }
}

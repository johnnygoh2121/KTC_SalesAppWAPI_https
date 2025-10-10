using System;

namespace KTC_SalesAppWAPI.Models.WStockCount
{
    public class SC1
    {   
        public int Id { get; set; }
        public Guid ScLineGuid { get; set; }
        public Guid ScGuid { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal CountedQty { get; set; }
        public decimal ReCountQty { get; set; }
        public decimal UomQty { get; set; }
        public int SeqNo { get; set; }
        public string SpaceID { get; set; }
        public string CounterCode { get; set; }
        public string CounterName { get; set; }
        public string ReCounterCode { get; set; }
        public string ReCounterName { get; set; }
        public DateTime CountedDt { get; set; }
        public DateTime ReCountedDt { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string SubSi { get; set; }
        public string ScanCode { get; set; }
        public DateTime MfgDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string Remarks { get; set; }
        public string LineStatus { get; set; }
        public string UomType { get; set; }
        public decimal U_CSUS_UOM { get; set; }
        public string SUPPCATNUM { get; set; }
        public string CODEBARS { get; set; }
        public decimal QUANTITY { get; set; } // counted qty in pcs
        public decimal REQUANTITY { get; set; } // for re counted qty in pcs
        public string BatchNo { get; set; } // for future use
        public DateTime ReMfgDate { get; set; }
        public DateTime ReExpDate { get; set; }
        public string ReRemarks { get; set; }

        public string ManBtchNum { get; set; }
    }
}

using KTC_SalesAppWAPI.Models.DN;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_CN1_Ext
    {        
        public long DOCENTRY { get; set; }
        public int LINENUM { get; set; }
        public long BASEENTRY { get; set; }
        public int BASELINE { get; set; }
        public string ITEMCODE { get; set; }
        public string ITEMNAME { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal PRICE { get; set; }
        public string TAXCODE { get; set; }
        public decimal TAXPERC { get; set; }
        public decimal TAXSUM { get; set; }
        public decimal LINETOTAL { get; set; }
        public string LINETYPE { get; set; }
        public string CodeBars { get; set; }
        public string ManBtchNum { get; set; }

        // trcn needed prop 
        public DateTime ExpDate { get; set; }
        public DateTime MfrDt { get; set; }
        public string Remark { get; set; }
        public string LotNo { get; set; }
        public string WhsCode { get; set; }
        public string Reason { get; set; }

        public Guid LineGuid { get; set; }

        public int UomQty { get; set; }
        public string AgencyCode { get; set; }
        // for app usage 
        public decimal RtnQty { get; set; }

        public decimal QtyInPcs { get; set; }
        public decimal CnIssueQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal VarianceQty { get; set; }

        public List<Bread_Batch> Batches { get; set; }

        // for dist guid 
        public string UserCode { get; set; }
        public string DiCardCode { get; set; }

    }
}

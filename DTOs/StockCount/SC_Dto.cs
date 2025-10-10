using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.StockCount;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.StockCount
{
    public class SC_DTo
    {
        public string Request { get; set; }
        public string QueryCompanyName { get; set; }
        public string QueryProdCode { get; set; }
        public FTAPP_InStoreStockCount Head { get; set; }
        public List<FTAPP_InStoreStockCountLine> Lines { get; set; }

        // for query the stock count head
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }
        public string StoreCode { get; set; }
        public string CounterCode { get; set; }

        // query stock line 
        public string HeadGuid { get; set; }

        public FTAPP_AppPostLog Line { get; set; }
    }
}

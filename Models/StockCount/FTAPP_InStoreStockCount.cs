using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.StockCount
{
    public class FTAPP_InStoreStockCount
    {
        public int id { get; set; }
        public string CountName { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public string CounterCode { get; set; }
        public string CounterName { get; set; }
        public DateTime CounteDt { get; set; }
        public string HeadGuid { get; set; }
        public DateTime CreatedDt { get; set; }

        public int NumCounted { get; set; }
    }
}

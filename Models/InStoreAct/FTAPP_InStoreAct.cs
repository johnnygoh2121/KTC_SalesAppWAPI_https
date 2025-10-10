using System;

namespace KTC_SalesAppWAPI.Models.InStoreAct
{
    public class FTAPP_InStoreAct
    {
        public int id { get; set; }
        public string ActName { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime ActDt { get; set; }
        public Guid HeadGuid { get; set; }
        public DateTime CreatedDt { get; set; }
        public string AgencyCode { get; set; }
        public string AgencyName { get; set; }
        public string SubSi { get; set; }

        public int TotalLine { get; set; }

        // 20230501
        public string DocStatus { get; set; } = "S";
    }
}

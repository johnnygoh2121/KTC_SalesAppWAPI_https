using System;

namespace KTC_SalesAppWAPI.Models.Pick
{
    public class FTAPP_FreezedItems
    {
        public int Id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string CodeBars { get; set; }
        public DateTime FreezeDt { get; set; }
        public string FeezeFor { get; set; }
        public string FreezeByUserCode { get; set; }
        public string FreezeByUserName { get; set; }
        public string AgencyCode { get; set; }
        public string AngencyName { get; set; }
    }
}

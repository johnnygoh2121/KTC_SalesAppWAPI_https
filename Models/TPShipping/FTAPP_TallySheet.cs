using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.TPShipping
{
    public class FTAPP_TallySheet
    {
        public int id { get; set; }
        public string Studio { get; set; }
        public string OrderDate { get; set; }
        public string ShippingCartonNo { get; set; }
        public int RunNo { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime TransDt { get; set; }
        public Guid HeadGuid { get; set; }

        public List<FTAPP_TallySheet1> Lines { get; set; }
    }
}

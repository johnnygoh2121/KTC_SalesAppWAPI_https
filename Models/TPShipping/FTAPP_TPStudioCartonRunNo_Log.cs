using System;

namespace KTC_SalesAppWAPI.Models.TPShipping
{
    public class FTAPP_TPStudioCartonRunNo_Log
    {
        public int id { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public int TPRunNo { get; set; }
        public string TPOrderDate { get; set; }
        public string TPStudio { get; set; }
        //public int OrderNo { get; set; }
        public string SubSi { get; set; }
        public string AppVersion { get; set; }
        public DateTime TransDt { get; set; }
    }
}

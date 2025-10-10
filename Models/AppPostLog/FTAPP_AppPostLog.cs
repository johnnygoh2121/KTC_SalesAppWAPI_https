using System;

namespace KTC_SalesAppWAPI.Models.AppPostLog
{
    public class FTAPP_AppPostLog
    {  
        public string AppModule { get; set; }
        public string UserCode { get; set; }
        public string CardCode { get; set; }
        public string SubSi { get; set; }
        public DateTime TransDt { get; set; }
        public string Details { get; set; }
        public string PostResult { get; set; }
        public string AppVersion { get; set; }
    }
}

using System;

namespace KTC_SalesAppWAPI.Models.AppUpdate
{
    public class FTAPP_Update
    {
        public int id { get; set; }
        public string AppVersion { get; set; }
        public string AppName { get; set; }
        public string ApkName { get; set; }
        public DateTime ReleaseDt { get; set; }
        public string AppType { get; set; }
    }
}

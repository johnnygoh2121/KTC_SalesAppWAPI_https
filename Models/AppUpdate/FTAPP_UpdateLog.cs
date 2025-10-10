using System;

namespace KTC_SalesAppWAPI.Models.AppUpdate
{
    public class FTAPP_UpdateLog
    {
        public int id { get; set; }
        public string ApkDownloadPath { get; set; }
        public string AppName { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime PhoneDt { get; set; }
        public DateTime ServerDt { get; set; }
        public string VersionFrom { get; set; }
        public string VersionTo { get; set; }
        public DateTime TransDt { get; set; }
        public string Subsi { get; set; }
    }
}

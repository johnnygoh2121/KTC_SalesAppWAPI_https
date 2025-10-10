using System;

namespace KTC_SalesAppWAPI.DTOs.AppUpdate
{
    public class AppUpdate_Dto
    {
        public string Request { get; set; }
        public string CurrentAppVersion { get; set; }
        public string CurrentAppName { get; set; }

        public string UserCode { get; set; }
        public string UserName { get; set; }
        public DateTime DeviceDt { get; set; }
        public string Subsi { get; set; }

        public string DeviceAppVersion { get; set; }

    }
}

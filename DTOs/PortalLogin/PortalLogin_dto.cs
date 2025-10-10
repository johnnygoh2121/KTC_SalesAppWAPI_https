using System;

namespace KTC_SalesAppWAPI.DTOs.PortalLogin
{
    public class PortalLogin_dto
    {
        public string userName { get; set; }
        public string password { get; set; }
        public string company { get; set; }
        public string appVersion { get; set; }
        public string appType { get; set; }
        public string usercode { get; set; }
        public bool isHelperLogin { get; set; }

        // 20250925 
        public DateTime DeviceDt { get; set; }

    }
}

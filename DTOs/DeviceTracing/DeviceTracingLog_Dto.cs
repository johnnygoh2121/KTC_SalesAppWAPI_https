using KTC_SalesAppWAPI.Models.DeviceTracingLog;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.DeviceTracing
{
    public class DeviceTracingLog_Dto
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public DeviceTraceLog Log { get; set; }
        public List<DeviceTraceLog> Logs { get; set; }
    }
}

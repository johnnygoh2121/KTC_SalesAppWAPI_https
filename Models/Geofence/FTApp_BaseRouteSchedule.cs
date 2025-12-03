using System;

namespace KTC_SalesAppWAPI.Models.GeoFence
{
    public class FTApp_BaseRouteSchedule
    {
        public int Id { get; set; }
        public string ScheduleNo { get; set; }
        public string ScheduleName { get; set; }
        public DateTime ScheduleDate { get; set; }
        public string ScheduleType { get; set; }
        public string RouteCode { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public int IsActive { get; set; }
        public int IsOffRouteCustomer { get; set; }
        public string SeqNo { get; set; }
    }
}

using System;

namespace KTC_SalesAppWAPI.Models.Geofence
{
    public class GeofenceSummary
    {
        public int Id { get; set; }
        public string TransitionName { get; set; }
        public string CardCode { get; set; }
        public string UserCode { get; set; }
        public DateTime Occured { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
}

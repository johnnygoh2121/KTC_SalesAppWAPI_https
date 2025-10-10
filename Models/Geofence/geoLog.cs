using System;

namespace KTC_SalesAppWAPI.Models.Geofence
{
    public class GeoLog
    {
        public string UserCode { get; set; }
        public string SlpName { get; set; }
        public string StoreCode { get; set; }
        public string  StoreName { get; set; }
        public DateTime Occured { get; set; }
        public string Transition { get; set; }
        public int TripId { get; set; }
        public double Duration { get; set; }
        public double DistanceToStore { get; set; }
        public double LogLatitude { get; set; } // Latitude and Longitude 
        public double LogLongitude { get; set; }
    }
}

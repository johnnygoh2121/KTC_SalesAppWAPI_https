using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Geofence
{
    public class FTAppGeoTrack
    {
        public int Id { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public DateTime ScheduleDate { get; set; }
        public string UserCode { get; set; }
        public string SlpName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid HeaderGuid { get; set; }
        public List<FTAppGeoTrackLine> Line { get; set; }
        public string Address { get; set; }

        // app usage 
        public string Subsi { get; set; }

        public bool IsOffRoute { get; set; } // 1 for yes , 0 / null as no 

    }
}

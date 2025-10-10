using System;

namespace KTC_SalesAppWAPI.Models.Geofence
{
    public class FTAppGeoTrackLine
    {
        public int Id { get; set; }
        public Guid HeadGuid { get; set; }
        public Guid LineGuid { get; set; }
        public int TripID { get; set; }
        public DateTime LastEnterDt { get; set; }
        public DateTime LastStayOnDt { get; set; }
        public DateTime LastExitDt { get; set; }
        public double LastEnterDt_DisToStore { get; set; }
        public double LastStayedDt_DisToStore { get; set; }
        public double LastExitDt_DisToStore { get; set; }
        public double Duration_EntryDtToExitDt { get; set; }
        public string Address { get; set; }
        public double ActualLat { get; set; }
        public double ActualLongi { get; set; }
        public string AppVersion {get; set;}
        public string TypeOfMiss { get; set; }
        public string Remarks { get; set; }
    }
}

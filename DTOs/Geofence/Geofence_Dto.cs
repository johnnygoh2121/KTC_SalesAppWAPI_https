using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Geofence;
using KTC_SalesAppWAPI.Models.GeoFence;
using System;

namespace KTC_SalesAppWAPI.DTOs.Geofence
{
    public class Geofence_Dto
    {
        public string Request { get; set; }
        public string CompanyName { get; set; }
        public GeofenceResult_Ext Result { get; set; }

        public DateTime UpdateExitDt { get; set; }
        public string UpdateStoreCode { get; set; }
        public string UpdateUserCode { get; set; }

        // for new geo management 
        public GeoLog UpdateLog { get; set; }

        //public FTAppGeoTrack TrackHead { get; set; }
        //public string TransitionName { get; set; }
        //public int TripId { get; set; }


        //public Guid GeoHeadGuid { get; set; }
        public string GeoSetTransitionField { get; set; }
        public string GeoSetTransitionDisField { get; set; }
        public int GeoTripId { get; set; }
        public double GeoDistance { get; set; }
        public string GeoUserCode { get; set; }
        public string GeoType { get; set; }
        public string GeoStoreCode { get; set; }
        public double StayDuration { get; set; }

        public double Lat { get; set; }
        public double Longi { get; set; }
        public string AppVersion { get; set; }

        //public string GeoTrackGuid { get; set; } // for update the exit for line.

        public FTAPP_AppPostLog Line { get; set; }

        public string TypeOfMiss { get; set; }
        public string Remarks { get; set; }
    }
}

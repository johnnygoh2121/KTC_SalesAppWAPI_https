using System;

namespace KTC_SalesAppWAPI.Models.GeoFence
{
    public class GeofenceResult_Ext
    {
        public string SlpName { get; set; }
        public string UserCode { get; set; } // the user coder
        public string UserName { get; set; } // the seller / user name
        public string CardCode { get; set; } // the current bp

        public int Year { get; set; } // for future use, record the year
        public int Month { get; set; } // for future use, record the Month 
        public int Day { get; set; } // for future used, record the day occur

        public DateTime Occured { get; set; } // date time this event occur

        public DateTime LastEnterTime { get; set; }
        //
        // Summary:
        //     Last time exited the geofence region
        public DateTime LastExitTime { get; set; }
        //
        // Summary:
        //     Result transition type
        public int Transition { get; set; }
        //
        // Summary:
        //     Region identifier
        public string RegionId { get; set; }
        //
        // Summary:
        //     Duration span between last exited and entred time
        public double Duration { get; set; }
        //
        // Summary:
        //     Time span between the last entry and current time.
        public double SinceLastEntry { get; set; }
        //
        // Summary:
        //     Result latitude
        public double Latitude { get; set; }
        //
        // Summary:
        //     Result longitude
        public double Longitude { get; set; }
        //
        // Summary:
        //     Result accuracy
        public double Accuracy { get; set; }
        //
        // Summary:
        //     Get transition name
        public string TransitionName { get; set; } // entry, stay on, exit unknow

        public double DistanceToStore { get; set; }
        public int TripId { get; set; }

        //system data
        //public string UserCode { get; set; } // the user coder
        //public string UserName { get; set; } // the seller / user name
        //public string CardCode { get; set; } // the current bp
        //public int Year { get; set; } // for future use, record the year
        //public int Month { get; set; } // for future use, record the Month 
        //public int Day { get; set; } // for future used, record the day occur
        //public DateTime Occured { get; set; } // date time this event occur

        //gefence data

        //public DateTime LastEnterTime { get; set; }

        //Summary:
        //     Last time exited the geofence region
        //public DateTime LastExitTime { get; set; }

        //Summary:
        //     Result transition type
        //public int Transition { get; set; }

        //Summary:
        //     Region identifier
        //public string RegionId { get; set; }

        //Summary:
        //     Duration span between last exited and entred time
        //public int Duration { get; set; }

        //Summary:
        //     Time span between the last entry and current time.
        //public int SinceLastEntry { get; set; }

        //Summary:
        //     Result latitude
        //public double Latitude { get; set; }

        //Summary:
        //     Result longitude
        //public double Longitude { get; set; }

        //Summary:
        //     Result accuracy
        //public double Accuracy { get; set; }

        //Summary:
        //     Get transition name
        //public string TransitionName { get; set; } // entry, stay on, exit unknow

    }
}

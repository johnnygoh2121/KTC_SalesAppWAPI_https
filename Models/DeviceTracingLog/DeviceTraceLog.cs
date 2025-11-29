using System;

namespace KTC_SalesAppWAPI.Models.DeviceTracingLog
{
    public class DeviceTraceLog
    {
        public int Id { get; set; }                  // Primary Key
        public string DeviceId { get; set; }         // Unique device identifier
        public string TruckNo { get; set; } = "";    // Truck number or plate
        public double Latitude { get; set; }         // GPS latitude
        public double Longitude { get; set; }        // GPS longitude
        public DateTime StopTime { get; set; }       // Timestamp when stop detected
        public DateTime DeviceDateTime { get; set; } // Device-reported time
        public double? Speed { get; set; }           // Speed at stop (nullable)
        public string StreetName { get; set; }       // Street name
        public string BuildingName { get; set; }     // Building name
        public string BusinessName { get; set; }     // Business name
        public string City { get; set; }             // City name
        public bool Synced { get; set; }             // Sync status (true/false)
        public string DriverName { get; set; } = ""; // the driver name 
    }
}

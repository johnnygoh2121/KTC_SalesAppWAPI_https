using System;

namespace KTC_SalesAppWAPI.Models.Transfer
{
    public class FTAPP_Transfer
    {
        public int Id { get; set; }
        public string ReceiverCode { get; set; }
        public string ReceiverName { get; set; }
        public string LocationCode { get; set; }
        public string LocationName { get; set; }
        public DateTime TransDt { get; set; }
        public Guid GroupGuid { get; set; }
        public string DocStatus { get; set; }
        public string DriverName { get; set; }
        public string Module { get; set; }
        public int DLBEntry { get; set; }

        public string Subsi { get; set; }
        public string SubsiId { get; set; }

    }
}

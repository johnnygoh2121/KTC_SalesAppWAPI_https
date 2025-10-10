using System;

namespace KTC_SalesAppWAPI.Models.COG
{
    public class FTAPP_DriverStandAloneTrcnLink
    {
        public int id { get; set; }
        public int CnDocNum { get; set; }
        public DateTime TransDt { get; set; }
        public int RtnEntry { get; set; }

        public string DriverName { get; set; }
        public string PlateNum { get; set; }
    }
}

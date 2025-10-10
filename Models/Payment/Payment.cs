using System;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class Payment
    {
        // for app access 
        public string SubSi { get; set; }

        // origi
        public int docentry { get; set; }
        public int linenum { get; set; }
        public string linetype { get; set; }
        public string linecode { get; set; }
        public double lineamt { get; set; }
        public DateTime linedate { get; set; }
        public string lineref { get; set; }
        public string bank1 { get; set; }
        public string bank2 { get; set; }
        public string filename { get; set; }
        public string @checked { get; set; }
        public string ChequeNo { get; set; }
        public string confirmed { get; set; }
        public string canceled { get; set; }
        public DateTime? bankdate { get; set; }
        public int ?batchid { get; set; }
        public string confusr { get; set; }
        public int? bankentry { get; set; }
        public int? bankabs { get; set; }

        public string headGuid { get; set; }
        public string lineGuid { get; set; }
    }
}

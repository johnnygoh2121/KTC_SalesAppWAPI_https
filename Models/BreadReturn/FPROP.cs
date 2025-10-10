using System;

namespace KTC_SalesAppWAPI.Models.BreadReturn
{
    public class FPROP
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public long DocEntry { get; set; }
        public string SapDoc { get; set; }
        public string Url { get; set; }
        public DateTime UploadDt { get; set; }
        public string Module { get; set; }
        public string UserCode { get; set; }
        public string UpdatedSap { get; set; }
        public string DocType { get; set; }
    }
}

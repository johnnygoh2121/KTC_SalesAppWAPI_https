using System;

namespace KTC_SalesAppWAPI.Models.FileUpload
{
    public class FTAPP_FileUploadLog
    {   
        public string Module { get; set; }
        public string FileType { get; set; }
        public DateTime TransDt { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string Filename { get; set; }
        public string Version { get; set; }
        public string SubSi { get; set; }
        public string CardCode { get; set; }
        public string UploadStatus { get; set; }

    }
}

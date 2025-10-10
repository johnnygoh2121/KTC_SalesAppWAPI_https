using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class Bread_FileUpload
    {
        public int Id { get; set; }
        public Guid HeaderGuid { get; set; }
        public DateTime UploadDatetime { get; set; }
        public string AppUser { get; set; }
        public string ServerSavedPath { get; set; }
    }
}

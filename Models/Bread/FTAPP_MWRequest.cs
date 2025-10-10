using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    public class FTAPP_MWRequest
    {
        public int Id { get; set; }
        public string Request { get; set; }
        public string SapUser { get; set; }
        public string SapPassword { get; set; }
        public DateTime RequestTime { get; set; }
        public string PhoneRegID { get; set; }
        public string Status { get; set; }
        public Guid Guid { get; set; }
        public int SapDocNumber { get; set; }
        public DateTime CompletedTime { get; set; }
        public int Tried { get; set; }
        public int AttachFileCnt { get; set; }
        public int CreateSAPUserSysId { get; set; }
        public string LastErrorMessage { get; set; }
    }
}

using System;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class FTAPP_SecretCodes
    {
        public int id { get; set; }
        public string Subsi { get; set; }
        public string WhsUserCode { get; set; }
        public string SenderUserCode { get; set; }
        public string SenderUserName { get; set; }
        public DateTime TransDt { get; set; }
        public int DocEntry { get; set; }
        public string DocNum { get; set; }
        public string Operation { get; set; }
        public Guid SecretCode { get; set; }
    }
}

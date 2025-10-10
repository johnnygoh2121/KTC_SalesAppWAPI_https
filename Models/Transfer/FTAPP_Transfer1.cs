using System;

namespace KTC_SalesAppWAPI.Models.Transfer
{
    public class FTAPP_Transfer1
    {
        public int Id { get; set; }
        public int InvNo { get; set; }
        public DateTime TransDt { get; set; }
        public Guid GroupGuid { get; set; }
    }
}

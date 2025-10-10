using System;

namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class FTAPP_RtnInvCnDlbLink
    {
        public int Id { get; set; }
        public int DlbEntry { get; set; }
        public int InvNum { get; set; }
        public int RtnEntry { get; set; }
        public int CnNum { get; set; }
        public DateTime TransDt { get; set; }
    }
}

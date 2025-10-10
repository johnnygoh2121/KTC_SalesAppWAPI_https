using System;

namespace KTC_SalesAppWAPI.Models.DN
{
    public class DnApprovalDetail
    {   public int id { get; set; }
        public int docentry { get; set; }
        public string usercode { get; set; }
        public string approval { get; set; }
        public DateTime apprdate { get; set; }
        public string remarks { get; set; }
    }
}

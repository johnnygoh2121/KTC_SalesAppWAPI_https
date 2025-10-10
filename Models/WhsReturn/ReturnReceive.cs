using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class ReturnReceive
    {
        public int DocEntry { get; set; }

        public List<ReturnReceiveLnes> Lines { get; set; }
    }
}

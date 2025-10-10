using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class PortalHouseBank
    {
        public string bankCode { get; set; }
        public string bankName { get; set; }
        public string account { get; set; }
        public int absEntry { get; set; }
    }
}

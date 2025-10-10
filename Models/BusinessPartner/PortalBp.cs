using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.BusinessPartner
{
    public class PortalBp
    {
        public string cardCode { get; set; }
        public string cardName { get; set; }
        public string groupName { get; set; }
        public string collector { get; set; }
        public double accountBalance { get; set; }
        public double availableCredit { get; set; }
        public int dueDays { get; set; }
        public string bgExpired { get; set; }
        public string bypassCredit { get; set; }
        public string disableDiscount { get; set; }

        public int groupCode { get; set; }      
        public string billToDef { get; set; }
        public string shipToDef { get; set; }
        public string appBlock { get; set; }

        public List<PortalBpAddress> addreses { get; set; }

        


    }
}

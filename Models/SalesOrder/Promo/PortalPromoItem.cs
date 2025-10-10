using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class PortalPromoItem
    {
        public string id { get; set; }
        public int promoEntry { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public int mbid { get; set; }
        public IList<MustBuyItem> mustBuyItem { get; set; }
        public IList<focItem> focItem { get; set; }
    }
}

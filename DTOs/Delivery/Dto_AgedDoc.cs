using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Pick;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.Delivery
{
    public class Dto_AgedDoc
    {
        public List<AgedDoc> AgedDocs { get; set; }
        public OINV Invoice { get; set; }
        public OWTR_Ext Transfer { get; set; }
    }
}

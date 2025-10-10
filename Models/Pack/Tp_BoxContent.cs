using System;

namespace KTC_SalesAppWAPI.Models.Pack
{
    public class Tp_BoxContent
    {
        public string ItemCode { get; set; } // item code
        public string ProductNo { get; set; } // suppcat num
        public decimal Quantity { get; set; } // loose qty
        public string OrderNo { get; set; } // SO Docentry
        
        //public int OrderNo { get; set; } // SO Docentry

        public string BoxId { get; set; } // the box id z14-xxxxx-xx
        public Guid BoxGuid { get; set; } // the box guid
        public string Packaging { get; set;  } // the box content pc and cs (full carton
    }
}

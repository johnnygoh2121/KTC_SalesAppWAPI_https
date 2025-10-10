using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.DTOs.COG
{
    public class Dto_TrcnItem
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public List<OITM_Ext> Items { get; set; }
        public CogItem Item { get; set; }
    }
}

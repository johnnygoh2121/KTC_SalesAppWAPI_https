using KTC_SalesAppWAPI.Models.Cdn;

namespace KTC_SalesAppWAPI.DTOs.MarketReturn
{
    public class Dto_MarketReturn
    {
        public string Request { get; set; }
        public string Subsi { get; set; }
        public int CnDocEntry { get; set; }
        public string CnDocNum { get; set; }
        public ORIN CN { get; set; } // for whs confirm physical qty
        
    }
}

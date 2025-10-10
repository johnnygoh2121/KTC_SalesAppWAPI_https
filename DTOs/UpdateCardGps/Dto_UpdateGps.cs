using KTC_SalesAppWAPI.Models.AppPostLog;

namespace KTC_SalesAppWAPI.DTOs.UpdateCardGps
{
    public class Dto_UpdateGps
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public decimal NewLongi { get; set; }
        public decimal NewLat { get; set; }
        public decimal CurrentLongi { get; set; }
        public decimal CurrentLat { get; set; }
        public FTAPP_AppPostLog Line { get; set; }
    }
}

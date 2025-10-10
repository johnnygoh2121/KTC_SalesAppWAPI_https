namespace KTC_SalesAppWAPI.DTOs.NewPick
{
    public class Dto_NewPick
    {
        public string Request { get; set; }
        public string SubSi { get; set; }
        public string SubSiID { get; set; }

        public long DocEntry { get; set; }
        public string WhsCode { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserToken { get; set; }
        public bool IsDevelopment { get; set; }
    }
}

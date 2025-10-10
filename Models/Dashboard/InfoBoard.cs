namespace KTC_SalesAppWAPI.Models.Dashboard
{
    public class InfoBoard
    {
        public string Title { get; set; } // desc the title such as MT
        public double MValue { get; set; } // use by sales and collection
        public double MTarget { get; set; }  
        public double Percentage { get; set; }
        public string Desc { get; set; }
    }
}

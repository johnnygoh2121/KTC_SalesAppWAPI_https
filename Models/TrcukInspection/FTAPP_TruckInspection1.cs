namespace KTC_SalesAppWAPI.Models.TrcukInspection
{
    public class FTAPP_TruckInspection1
    {
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public string Section { get; set; }
        public string Inspection { get; set; }
        public string Description { get; set; }
        public int InspectionResult { get; set; }
        

        //for app 
        public bool IsChecked { get; set; } 
    }
}

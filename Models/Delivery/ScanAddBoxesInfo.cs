namespace KTC_SalesAppWAPI.Models.Delivery
{
    public class ScanAddBoxesInfo
    {
        public string ScanAddStatus { get; set; }  // complete, cancel or partial
        public int TotalInvBoxes { get; set; }
        public int ScanAddedBoxes { get; set; }
    }
}

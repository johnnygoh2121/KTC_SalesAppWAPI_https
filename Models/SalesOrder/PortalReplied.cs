namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class PortalReplied
    {
        public bool actionSuccess { get; set; }
        public string errorMessage { get; set; }
        public string actionResult { get; set; }
        public string documentStatus { get; set; }

        // for charge
        public decimal HrChargeAmt { get; set; }
        public string ChargedUserCode { get; set; }
        public string ChargedUserName { get; set; }
    }
}

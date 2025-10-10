namespace KTC_SalesAppWAPI.Models.CreditApplication
{
    public class Collector // simulate OSLP in SAP B1 
    {
        public int SlpCode { get; set; }
        public string SlpName { get; set; }
        public string Memo { get; set; }
        public decimal Commission { get; set; }
        public short GroupCode { get; set; }
        public string Locked { get; set; }
        public string DataSource { get; set; }
        public short UserSign { get; set; }
        public int EmpID { get; set; }
        public string Active { get; set; }
        public string U_DMXSP { get; set; }
        public string U_GSKSP { get; set; }
        public decimal U_COMMRATE { get; set; }
        public string U_COKESP { get; set; }
        public string U_KCSP { get; set; }
        public string U_DKSHSP { get; set; }
        public string U_GSKMA { get; set; }
        public string U_PUFCODE { get; set; }
        public string U_PRID { get; set; }
        public string U_LORCODE { get; set; }
        public string U_ABBCODE { get; set; }

    }
}

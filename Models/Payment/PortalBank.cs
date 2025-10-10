namespace KTC_SalesAppWAPI.DTOs.Payment
{
    public class PortalBank
    {
        public string bankCode { get; set; }
        public string bankName { get; set; }
        public string dfltAcct { get; set; }
        public string dfltBranch { get; set; }
        public int? nextChckNo { get; set; }
        public string locked { get; set; }
        public string dataSource { get; set; }
        public int userSign { get; set; }
        public string swiftNum { get; set; }
        public string iban { get; set; }
        public string countryCod { get; set; }
        public string postOffice { get; set; }
        public string aliasName { get; set; }
        public int absEntry { get; set; }
        public int? dfltActKey { get; set; }
        public int? nextNum { get; set; }
        public string bsPstDate { get; set; }
        public string bsValDate { get; set; }
        public int? bnkOpCode { get; set; }
        public string bsDocDate { get; set; }
        public string u_INACTIVE { get; set; }
    }
}

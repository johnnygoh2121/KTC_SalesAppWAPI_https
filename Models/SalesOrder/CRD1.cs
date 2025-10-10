namespace KTC_SalesAppWAPI.Models.SalesOrder
{
    public class CRD1
    {
        #region fields for table CRD1
        public string Address { get; set; }
        public string CardCode { get; set; }
        public string Street { get; set; }
        public string Block { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public short UserSign { get; set; }
        public int LogInstanc { get; set; }
        public string ObjType { get; set; }
        public string LicTradNum { get; set; }
        public int LineNum { get; set; }
        public string TaxCode { get; set; }
        public string Building { get; set; }
        public string AdresType { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string AddrType { get; set; }
        public string StreetNo { get; set; }
        public string AltCrdName { get; set; }
        public string AltTaxId { get; set; }
        public string TaxOffice { get; set; }
        public string GlblLocNum { get; set; }
        #endregion fields for table CRD1

        // methods
        //public string GetAddress() => $"{Street}, {Block}, {City}, {Country}";        
        public string GetAddress()
        {
            var address = "";
            if (!string.IsNullOrWhiteSpace(Address2))
            {   
                address += $"{Address2}, ";
            }

            if (!string.IsNullOrWhiteSpace(Address3))
            {
                address += $"{Address3}, ";
            }

            if (!string.IsNullOrWhiteSpace(Street))
            {
                address += $"{Street}, ";
            }

            if (!string.IsNullOrWhiteSpace(Block))
            {
                address += $"{Block}, ";
            }

            if (!string.IsNullOrWhiteSpace(ZipCode))
            {
                address += $"{ZipCode}, ";
            }

            if (!string.IsNullOrWhiteSpace(City))
            {
                address += $"{City}, ";
            }

            if (!string.IsNullOrWhiteSpace(County))
            {
                address += $"{County}, ";
            }

            //if (!string.IsNullOrWhiteSpace(Country))
            //{
            //    address += $"{Country}, ";
            //}

            if (!string.IsNullOrWhiteSpace(State))
            {
                address += $"{State}, ";
            }

            address = address.Trim();
            if (!string.IsNullOrWhiteSpace(address))
            {
                address = address.Substring(0, address.Length - 1);
                address = address.Replace(",,", ", ");
                address = address.Replace(", ,", ", ");
                return address;
            }

            return "";
        }       
    }
}


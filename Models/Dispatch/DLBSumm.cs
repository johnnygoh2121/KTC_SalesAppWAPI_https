using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.Dispatch
{
    public class DLBSumm
    {
        // for check in date time
        public int Id { get; set; } // for react native // 20250327
        public DateTime LastCheckedInDt { get; set; } = default;
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Territory { get; set; }

        public string Street { get; set; }
        public string Block { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string State { get; set; }

        public int NumOfDoc { get; set; }

        public string DocType { get; set; }

        public string DriverName { get; set; }

        public string GeoCode { get; set; } // read from DLB
        // interbranch indicator
        // 20251128
        public bool IsInterbranch { get; set; } = false;
        public string DROP_POINT_GEOCODE { get; set; }

        public string WhsCode { get; set; } // for inter branch 
        public string WhsName { get; set; } // for inter branch

        public string U_DELGLN { get; set; } // for store / outlet  whs 

        public long DocEntry { get; set; }

        public DateTime OutDt { get; set; } = default;

        public List<int> InvoiceNumList { get; set; }

        public string GetAddress
        {
            get
            {
                var address = "";
                if (!string.IsNullOrWhiteSpace(Street))
                {
                    address += $"{Street},";
                }

                if (!string.IsNullOrWhiteSpace(Block))
                {
                    address += $"{Block},";
                }
                if (!string.IsNullOrWhiteSpace(City))
                {
                    address += $"{City},";
                }
                if (!string.IsNullOrWhiteSpace(ZipCode))
                {
                    address += $"{ZipCode},";
                }
                if (!string.IsNullOrWhiteSpace(County))
                {
                    address += $"{County},";
                }
                if (!string.IsNullOrWhiteSpace(Country))
                {
                    address += $"{Country},";
                }
                if (!string.IsNullOrWhiteSpace(Territory))
                {
                    address += $"{Territory},";
                }
                if (!string.IsNullOrWhiteSpace(address))
                {
                    address = address.Substring(0, address.Length - 1);
                }

                return address;
            }
        }

    }
}

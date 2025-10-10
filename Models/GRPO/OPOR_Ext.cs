using System;

namespace KTC_SalesAppWAPI.Models.GRPO
{
    public class OPOR_Ext
    {
        /// <summary>
        /// for App display
        /// </summary>
        public string Subsi { get; set; }
        public string SubsiId { get; set; }
        public int TotalLine { get; set; }

        // original

        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocStatus { get; set; }

        public string CardCode { get; set; } // agency
        public string CardName { get; set; }
        public string Refno { get; set; }

        public string Whscode { get; set; }

        public string Towhs { get; set; }

        public string Remarks { get; set; }

        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }

        public string NumAtCard { get; set; }

        public string ForceBacthLot { get; set; }

    }
}

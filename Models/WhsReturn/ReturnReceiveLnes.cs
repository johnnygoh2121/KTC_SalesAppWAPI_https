using KTC_SalesAppWAPI.Models.Batches;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Models.WhsReturn
{
    public class ReturnReceiveLnes
    {

        /// <summary>
        /// ORIN DocEntry No
        /// </summary>
        public int DocEntry { get; set; }

        /// <summary>
        /// RIN1 LineNum
        /// </summary>
        public int LineNum { get; set; }

        /// <summary>
        /// Receiveing Warehouse Code
        /// </summary>
        public string ReceivedToWarehouse { get; set; }

        /// <summary>
        /// Receiveing Reason Code
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>
        /// Receive Quantity (In Pcs)
        /// </summary>
        public decimal ReceivedQty { get; set; }

        /// <summary>
        /// Receive Date
        /// </summary>
        public DateTime ReceivedDate { get; set; }

        /// <summary>
        /// Received By
        /// </summary>
        public string ReceivedBy { get; set; }

        /// <summary>
        /// Lot No
        /// </summary>
        public string LotNo { get; set; }

        /// <summary>
        /// Expired Date
        /// </summary>
        public DateTime? ExpDate { get; set; }

        /// <summary>
        /// Manufacturing Date
        /// </summary>
        public DateTime? MfrDate { get; set; }

        // whs remarks 
        public string LineRemarks { get; set; }

        // 20211203
        // get ready for the batch issue
        public List<BatchNo> Batches { get; set; }
    }
}

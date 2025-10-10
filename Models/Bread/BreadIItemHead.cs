using System;

namespace KTC_SalesAppWAPI.Models.Bread
{
    // general holding the doc head from inventory transfer
    // invoice
    // delivery order

    public class BreadIItemHead
    {
        public int Id { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public string DocType { get; set; } // sap  inventory transfer, invoice / delivery order
        public int LinesCount { get; set; }

        public string SubSi { get; set; }
        public string SubsiId { get; set; }

        public int DocEntry { get; set; }
        public int DocNum { get; set; }

        public string ReceiverCardCode { get; set; }
        public string ReceiverCardName { get; set; }
        public string ReceiverCardAddress { get; set; }

        // for transporter invoice 
        public string TransporterCode { get; set; }
        public string TransporterCardName { get; set; }

        public int InvDocNum { get; set; }
        public int InvDocEntry { get; set; }

        // for inventory transfer doc
        public int ITDocNum { get; set; }
        public int ITDocEntry { get; set; }

        public Guid HeaderGuid { get; set; }

        public string IsTransfered { get; set; } = "";

        public string Ref2 { get; set; }

        public string TruckNo { get; set; }

        public int UsedTrayQty { get; set; } = 0;
    }
}

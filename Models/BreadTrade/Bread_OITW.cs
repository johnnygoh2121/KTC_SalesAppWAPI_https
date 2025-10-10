using System;

namespace KTC_SalesAppWAPI.Models.BreadTrade
{
    public class Bread_OITW
    {
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }
        public decimal OnHand { get; set; }
        public decimal IsCommited { get; set; }
        public decimal OnOrder { get; set; }
        public decimal Consig { get; set; }
        public decimal Counted { get; set; }
        public string WasCounted { get; set; }
        public short UserSign { get; set; }
        public decimal MinStock { get; set; }
        public decimal MaxStock { get; set; }
        public decimal MinOrder { get; set; }
        public decimal AvgPrice { get; set; }
        public string Locked { get; set; }
        public string BalInvntAc { get; set; }
        public string SaleCostAc { get; set; }
        public string TransferAc { get; set; }
        public string RevenuesAc { get; set; }
        public string VarianceAc { get; set; }
        public string DecreasAc { get; set; }
        public string IncreasAc { get; set; }
        public string ReturnAc { get; set; }
        public string ExpensesAc { get; set; }
        public string EURevenuAc { get; set; }
        public string EUExpensAc { get; set; }
        public string FrRevenuAc { get; set; }
        public string FrExpensAc { get; set; }
        public string ExmptIncom { get; set; }
        public string PriceDifAc { get; set; }
        public string ExchangeAc { get; set; }
        public string BalanceAcc { get; set; }
        public string PurchaseAc { get; set; }
        public string PAReturnAc { get; set; }
        public string PurchOfsAc { get; set; }
        public string ShpdGdsAct { get; set; }
        public string VatRevAct { get; set; }
        public decimal StockValue { get; set; }
        public string DecresGlAc { get; set; }
        public string IncresGlAc { get; set; }
        public string StokRvlAct { get; set; }
        public string StkOffsAct { get; set; }
        public string WipAcct { get; set; }
        public string WipVarAcct { get; set; }
        public string CostRvlAct { get; set; }
        public string CstOffsAct { get; set; }
        public string ExpClrAct { get; set; }
        public string ExpOfstAct { get; set; }
        public string Object { get; set; }
        public int logInstanc { get; set; }
        public DateTime createDate { get; set; }
        public short userSign2 { get; set; }
        public DateTime updateDate { get; set; }
        public string ARCMAct { get; set; }
        public string ARCMFrnAct { get; set; }
        public string ARCMEUAct { get; set; }
        public string ARCMExpAct { get; set; }
        public string APCMAct { get; set; }
        public string APCMFrnAct { get; set; }
        public string APCMEUAct { get; set; }
        public string RevRetAct { get; set; }
        public string NegStckAct { get; set; }
        public string StkInTnAct { get; set; }
        public string PurBalAct { get; set; }
        public string WhICenAct { get; set; }
        public string WhOCenAct { get; set; }
        public string WipOffset { get; set; }
        public string StockOffst { get; set; }
        public int DftBinAbs { get; set; }
        public string DftBinEnfd { get; set; }
        public string Freezed { get; set; }
        public int FreezeDoc { get; set; }
    }
}

using Dapper;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.SalesOrder;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers.DiApi
{
    public class BreadDiApi_Delivery
    {
        public string PostedDocEntry { get; set; }
        public string PostedDocNum { get; set; } // transfer doc num 
        public int GRPostDocEntry { get; set; } // for good receipt doc num
        public int ITPostDocEntry { get; set; } // for good receipt doc num

        string Common_DBConnStr { get; set; } = string.Empty;
        string errmsg { get; set; }
        int AttachedFileCnt { get; set; } = -1;

        Company oCompany { get; set; }
        DbInfo Db { get; set; }
        BreadDocHeader Doc { get; set; } = null;
        List<BreadDocDetail> DocDetails { get; set; } = null;

        string CurSapTableName = "OWTR";
        BoObjectTypes CurrentDocType = BoObjectTypes.oStockTransfer;

        // for cn good issues 
        Bread_CN_Ext CnDoc { get; set; }
        List<Bread_CN1_Ext> CnLines { get; set; }

        SqlConnection Conn { get; set; } // for module connection
        string GRRemark { get; set; }
        string Ref2 { get; set; }

        // for create of invoice or inventory transfer
        public BreadDiApi_Delivery(DbInfo db, BreadDocHeader head, List<BreadDocDetail> lines,
                            string common_DBConnStr, int filesCount, string curSapTableName,
                            BoObjectTypes currentDocType = BoObjectTypes.oDeliveryNotes,
                            string U_CSUS_REMARKS = "", string ref2 = "")
        {
            Doc = head;
            DocDetails = lines;
            Db = db;
            Common_DBConnStr = common_DBConnStr;
            AttachedFileCnt = filesCount;
            CurSapTableName = curSapTableName;
            CurrentDocType = currentDocType;
            Conn = new SqlConnection(common_DBConnStr);
            GRRemark = U_CSUS_REMARKS;
            Ref2 = ref2;
        }

        // parking the cn to good issue code 
        public BreadDiApi_Delivery(DbInfo db, Bread_CN_Ext head, List<Bread_CN1_Ext> cnLines,
                            string common_DBConnStr, int filesCount, string curSapTableName, BoObjectTypes currentDocType)
        {
            CnDoc = head;
            CnLines = cnLines;
            Db = db;
            Common_DBConnStr = common_DBConnStr;
            AttachedFileCnt = filesCount;
            CurSapTableName = curSapTableName;
            CurrentDocType = currentDocType;
            Conn = new SqlConnection(common_DBConnStr);
        }

        // for file attachment only
        public BreadDiApi_Delivery(DbInfo db,
                            string common_DBConnStr, string curSapTableName, BoObjectTypes currentDocType)
        {
            Db = db;
            Common_DBConnStr = common_DBConnStr;
            CurSapTableName = curSapTableName;
            CurrentDocType = currentDocType;
            Conn = new SqlConnection(common_DBConnStr);
        }

        public BreadDiApi_Delivery(DbInfo db)
        {
            Db = db;
        }

        //  2022 04 01
        public string CreateGoodIssueAndUpdate_CN(string remarks, int CnEntry)
        {
            string modName = $"[BreadDiApi_Delivery][CrtGdIss]";

            try
            {
                errmsg = connectSAP();
                // OIGE

                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg;
                }

                string webdb = Db.WEBDB;
                //oCompany.StartTransaction();

                Documents sapGiDoc = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit);
                if (!string.IsNullOrWhiteSpace($"{CnDoc.SapCnDocNum}"))
                {
                    sapGiDoc.Reference2 = $"{CnDoc.SapCnDocNum}";
                }

                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    sapGiDoc.Comments = remarks;
                    sapGiDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarks;
                }

                if (!string.IsNullOrWhiteSpace($"{CnDoc.DOCENTRY}"))
                {
                    sapGiDoc.UserFields.Fields.Item("U_SOENTRY").Value = $"{CnDoc.DOCENTRY}";
                }

                if (CnLines == null)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nThere is no detail lines found for this request. " +
                        $"Please contact sys admin to help.\n";
                    return errmsg;
                }

                var items = CnLines.GroupBy(x => x.ITEMCODE).Select(y => new
                {
                    ItemCode = y.First().ITEMCODE,
                    OrderQty = y.Sum(c => c.RtnQty),
                    ToWhsCode = y.First().WhsCode
                }).Distinct().ToList();

                var curLineCnt = 0;
                var addedLineCnt = 0; // for checking any added line

                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = CnLines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).Sum(x => x.RtnQty);
                    if (sumOfQty == 0) continue;

                    // 20221223 
                    // check qty enough for issue 
                    var onhand = GetWhsOnHand(Db.SAPDB, itemCode.ItemCode, itemCode.ToWhsCode);
                    if (onhand == -1)
                    {
                        //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        errmsg += $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                        return errmsg;
                    }

                    if (onhand < sumOfQty) continue; // if insufficient then continue to next line

                    // initial set to 
                    sapGiDoc.Lines.SetCurrentLine(curLineCnt);
                    sapGiDoc.Lines.ItemCode = itemCode.ItemCode;
                    sapGiDoc.Lines.Quantity = (double)sumOfQty;
                    sapGiDoc.Lines.WarehouseCode = itemCode.ToWhsCode;

                    var branch = GetWhsBranchCode(Db.SAPDB, itemCode.ToWhsCode);
                    if (branch != "")  // dim 1 - branch
                    {
                        sapGiDoc.Lines.CostingCode = branch; // dim 1
                    }

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;

                    // 20220313
                    // agency code
                    if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
                    {
                        sapGiDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
                    }

                    // 20220313
                    // get the GR reason code and GL account code 
                    var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGI_ReasonCode");  //"AppGR_ReasonCode");
                    if (reasonObj != null)
                    {
                        if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
                        {
                            sapGiDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
                        }
                        //if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
                        //{
                        //    sapGiDoc.Lines.AccountCode = reasonObj.GLCode;
                        //}
                    }

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = CnLines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.LotNo)) continue;
                            if (batch.QUANTITY == 0) continue;

                            sapGiDoc.Lines.BatchNumbers.BatchNumber = batch.LotNo;
                            sapGiDoc.Lines.BatchNumbers.Quantity = (double)batch.RtnQty;
                            sapGiDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    // 20220313
                    // agency for gi
                    //if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
                    //{
                    //    sapGiDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
                    //}

                    sapGiDoc.Lines.Add();
                    // continue next item line
                    curLineCnt++;
                    addedLineCnt++;
                }

                // no line being added 
                // no line issue out 
                if (addedLineCnt == 0)
                {
                    errmsg = $"No line qualified for issue. [CompName] {Db.COMPANYNAME}, [CN Entry# {CnEntry}] roll backed";
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return errmsg;
                }

                int addResult = sapGiDoc.Add();
                if (addResult != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n[CompName] {Db.COMPANYNAME}, [CN Entry# {CnEntry}]";
                }

                // when add result = 0
                int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                // added 27-Dec-2019
                // for file attachment
                if (AttachedFileCnt > 0)
                {
                    AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
                }

                Documents sapGiDoc1 = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit);
                var isLoadGi = sapGiDoc1.GetByKey(newKey);
                Documents sapCn = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oCreditNotes);
                var isLoadCn = sapCn.GetByKey(CnEntry);

                if (isLoadGi && isLoadCn)
                {
                    // load the cn and update the ref 2 
                    sapCn.Reference2 = $"{sapGiDoc1.DocNum}";
                    var remarkForCn = $"GOODS ISSUES #{sapGiDoc1.DocNum} FOR TRCN, {DateTime.Now:dd-MMM-yyyy}";
                    sapCn.Comments = remarkForCn;
                    sapCn.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarkForCn;
                    sapCn.Update();
                }
                else
                {
                    errmsg = $"Unable to update sap doc reference, fail to load document by DocEntry CN: {CnEntry}, GI:{newKey}";
                    return errmsg;
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapGiDoc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapGiDoc1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapCn);

                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                PostedDocEntry = $"{newKey}";    // docentry from the object 
                PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
                UpdateGIEntry(PostedDocEntry, $"{CnDoc.DOCENTRY}");
                return "";
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
               // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }

        }

        void UpdateGIEntry(string giDocEntry, string cnDocEntry)
        {
            try
            {
                var updateSql = @$"update {Db.WEBDB}..FTAPP_RCN 
                                   set GIENTRY = @giDocEntry 
                                   where docEntry = @cnDocEntry";

                using var conn = new SqlConnection(Common_DBConnStr);
                conn.Execute(updateSql, new
                {
                    giDocEntry = giDocEntry,
                    cnDocEntry = cnDocEntry
                });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }


        public string CreateInventryTransfer_ITT2It(string userType)
        {
            string modName = $"[BreadDiApi_Delivery][InvntryTrnsfr]";
            try
            {
                string errmsg = connectSAP();
                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg;
                }

                string webdb = Db.WEBDB;
                //oCompany.StartTransaction();
                StockTransfer sapDoc = (StockTransfer)oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

                // prepare the document header
                //string cardCode = Doc.CardCode;
                //sapDoc.CardCode = cardCode;

                // doc reference 2 -> special cut off the extra lenght
                //if (!string.IsNullOrWhiteSpace(Doc.Ref2))
                //{
                //    sapDoc.Reference2 = Doc.Ref2.Substring(0, 11);
                //}
                //if (!string.IsNullOrWhiteSpace(Doc.Comments))
                //{
                //    sapDoc.Comments = $"{Doc.Comments}";
                //}
                if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
                {
                    sapDoc.JournalMemo = $"{Doc.JrnlMemo}";
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardCode))
                {
                    sapDoc.UserFields.Fields.Item("U_Receiver_Code").Value = Doc.ODCardCode;
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardName))
                {
                    sapDoc.UserFields.Fields.Item("U_Receiver_Name").Value = Doc.ODCardName;
                }

                if (!string.IsNullOrWhiteSpace(GRRemark))
                {
                    sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = GRRemark;
                    sapDoc.Comments = $"{GRRemark}";
                }
                if (!string.IsNullOrWhiteSpace(Ref2))
                {
                    sapDoc.Reference2 = Ref2;
                }

                if (DocDetails == null)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
                    return errmsg;
                }

                var defFromWhs = SelectDefFromWh(Db.WEBDB);
                var items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
                {
                    ItemCode = y.First().ItemCode,
                    OrderQty = y.Sum(c => c.OrderQty),
                    ToWhsCode = y.First().ToWhsCode,
                    FromWhsCode = y.First().FromWhsCode
                }).Distinct().ToList();

                var curLineCnt = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
                    if (sumOfQty == 0) continue;

                    // initial set to 
                    sapDoc.Lines.SetCurrentLine(curLineCnt);
                    sapDoc.Lines.ItemCode = itemCode.ItemCode;
                    sapDoc.Lines.Quantity = sumOfQty;
                    sapDoc.Lines.WarehouseCode = itemCode.ToWhsCode;
                    sapDoc.Lines.FromWarehouseCode = itemCode.FromWhsCode;// defFromWhs; /// how 

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
                            if (batch.OrderQty == 0) continue;

                            sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
                            sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
                            sapDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    sapDoc.Lines.Add();
                    // continue next item line
                    curLineCnt++;
                }

                int addResult = sapDoc.Add();
                if (addResult != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                }

                // when add result = 0
                int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                // added 27-Dec-2019
                // commented on 27-Apr-2022 , transfer doc diapi not support file attachment
                // for file attachment
                //if (AttachedFileCnt > 0)
                //{
                //    AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
                //}

                StockTransfer itDoc1 = (StockTransfer)oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);
                var isLoad = itDoc1.GetByKey(newKey);
                if (!isLoad)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"Load IT docentry {newKey} fail";
                }

                #region create tray invoice 
                // for create invoice for tray 
                // create the invoice for tray for this seller
                //if (Doc.UsedTrayQty > 0 && $"{userType}".ToLower() == "tr")
                //{
                //    Documents trayInv = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInvoices);
                //    trayInv.CardCode = cardCode;

                //    var tray_itemCode = GetTrayItemCode(Db.SAPDB);
                //    if (string.IsNullOrWhiteSpace(tray_itemCode))
                //    {
                //        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                //        return $"{modName}\nQuery tray item code fail\nIT# {itDoc1.DocNum} @{Db.COMPANYNAME}";
                //    }

                //    trayInv.Lines.ItemCode = tray_itemCode;
                //    trayInv.Lines.Quantity = Doc.UsedTrayQty;
                //    var price = GetTrayPrice(Db.SAPDB, tray_itemCode);

                //    if (price > 0)
                //    {
                //        trayInv.Lines.Price = price;
                //    }

                //    var cardBranch = GetCardBranch(Db.SAPDB, cardCode);
                //    if (!string.IsNullOrWhiteSpace(cardBranch))  // dim 1
                //    {
                //        trayInv.Lines.CostingCode = cardBranch; // dim 1
                //    }

                //    trayInv.Lines.Add();
                //    trayInv.Reference2 = $"{itDoc1.DocNum}";
                //    trayInv.Comments = $"Base on IT# {itDoc1.DocNum}";
                //    sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value =$"{itDoc1.DocNum}";

                //    if (trayInv.Add() != 0)
                //    {
                //        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                //        return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                //    }

                //    // trayInv
                //    System.Runtime.InteropServices.Marshal.ReleaseComObject(trayInv);
                //}
                #endregion create tray invoice

                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                PostedDocEntry = newKey.ToString(); // docentry from the object 
                PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);

                UpdateCrPickedStatus($"{Doc.Guid}", "ITransfered", int.Parse(PostedDocNum));
                UpdateRequest($"{Doc.Guid}");
                UpdateHeader($"{Doc.Guid}");
                return "";
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
        }


        // conbine create gr then inventory transfer 
        // create the good received based on seller / transporter confirmed qty
        public string CreateGoodsReceive_InvtTransfer(string userType)
        {

            string modName = $"[BreadDiApi_Delivery][CreateGoodsReceive_InvtTransfer]";
            try
            {
                #region create gr
                string errmsg = connectSAP();
                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg;
                }

                string webdb = Db.WEBDB;
               // oCompany.StartTransaction();
                Documents grDoc = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
                Documents grDoc1 = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

                // prepare the document header
                //string cardCode = Doc.CardCode;
                //grDoc.CardCode = cardCode;

                if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
                {
                    grDoc.JournalMemo = $"{Doc.JrnlMemo}";
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardCode))
                {
                    grDoc.UserFields.Fields.Item("U_Receiver_Code").Value = Doc.ODCardCode;
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardName))
                {
                    grDoc.UserFields.Fields.Item("U_Receiver_Name").Value = Doc.ODCardName;
                }

                if (!string.IsNullOrWhiteSpace(GRRemark))
                {
                    grDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = GRRemark;
                    grDoc.Comments = $"{GRRemark}"; // 20220313 use auto remark from api
                }

                //20220320
                if (!string.IsNullOrWhiteSpace(Doc.TruckNo))
                {
                    grDoc.UserFields.Fields.Item("U_TRUCKNO").Value = Doc.TruckNo;
                }

                if (DocDetails == null)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
                    return errmsg;
                }

                var defFromWhs = SelectDefFromWh(Db.WEBDB);
                var items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
                {
                    ItemCode = y.First().ItemCode,
                    OrderQty = y.Sum(c => c.OrderQty),
                    ToWhsCode = y.First().ToWhsCode
                }).Distinct().ToList();


                int docSeries = GetGrDocSeries(Db.SAPDB);
                if (docSeries > 0)
                {
                    grDoc.Series = docSeries;
                }

                // targetWhs
                var targetWhs = GetEntryWhsCode(Db.SAPDB);

                // branch infor 
                var whsBranch = GetWhsBranchCode(Db.SAPDB, targetWhs);  //GetCardBranch(Db.SAPDB, Doc.CardCode); // dim1

                var curLineCnt = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
                    if (sumOfQty == 0) continue;

                    // initial set to 
                    grDoc.Lines.SetCurrentLine(curLineCnt);
                    grDoc.Lines.ItemCode = itemCode.ItemCode;
                    grDoc.Lines.Quantity = sumOfQty;
                    grDoc.Lines.WarehouseCode = targetWhs;

                    double price = GetGrPrice(Db.SAPDB, itemCode.ItemCode);
                    if (price > 0)
                    {
                        grDoc.Lines.Price = price;
                    }

                    // 20240731
                    if (!string.IsNullOrWhiteSpace(whsBranch))  // dim 1
                    {
                        grDoc.Lines.CostingCode = whsBranch; // dim 1
                    }

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;
                    // 20220313
                    // agency code
                    // 20240731
                    // reopen the line 
                    if (!string.IsNullOrWhiteSpace(itemInfo.CardCode)) // // agency code
                    {
                        grDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
                    }

                    var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGR_ReasonCode");
                    if (reasonObj != null)
                    {
                        if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
                        {
                            grDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
                        }

                        //if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
                        //{
                        //    grDoc.Lines.AccountCode = reasonObj.GLCode;
                        //}
                    }

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
                            if (batch.OrderQty == 0) continue;

                            grDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
                            grDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
                            grDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    grDoc.Lines.Add();
                    // continue next item line
                    curLineCnt++;
                }

                int addResult = grDoc.Add();
                if (addResult != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                }

                // when add result = 0
                int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                // added 27-Dec-2019
                // for file attachment
                if (AttachedFileCnt > 0)
                {
                    AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
                }

                var isLoadGrDone = grDoc1.GetByKey(newKey);
                if (!isLoadGrDone)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"Fail to load GR doc (GRD Key# {newKey})";
                }

                UpdateCrPickedStatus($"{Doc.Guid}", "GoodsReceived");
                UpdateRequest($"{Doc.Guid}");
                UpdateHeader($"{Doc.Guid}");

                GRRemark = $"GR-IT # {grDoc1.DocNum} , {DateTime.Now:dd-MMM-yy}";
                var grDocNum = grDoc1.DocNum;

                GRPostDocEntry = grDoc1.DocEntry; // 20240816

                int grDocEntry = newKey;
                #endregion create gr

                #region create inventory transfer
                StockTransfer itDoc = (StockTransfer)oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);
                StockTransfer itDoc1 = (StockTransfer)oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

                // prepare the document header
                //cardCode = Doc.CardCode;
                //itDoc.CardCode = cardCode;
                if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
                {
                    itDoc.JournalMemo = $"{Doc.JrnlMemo}";
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardCode))
                {
                    itDoc.UserFields.Fields.Item("U_Receiver_Code").Value = Doc.ODCardCode;
                }

                if (!string.IsNullOrWhiteSpace(Doc.ODCardName))
                {
                    itDoc.UserFields.Fields.Item("U_Receiver_Name").Value = Doc.ODCardName;
                }

                //20220320
                if (!string.IsNullOrWhiteSpace(Doc.TruckNo))
                {
                    itDoc.UserFields.Fields.Item("U_TRUCKNO").Value = Doc.TruckNo;
                }

                if (!string.IsNullOrWhiteSpace(GRRemark))
                {
                    itDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = GRRemark;//$"GR-IT # {PostedDocNum}, {DateTime.Now:dd-MMM-yy}"; ;
                    itDoc.Comments = GRRemark;
                }
                if (!string.IsNullOrWhiteSpace($"{grDocNum}"))
                {
                    itDoc.Reference2 = $"{grDocNum}";
                }

                if (DocDetails == null)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
                    return errmsg;
                }

                defFromWhs = SelectDefFromWh(Db.WEBDB);
                items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
                {
                    ItemCode = y.First().ItemCode,
                    OrderQty = y.Sum(c => c.OrderQty),
                    ToWhsCode = y.First().ToWhsCode
                }).Distinct().ToList();

                // 20250917
                itDoc.FromWarehouse = defFromWhs;
                itDoc.ToWarehouse = items?.First().ToWhsCode;                

                curLineCnt = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
                    if (sumOfQty == 0) continue;

                    // initial set to 
                    itDoc.Lines.SetCurrentLine(curLineCnt);
                    itDoc.Lines.ItemCode = itemCode.ItemCode;
                    itDoc.Lines.Quantity = sumOfQty;
                    itDoc.Lines.WarehouseCode = itemCode.ToWhsCode;
                    itDoc.Lines.FromWarehouseCode = defFromWhs; /// how 

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
                            if (batch.OrderQty == 0) continue;

                            itDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
                            itDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
                            itDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    itDoc.Lines.Add();
                    // continue next item line
                    curLineCnt++;
                }

                addResult = itDoc.Add();
                if (addResult != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                }

                // when add result = 0
                newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                // added 27-Dec-2019
                // for file attachment
                if (AttachedFileCnt > 0)
                {
                    AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
                }

                var isItDoc1Found = itDoc1.GetByKey(newKey);
                if (!isItDoc1Found)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"Fail to load inventory transfer (IT Key# {newKey}), base GR# {grDocNum}";
                }

                // 20250809
                var itDocNum = (int)itDoc1.DocNum;

                grDoc1.Reference2 = $"{itDoc1.DocNum}";
                grDoc1.Update();

                PostedDocEntry = $"{newKey}"; // docentry from the object 
                PostedDocNum = $"{itDoc1.DocNum}"; // GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
                ITPostDocEntry = newKey;

                #endregion

                // create the invoice for tray for this seller
                #region create tray invoice
                //if (Doc.UsedTrayQty > 0 && $"{userType}".ToLower() == "seller")
                //{
                //    Documents trayInv = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInvoices);
                //    trayInv.CardCode = cardCode;

                //    var tray_itemCode = GetTrayItemCode(Db.SAPDB);
                //    if (string.IsNullOrWhiteSpace(tray_itemCode))
                //    {
                //        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                //        return $"{modName}\nQuery tray item code fail\nIT# {itDoc1.DocNum} @{Db.COMPANYNAME}";
                //    }

                //    trayInv.Lines.ItemCode = tray_itemCode;
                //    trayInv.Lines.Quantity = Doc.UsedTrayQty;
                //    var price = GetTrayPrice(Db.SAPDB, tray_itemCode);

                //    if (price > 0)
                //    {
                //        trayInv.Lines.Price = price;
                //    }

                //    if (!string.IsNullOrWhiteSpace(cardBranch))  // dim 1
                //    {
                //        trayInv.Lines.CostingCode = cardBranch; // dim 1
                //    }

                //    trayInv.Lines.Add();
                //    trayInv.Reference2 = $"{itDoc1.DocNum}";
                //    trayInv.Comments = $"Base on IT# {itDoc1.DocNum}";
                //    trayInv.UserFields.Fields.Item("U_CSUS_REMARKS").Value = $"{itDoc1.DocNum}";

                //    if (trayInv.Add() != 0)
                //    {
                //        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                //        return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                //    }

                //    // trayInv
                //    System.Runtime.InteropServices.Marshal.ReleaseComObject(trayInv);
                //}
                #endregion create tray invoice

                System.Runtime.InteropServices.Marshal.ReleaseComObject(itDoc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(itDoc1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(grDoc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(grDoc1);
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                UpdateCrPickedStatus($"{Doc.Guid}", "ITransfered", itDocNum);
                UpdateRequest($"{Doc.Guid}");
                UpdateHeader($"{Doc.Guid}");

                // check both doc exist 
                // 20240816
                
                using var conn = new SqlConnection(Db.GetErpDbConnStr());
                var grFound_sp = @$"Select docentry from {Db.SAPDB}..OIGN with (nolock) 
                                    where DocEntry = @DocEntry
                                    and Ref2 = @PostedDocNum ; ";

                var foundGr = conn.ExecuteScalar<int>(grFound_sp, new { DocEntry = this.GRPostDocEntry, PostedDocNum });
                if (foundGr == 0)
                {
                    return $"Error reading the good receipt number from database, " +
                        $"please try post again. Thanks [E963GR] ITNum:{PostedDocNum}, GREntry{GRPostDocEntry}";
                }

                // -------------------------------------------

                // check Invetory transfer
                var itFound_sp = @$"Select DocEntry 
                                    from {Db.SAPDB}..OWTR with (nolock) 
                                    where DocEntry = @DocEntry 
                                    and ref2 = @GrDocNum ;";                      

                var foundIt = conn.ExecuteScalar<int>(itFound_sp, new { DocEntry = ITPostDocEntry, GrDocNum = grDocNum });

                if (foundIt == 0)
                {
                    return $"Error reading the transfer number from database, " +
                        $"please try post again. Thanks [E963IT] ITNum:{PostedDocNum}, GREntry{GRPostDocEntry}\";";
                }

                return "";
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
        }

        public string GetEntryWhsCode(string sapDb)
        {
            try
            {
                var sql = $@"select U_SetupValue 
                            from {sapDb}..[@APPSETUP] with (nolock)
                            Where U_SetupName = 'AppDefWhsCode' ";

                return Conn.Query<string>(sql).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return "";
            }
        }

        public string GetWhsBranchCode(string sapDb, string targetWhs)
        {
            try
            {
                var sql = $@"SELECT U_OcrCod1  
                             FROM {sapDb}..OWHS with (NOLOCK)
                             WHERE WhsCode = '{targetWhs}'";

                return Conn.Query<string>(sql).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return "";
            }
        }

        public double GetGrPrice(string sapDb, string itemCode)
        {
            try
            {
                //var sql = $@"select t0.Price
                //            from 
                //            {sapDb}..ITM1 t0 with (nolock)
                //            inner join  
                //            {sapDb}..[@APPSETUP]  t1 with (nolock) on t1.U_SetupValue = t0.PriceList
                //            Where U_SetupName = 'AppDefGRPriceListID'
                //            and ItemCode = '{itemCode}'";

                var sql = $@" select t2.Price
                            from {sapDb}..[@APPSETUP] t0 with(nolock)
                            inner join {sapDb}..OPLN t1 with(nolock) on t0.U_SetupName = 'AppDefGRPriceListName'  
							                            and t1.ListName = t0.U_SetupValue 
                            inner join {sapDb}..ITM1 t2 with(nolock) on t2.ItemCode = '{itemCode}'
							                            and  t2.PriceList = t1.ListNum ";

                return Conn.Query<double>(sql).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        int GetGrDocSeries(string sapDb)
        {
            try
            {
                var sql = $@"select U_SetupValue 
                            from {sapDb}..[@APPSETUP] with (nolock)
                            Where U_SetupName = 'AppGRDocSeries' ";

                return Conn.Query<int>(sql).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        public string CreateInv_Cn()
        {
            string modName = $"[BreadDiApi_Delivery][Inv_CN]";
            try
            {
                errmsg = connectSAP();

                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg;
                }

                string webdb = Db.WEBDB;  //AppUtilities.getWebDB();
                //oCompany.StartTransaction();
                Documents sapDoc = (Documents)oCompany.GetBusinessObject(this.CurrentDocType);

                // prepare the document header
                string cardCode = Doc.CardCode;     //ut.SafeGetStr(docHeader.Rows[0]["cardCode"]);
                sapDoc.CardCode = cardCode;

                var today = DateTime.Now;
                sapDoc.TaxDate = (Doc.DocDate == default) ? DateTime.Now : Doc.DocDate;
                sapDoc.TaxDate = (Doc.TaxDate == default) ? DateTime.Now : Doc.TaxDate;
                sapDoc.DocDueDate = (Doc.DueDate == default) ? DateTime.Now : Doc.DueDate;

                // doc reference 2 -> special cut off the extra lenght
                if (!string.IsNullOrWhiteSpace(Doc.Ref2))
                {
                    sapDoc.Reference2 = Doc.Ref2.Substring(0, 11);
                }

                if (!string.IsNullOrWhiteSpace(Doc.Comments))
                {
                    sapDoc.Comments = $"{Doc.Comments}";
                }

                if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
                {
                    sapDoc.JournalMemo = $"{Doc.JrnlMemo}";
                }

                if (string.IsNullOrWhiteSpace(Doc.NumberAtCard))
                {
                    sapDoc.NumAtCard = $"{Doc.NumberAtCard}";
                }

                var contactPersonName = Doc.ContactPerson;
                if (!string.IsNullOrWhiteSpace(contactPersonName))
                {
                    var resultContanctPersonId = GetContactPersonCode(cardCode, contactPersonName);
                    if (resultContanctPersonId > -1)
                    {
                        sapDoc.ContactPersonCode = resultContanctPersonId;
                    }
                }

                if (!string.IsNullOrWhiteSpace(Doc.ShipAddress))
                {
                    sapDoc.ShipToCode = Doc.ShipAddress; // ut.SafeGetStr(docHeader.Rows[0]["shipAddress"]);
                }

                if (!string.IsNullOrWhiteSpace(Doc.BillAddress))
                {
                    sapDoc.PayToCode = Doc.BillAddress; // ut.SafeGetStr(docHeader.Rows[0]["shipAddress"]);
                }
                if (Doc.BaseITEntry > 0) // indicate a link to SAP Inventory transfer doc entry
                {
                    sapDoc.UserFields.Fields.Item("U_SOENTRY").Value = Doc.BaseITEntry;
                }

                sapDoc.HandWritten = BoYesNoEnum.tNO;
                if (DocDetails == null)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nDetails lines were empty. Create of invoice fail.";
                    return errmsg;
                }

                var items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
                {
                    ItemCode = y.First().ItemCode,
                    OrderQty = y.Sum(c => c.OrderQty),
                    FromWhsCode = y.First().FromWhsCode
                }).Distinct().ToList();


                var defWhs = SelectDefFromWh(Db.WEBDB);
                var cardBranch = GetCardBranch(Db.SAPDB, Doc.CardCode); // dim1

                sapDoc.Series = CurrentDocType == BoObjectTypes.oInvoices ?
                            GetArInvoiceDocSeries(Db.WEBDB, defWhs) : // get predefine doc series 
                            GetArCN_DocSeries(Db.WEBDB, defWhs);

                var curLineCnt = 0;

                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
                    if (sumOfQty == 0) continue;

                    if (cardBranch != "")  // dim 1
                    {
                        sapDoc.Lines.CostingCode = cardBranch; // dim 1
                    }

                    // initial set to 
                    sapDoc.Lines.SetCurrentLine(curLineCnt);
                    sapDoc.Lines.ItemCode = itemCode.ItemCode;
                    sapDoc.Lines.Quantity = sumOfQty;

                    if (!string.IsNullOrWhiteSpace(itemCode.FromWhsCode))
                    {
                        sapDoc.Lines.WarehouseCode = itemCode.FromWhsCode;
                    }
                    else
                    {
                        sapDoc.Lines.WarehouseCode = defWhs;
                    }

                    sapDoc.Lines.CostingCode = cardBranch; // dimension 1

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
                            if (batch.OrderQty == 0) continue;

                            sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
                            sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
                            sapDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    sapDoc.Lines.Add();
                    // continue next item line
                    curLineCnt++;
                }

                int addResult = sapDoc.Add();
                if (addResult != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}";
                    return errmsg;
                }

                // when add result = 0
                int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                // added 27-Dec-2019
                // for file attachment
                if (AttachedFileCnt > 0)
                {
                    AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                PostedDocEntry = newKey.ToString(); // docentry from the object 
                PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
                UpdateCrPickedStatus($"{Doc.Guid}", "Invoiced");
                UpdateRequest($"{Doc.Guid}");
                UpdateHeader($"{Doc.Guid}");

                return "";
            }

            catch (Exception e)
            {
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

                errmsg = $"{e.Message}\n{e.StackTrace}";
                return errmsg;
            }

        }
        int GetArInvoiceDocSeries(string webDb, string whsCode)
        {
            int defSeries = 4;
            try
            {
                var sql = $@"select INVARINVSERIES 
                            from {webDb}..SAPREC with (nolock)
                                Where ACTIVATEINV = 'Y'
                                      and WHSCODE = @whsCode ";

                var seriesRes = Conn.Query<int>(sql, new
                {
                    whsCode = whsCode
                }).FirstOrDefault();

                if (seriesRes >= 0)
                {
                    return defSeries;
                }
                return seriesRes;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return defSeries;
            }
        }


        int GetArCN_DocSeries(string webDb, string whsCode)
        {
            int defSeries = 5;
            try
            {
                var sql = $@"select CNARCNSERIES 
                            from {webDb}..SAPREC with (nolock)
                                Where ACTIVATEINV = 'Y'
                                      and WHSCODE = @whsCode ";

                var seriesRes = Conn.Query<int>(sql, new
                {
                    whsCode = whsCode
                }).FirstOrDefault();

                if (seriesRes >= 0)
                {
                    return defSeries;
                }
                return seriesRes;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return defSeries;
            }
        }

        string GetCardBranch(string sapDb, string cardCode)
        {
            try
            {
                var sql = $@"SELECT t2.PrcCode [BranchCode]
                        FROM {sapDb}..OCRD t0 with (nolock) 
                            LEFT JOIN {sapDb}..OTER t1 with (nolock) on t0.Territory = t1.territryID
							LEFT JOIN {sapDb}..OPRC t2 with (nolock) on t0.U_COSTCTR = t2.PrcCode
                        WHERE CardCode= @cardCode";

                using var conn = new SqlConnection(Common_DBConnStr);
                var result = conn.Query<string>(sql, new
                {
                    cardCode = cardCode
                }).FirstOrDefault();

                if (result == null) // query the ocrd u_cost center
                {
                    sql = @$"select U_COSTCTR 
                            from {sapDb}..OCRD with (nolock)
                            where cardcode = @CardCode";

                    result = conn.Query<string>(sql, new { CardCode = cardCode }).FirstOrDefault();
                }

                return result;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        decimal GetWhsOnHand(string dbSap, string itemCode, string whsCode)
        {
            try
            {
                var sp_checkOnhand = @$"select OnHand 
                                    from {dbSap}..OITW with (nolock)
                                    where ItemCode = @itemCode
                                    and WhsCode = @whsCode ";

                using var conn = new SqlConnection(Common_DBConnStr);
                var result = conn.ExecuteScalar<decimal>(sp_checkOnhand, new
                {
                    itemCode = itemCode,
                    whsCode = whsCode
                });

                return result;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

        int GetContactPersonCode(string cardCode, string contactName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactName))
                {
                    return -1;
                }

                string sql = $"SELECT cntctCode" +
                            $" FROM {Db.WEBDB}..OCPR" +
                            $" WHERE CardCode = @cardCode" +
                            $" AND Name = @contactName ";

                return Conn.ExecuteScalar<int>(sql, new { cardCode, contactName });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }
        string SelectDefFromWh(string webDb)
        {
            try
            {
                var sql = @"exec sp_SelectDefFromWh @webDb";
                return Conn.Query<string>(sql, new
                {
                    webDb = webDb
                }).FirstOrDefault();

            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return string.Empty;
            }
        }

        GR_GI_Reacon_GLCode GetReason_GLCodes(string sapDb, string setupname) // GR and G1
        {
            try
            {
                var sql = $@"select t1.Code [ReasonCode], t1.U_CSUS_GL [GLCode]
                            from {sapDb}..[@APPSETUP] t0 
                            inner join {sapDb}..[@CSUS_REASON_GLACCT] t1 on t1.Name = t0.U_SetupValue 
                            Where t0.U_SetupName = @setupname";

                using var conn = new SqlConnection(Common_DBConnStr);
                return conn.Query<GR_GI_Reacon_GLCode>(sql, new { setupname }).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        OITM_Ext GetItemInfo(string webDb, string itemCode)
        {
            try
            {
                var sql = @"sp_GetItemSetup @webDb, @itemCode";
                using var conn = new SqlConnection(Common_DBConnStr);
                return conn.Query<OITM_Ext>(sql, new
                {
                    webDb = webDb,
                    itemCode = itemCode
                }).FirstOrDefault();
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        string GetDocNumberbyDoEntry(string docEntry, string tableName)
        {
            try
            {
                string sql = $@"SELECT DocNum  
                               FROM {Db.SAPDB}..{tableName} 
                               WHERE DocEntry = @docEntry";

                var result = Conn.ExecuteScalar<string>(sql, new { docEntry = docEntry });
                return result;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return "-1";
            }
        }
        public void UpdateHeader(string guid)
        {
            try
            {
                // update the request table 
                var updateSql = $@"Update {Db.WEBDB}..FTAPP_MWDocHeader 
                                    Set BaseITEntry = @docEntry 
                                      , SapDocNo = @docNum
                                      , DocNum = @docNum
                                    Where GUID = @guid";
                Conn.Execute(updateSql, new
                {
                    docEntry = PostedDocEntry,
                    docNum = PostedDocNum,
                    guid = guid
                });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }
        public void UpdateRequest(string guid)
        {
            try
            {
                // update the request table 
                var updateSql = $@"Update {Db.WEBDB}..FTAPP_MWRequest Set 
                                         Status = 'SUCCESS' 
                                        ,CompletedTime = GETDATE()
                                        ,SAPDocNumber = @postedDocNum                                        
                                        Where GUID = @guid";
                Conn.Execute(updateSql, new
                {
                    postedDocNum = PostedDocNum,
                    guid = guid
                });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }
        public void UpdateCrPickedStatus(string guid, string docStatus)
        {
            try
            {
                var updateSql = @"Update FTAPP_BreadDODraftHead 
                                  Set DocStatus = @docStatus
                                  Where HeadGuid = @guid";

                var conn = Conn.Execute(updateSql, new
                {
                    docStatus = docStatus,
                    guid = guid
                });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }

        // 20250809
        public void UpdateCrPickedStatus(string guid, string docStatus, int itDocNum)
        {
            try
            {
                var updateSql = @"Update FTAPP_BreadDODraftHead 
                                  Set DocStatus = @docStatus, ITDocNum = @ITDocNum
                                  Where HeadGuid = @guid";

                var conn = Conn.Execute(updateSql, new
                {
                    docStatus = docStatus,
                    ITDocNum = itDocNum,
                    guid = guid
                });
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }


        void AddFileAttachment(int docEntry, string guidHeader, BoObjectTypes objectTypes)
        {
            try
            {
                var files = GetAttachmentFiles(guidHeader);
                if (files == null) return;
                if (files.Count == 0) return;

                Documents doc = (Documents)oCompany.GetBusinessObject(objectTypes);
                doc.GetByKey(docEntry);

                var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                int fileCnt = 0;

                files.ForEach(file =>
                {
                    if (File.Exists(file.ServerSavedPath))
                    {
                        attachedments.Lines.Add();
                        attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(file.ServerSavedPath);
                        attachedments.Lines.FileExtension = Path.GetExtension(file.ServerSavedPath).Substring(1);
                        attachedments.Lines.SourcePath = Path.GetDirectoryName(file.ServerSavedPath);
                        attachedments.Lines.Override = BoYesNoEnum.tYES;
                        fileCnt++;
                    }
                });

                if (fileCnt != files.Count)
                {
                    errmsg = $"Marketing Doc Attachments {oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}";
                    return;
                }

                if (attachedments.Add() == 0)
                {
                    int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                    doc.AttachmentEntry = iAttEntry;
                    doc.Update();
                    //errmsg = $"{files.Count} File(s) Attached, DocNo: {PostedDocNum}\n{Doc.Guid}";
                    return;
                }

                // else                                    
                //errmsg = $"Doc Attachments {oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}";
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
            }
        }

        // 20220222
        // for direct attach a svr file into a sap doc
        public string Add_FileAttachment(int docEntry, string svyPhyPath, BoObjectTypes objectTypes)
        {
            try
            {
                if (Db == null)
                {
                    return "Invalid db";
                }

                errmsg = connectSAP();
                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg;
                }

                string webdb = Db.WEBDB;
                //if (!oCompany.InTransaction)
                //{
                //    oCompany.StartTransaction();
                //}

                Documents doc = (Documents)oCompany.GetBusinessObject(objectTypes);
                var isLoaded = doc.GetByKey(docEntry);
                if (!isLoaded)
                {

                    errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return errmsg;
                }

                var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);

                if (File.Exists(svyPhyPath))
                {
                    attachedments.Lines.Add();
                    attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                    attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                    attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                    attachedments.Lines.Override = BoYesNoEnum.tYES;

                    if (attachedments.Add() == 0)
                    {
                        int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                        doc.AttachmentEntry = iAttEntry;
                        doc.Update();

                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                        //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                        return "";
                    }
                }

                errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
        }

        List<Bread_FileUpload> GetAttachmentFiles(string guid) // query from midware
        {
            try
            {
                var sqlQuery = $@"SELECT * 
                                 FROM {Db.WEBDB}..FTAPP_MWFileUpload 
                                 WHERE HeaderGuid = @guid";

                var results = Conn.Query<Bread_FileUpload>(sqlQuery, new { guid }).ToList();
                return results;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        public string connectSAP()
        {
            // 20220426
            if (Program.SapCompanies == null) Program.SapCompanies = new Dictionary<string, Company>();

            // 20220426
            // look for the connected sap object 
            var isListed = Program.SapCompanies.ContainsKey(Db.COMPANYNAME);
            if (isListed)
            {
                oCompany = Program.SapCompanies[Db.COMPANYNAME];
            }

            //20220426
            // if company object is null 
            if (oCompany == null) oCompany = new SAPbobsCOM.Company();
            if (oCompany.Connected)
            {
                if (Program.SapCompanies == null) Program.SapCompanies = new Dictionary<string, Company>();
                Program.SapCompanies.Remove(Db.COMPANYNAME);
                Program.SapCompanies.Add(Db.COMPANYNAME, oCompany);
                return "";
            }

            if (oCompany == null) oCompany = new SAPbobsCOM.Company();
            if (oCompany.Connected) return "";
            string errmsg = "";
            try
            {
                string sapdb = $"{Db.SAPDB}".Trim();
                string sapserver = $"{Db.SAPSERVER}".Trim();
                string sapuser = $"{Db.SAPUSERNAME}".Trim();
                string sappassword = $"{Db.SAPPASSWORD}".Trim();
                string sqluser = $"{Db.WEBDBUSR}".Trim();
                string sqlpassword = $"{Db.WEBDBPASS}".Trim();
                string license = $"{Db.SAPLICENSE}".Trim();
                int dbtype = Db.SAP_DbType; //  "6";  

                oCompany.CompanyDB = sapdb;
                oCompany.DbPassword = sqlpassword;
                oCompany.DbServerType = (BoDataServerTypes)dbtype;
                oCompany.DbUserName = sqluser;
                oCompany.language = SAPbobsCOM.BoSuppLangs.ln_English;
                //oCompany.LicenseServer = license;                
                oCompany.SLDServer = license;

                oCompany.Password = sappassword;
                oCompany.Server = sapserver;
                oCompany.UserName = sapuser;
                oCompany.UseTrusted = false;

                int rtncode = oCompany.Connect();
                if (rtncode != 0)
                {
                    errmsg = oCompany.GetLastErrorDescription();
                    if (errmsg == "") errmsg = "Unable to connect to SAP, unknown reason!";

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oCompany);
                    oCompany = null;
                }

                //20220426
                // keep the sap connection in directory list 
                if (Program.SapCompanies == null) Program.SapCompanies = new Dictionary<string, Company>();
                Program.SapCompanies.Remove(Db.COMPANYNAME);
                Program.SapCompanies.Add(Db.COMPANYNAME, oCompany);
                return errmsg;
            }
            catch (Exception ex)
            {
                errmsg = ex.Message;
                return errmsg;
            }
        }
    }
}


#region hided code
//public BreadDiApi_Delivery(DbInfo db, string common_DBConnStr, string curSAPTableName)
//{
//    Db = db;
//    CurSapTableName = curSAPTableName;
//    Conn = new SqlConnection(common_DBConnStr);
//}

//public string CreateGoodIssue(string remarks)
//{
//    string modName = $"[BreadDiApi_Delivery][CrtGdIss]";

//    try
//    {
//        connectSAP();
//        errmsg = "";
//        // OIGE

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();
//        Documents sapDoc = oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit);

//        // prepare the document header
//        if (!string.IsNullOrWhiteSpace(CnDoc.CARDCODE))
//        {
//            sapDoc.CardCode = CnDoc.CARDCODE;
//        }
//        if (!string.IsNullOrWhiteSpace(CnDoc.CARDNAME))
//        {
//            sapDoc.CardName = CnDoc.CARDNAME;
//        }

//        if (!string.IsNullOrWhiteSpace($"{CnDoc.SapCnDocNum}"))
//        {
//            sapDoc.Reference2 = $"{CnDoc.SapCnDocNum}";
//        }

//        if (!string.IsNullOrWhiteSpace(remarks))
//        {
//            sapDoc.Comments = remarks;
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarks;
//        }


//        if (!string.IsNullOrWhiteSpace($"{CnDoc.DOCENTRY}"))
//        {
//            sapDoc.UserFields.Fields.Item("U_SOENTRY").Value = $"{CnDoc.DOCENTRY}";
//        }

//        if (CnLines == null)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
//            return errmsg;
//        }

//        var items = CnLines.GroupBy(x => x.ITEMCODE).Select(y => new
//        {
//            ItemCode = y.First().ITEMCODE,
//            OrderQty = y.Sum(c => c.RtnQty),
//            ToWhsCode = y.First().WhsCode
//        }).Distinct().ToList();

//        var cardBranch = GetCardBranch(Db.SAPDB, CnDoc.CARDCODE);
//        var curLineCnt = 0;
//        for (int i = 0; i < items.Count; i++)
//        {
//            var itemCode = items[i];
//            if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

//            // get the sum of the line order
//            var sumOfQty = CnLines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).Sum(x => x.RtnQty);
//            if (sumOfQty == 0) continue;

//            // initial set to 
//            sapDoc.Lines.SetCurrentLine(curLineCnt);
//            sapDoc.Lines.ItemCode = itemCode.ItemCode;
//            sapDoc.Lines.Quantity = (double)sumOfQty;
//            sapDoc.Lines.WarehouseCode = itemCode.ToWhsCode;

//            if (cardBranch != "")  // dim 1
//            {
//                sapDoc.Lines.CostingCode = cardBranch; // dim 1
//            }

//            var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
//            if (itemInfo == null) continue;

//            // 20220313
//            // agency code
//            if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
//            {
//                sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
//            }

//            // 20220313
//            // get the GR reason code and GL account code 
//            var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGI_ReasonCode");  //"AppGR_ReasonCode");
//            if (reasonObj != null)
//            {
//                if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
//                {
//                    sapDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
//                }
//                if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
//                {
//                    sapDoc.Lines.AccountCode = reasonObj.GLCode;
//                }
//            }

//            if ($"{itemInfo.ManBtchNum}" == "Y")
//            {
//                // get all it line 
//                var batches = CnLines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).ToList();
//                //var batLineCount = 0;
//                for (int y = 0; y < batches.Count; y++)
//                {
//                    var batch = batches[y];
//                    if (batch == null) continue;

//                    if (string.IsNullOrWhiteSpace(batch.LotNo)) continue;
//                    if (batch.QUANTITY == 0) continue;

//                    sapDoc.Lines.BatchNumbers.BatchNumber = batch.LotNo;
//                    sapDoc.Lines.BatchNumbers.Quantity = (double)batch.RtnQty;
//                    sapDoc.Lines.BatchNumbers.Add();
//                }
//            }

//            // 20220313
//            // agency for gi
//            if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
//            {
//                sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
//            }

//            sapDoc.Lines.Add();
//            // continue next item line
//            curLineCnt++;
//        }

//        int addResult = sapDoc.Add();
//        if (addResult != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//        }

//        // when add result = 0
//        int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

//        // added 27-Dec-2019
//        // for file attachment
//        if (AttachedFileCnt > 0)
//        {
//            AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
//        }

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        PostedDocEntry = newKey.ToString(); // docentry from the object 
//        PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
//        UpdateGIEntry(PostedDocEntry, $"{CnDoc.DOCENTRY}");

//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }

//}

//public string CreateInventryTransfer()
//{
//    string modName = $"[BreadDiApi_Delivery][InvntryTrnsfr]";
//    try
//    {
//        string errmsg = "";
//        connectSAP();

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();
//        StockTransfer sapDoc = (StockTransfer)oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);

//        // prepare the document header
//        string cardCode = Doc.CardCode;
//        sapDoc.CardCode = cardCode;

//        // doc reference 2 -> special cut off the extra lenght
//        //if (!string.IsNullOrWhiteSpace(Doc.Ref2))
//        //{
//        //    sapDoc.Reference2 = Doc.Ref2.Substring(0, 11);
//        //}
//        //if (!string.IsNullOrWhiteSpace(Doc.Comments))
//        //{
//        //    sapDoc.Comments = $"{Doc.Comments}";
//        //}
//        if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
//        {
//            sapDoc.JournalMemo = $"{Doc.JrnlMemo}";
//        }

//        if (!string.IsNullOrWhiteSpace(Doc.ODCardCode))
//        {
//            sapDoc.UserFields.Fields.Item("U_Receiver_Code").Value = Doc.ODCardCode;
//        }

//        if (!string.IsNullOrWhiteSpace(Doc.ODCardName))
//        {
//            sapDoc.UserFields.Fields.Item("U_Receiver_Name").Value = Doc.ODCardName;
//        }

//        if (!string.IsNullOrWhiteSpace(GRRemark))
//        {
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = GRRemark;
//            sapDoc.Comments = $"{GRRemark}";
//        }
//        if (!string.IsNullOrWhiteSpace(Ref2))
//        {
//            sapDoc.Reference2 = Ref2;
//        }


//        if (DocDetails == null)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
//            return errmsg;
//        }

//        var defFromWhs = SelectDefFromWh(Db.WEBDB);
//        var items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
//        {
//            ItemCode = y.First().ItemCode,
//            OrderQty = y.Sum(c => c.OrderQty),
//            ToWhsCode = y.First().ToWhsCode
//        }).Distinct().ToList();

//        var curLineCnt = 0;
//        for (int i = 0; i < items.Count; i++)
//        {
//            var itemCode = items[i];
//            if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

//            // get the sum of the line order
//            var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
//            if (sumOfQty == 0) continue;

//            // initial set to 
//            sapDoc.Lines.SetCurrentLine(curLineCnt);
//            sapDoc.Lines.ItemCode = itemCode.ItemCode;
//            sapDoc.Lines.Quantity = sumOfQty;
//            sapDoc.Lines.WarehouseCode = itemCode.ToWhsCode;
//            sapDoc.Lines.FromWarehouseCode = defFromWhs; /// how 

//            var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
//            if (itemInfo == null) continue;

//            if ($"{itemInfo.ManBtchNum}" == "Y")
//            {
//                // get all it line 
//                var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
//                //var batLineCount = 0;
//                for (int y = 0; y < batches.Count; y++)
//                {
//                    var batch = batches[y];
//                    if (batch == null) continue;

//                    if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
//                    if (batch.OrderQty == 0) continue;

//                    sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
//                    sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
//                    sapDoc.Lines.BatchNumbers.Add();
//                }
//            }

//            sapDoc.Lines.Add();
//            // continue next item line
//            curLineCnt++;
//        }

//        int addResult = sapDoc.Add();
//        if (addResult != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//        }

//        // when add result = 0
//        int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

//        // added 27-Dec-2019
//        // for file attachment
//        if (AttachedFileCnt > 0)
//        {
//            AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
//        }

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        PostedDocEntry = newKey.ToString(); // docentry from the object 
//        PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);

//        UpdateCrPickedStatus($"{Doc.Guid}", "ITransfered");
//        UpdateRequest($"{Doc.Guid}");
//        UpdateHeader($"{Doc.Guid}");

//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }
//}

// update good receipt ref 2 
//public string UpdateDocRef2(int docEntry, BoObjectTypes sapDocType, string ref2Value, string docRemarks = "")
//{
//    try
//    {
//        string errmsg = "";
//        connectSAP();
//        oCompany.StartTransaction();

//        Documents sapDoc = (Documents)oCompany.GetBusinessObject(sapDocType);
//        var isLoad = sapDoc.GetByKey(docEntry);
//        if (isLoad == false)
//        {
//            return $"Unable to update sap doc reference, fail to load document by docentry {docEntry}";
//        }

//        sapDoc.Reference2 = ref2Value;

//        if (!string.IsNullOrWhiteSpace(docRemarks))
//        {
//            sapDoc.Comments = docRemarks;
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = docRemarks;
//        }

//        sapDoc.Update();

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }
//}


// create the good received based on seller / transporter confirmed qty
//public string CreateGoodsReceive()
//{
//    string modName = $"[BreadDiApi_Delivery][CreateGoodsReceive]";
//    try
//    {
//        string errmsg = "";
//        connectSAP();

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();
//        Documents sapDoc = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

//        // prepare the document header
//        string cardCode = Doc.CardCode;
//        sapDoc.CardCode = cardCode;

//        // doc reference 2 -> special cut off the extra lenght
//        if (!string.IsNullOrWhiteSpace(Doc.Ref2))
//        {
//            sapDoc.Reference2 = Doc.Ref2.Substring(0, 11);
//        }
//        // commented on 20220313
//        // use auto remark generated from api
//        //if (!string.IsNullOrWhiteSpace(Doc.Comments))
//        //{
//        //    sapDoc.Comments = $"{Doc.Comments}";
//        //}
//        if (!string.IsNullOrWhiteSpace(Doc.JrnlMemo))
//        {
//            sapDoc.JournalMemo = $"{Doc.JrnlMemo}";
//        }

//        if (!string.IsNullOrWhiteSpace(Doc.ODCardCode))
//        {
//            sapDoc.UserFields.Fields.Item("U_Receiver_Code").Value = Doc.ODCardCode;
//        }

//        if (!string.IsNullOrWhiteSpace(Doc.ODCardName))
//        {
//            sapDoc.UserFields.Fields.Item("U_Receiver_Name").Value = Doc.ODCardName;
//        }

//        if (!string.IsNullOrWhiteSpace(GRRemark))
//        {
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = GRRemark;
//            sapDoc.Comments = $"{GRRemark}"; // 20220313 use auto remark from api
//        }

//        if (DocDetails == null)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
//            return errmsg;
//        }

//        var defFromWhs = SelectDefFromWh(Db.WEBDB);
//        var items = DocDetails.GroupBy(x => x.ItemCode).Select(y => new
//        {
//            ItemCode = y.First().ItemCode,
//            OrderQty = y.Sum(c => c.OrderQty),
//            ToWhsCode = y.First().ToWhsCode
//        }).Distinct().ToList();


//        int docSeries = GetGrDocSeries(Db.SAPDB);
//        if (docSeries > 0)
//        {
//            sapDoc.Series = docSeries;
//        }

//        // targetWhs
//        var targetWhs = GetEntryWhsCode(Db.SAPDB);

//        // branch infor 
//        var cardBranch = GetCardBranch(Db.SAPDB, Doc.CardCode); // dim1
//        var curLineCnt = 0;
//        for (int i = 0; i < items.Count; i++)
//        {
//            var itemCode = items[i];
//            if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

//            // get the sum of the line order
//            var sumOfQty = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.OrderQty);
//            if (sumOfQty == 0) continue;

//            // initial set to 
//            sapDoc.Lines.SetCurrentLine(curLineCnt);
//            sapDoc.Lines.ItemCode = itemCode.ItemCode;
//            sapDoc.Lines.Quantity = sumOfQty;
//            sapDoc.Lines.WarehouseCode = targetWhs;

//            double price = GetGrPrice(Db.SAPDB, itemCode.ItemCode);
//            if (price > 0)
//            {
//                sapDoc.Lines.Price = price;
//            }

//            if (!string.IsNullOrWhiteSpace(cardBranch))  // dim 1
//            {
//                sapDoc.Lines.CostingCode = cardBranch; // dim 1
//            }

//            var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
//            if (itemInfo == null) continue;
//            // 20220313
//            // agency code
//            if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
//            {
//                sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
//            }

//            var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGR_ReasonCode");
//            if (reasonObj != null)
//            {
//                if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
//                {
//                    sapDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
//                }
//                if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
//                {
//                    sapDoc.Lines.AccountCode = reasonObj.GLCode;
//                }
//            }

//            if ($"{itemInfo.ManBtchNum}" == "Y")
//            {
//                // get all it line 
//                var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
//                //var batLineCount = 0;
//                for (int y = 0; y < batches.Count; y++)
//                {
//                    var batch = batches[y];
//                    if (batch == null) continue;

//                    if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
//                    if (batch.OrderQty == 0) continue;

//                    sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
//                    sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
//                    sapDoc.Lines.BatchNumbers.Add();
//                }
//            }



//            sapDoc.Lines.Add();
//            // continue next item line
//            curLineCnt++;
//        }

//        int addResult = sapDoc.Add();
//        if (addResult != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//        }

//        // when add result = 0
//        int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

//        // added 27-Dec-2019
//        // for file attachment
//        if (AttachedFileCnt > 0)
//        {
//            AddFileAttachment(newKey, $"{Doc.Guid}", CurrentDocType);
//        }

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        PostedDocEntry = newKey.ToString(); // docentry from the object 
//        PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);

//        UpdateCrPickedStatus($"{Doc.Guid}", "GoodsReceived");
//        UpdateRequest($"{Doc.Guid}");
//        UpdateHeader($"{Doc.Guid}");

//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }

//}

// create good received for distributor before invoice
//public string CreateGoodsReceive_Dist_Invoice(Bread_OINV_Ext invDoc,
//    List<Bread_INV1_Ext> lines, string remarks)
//{
//    string modName = $"[BreadDiApi_Delivery][CreateGoodsReceive_Dist]";
//    try
//    {
//        string errmsg = "";
//        connectSAP();
//        errmsg = "";

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();
//        Documents sapDoc = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

//        // prepare the document header
//        string cardCode = invDoc.CARDCODE;
//        sapDoc.CardCode = cardCode;

//        // doc reference 2 -> special cut off the extra lenght
//        //if (!string.IsNullOrWhiteSpace(invDoc.CUSTREF))
//        //{
//        //    sapDoc.Reference2 = invDoc.CUSTREF.Substring(0, 11);
//        //}
//        // commented on 20220313
//        // use auto remark generated from api
//        //if (!string.IsNullOrWhiteSpace(Doc.Comments))
//        //{
//        //    sapDoc.Comments = $"{Doc.Comments}";
//        //}
//        if (!string.IsNullOrWhiteSpace(invDoc.REMARKS))
//        {
//            sapDoc.JournalMemo = $"{invDoc.REMARKS}";
//        }

//        if (!string.IsNullOrWhiteSpace(remarks))
//        {
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarks;
//            sapDoc.Comments = $"{remarks}"; // 20220313 use auto remark from api
//        }

//        // cross reference
//        if (!string.IsNullOrWhiteSpace(Ref2))
//        {
//            sapDoc.Reference2 = Ref2;
//        }

//        if (lines == null)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
//            return errmsg;
//        }

//        var items = lines.GroupBy(x => x.ITEMCODE).Select(y => new
//        {
//            ItemCode = y.First().ITEMCODE,
//            OrderQty = y.Sum(c => c.QUANTITY)
//        }).Distinct().ToList();

//        int docSeries = GetGrDocSeries(Db.SAPDB);
//        if (docSeries > 0)
//        {
//            sapDoc.Series = docSeries;
//        }

//        // targetWhs
//        var targetWhs = GetEntryWhsCode(Db.SAPDB);

//        // branch infor 
//        var cardBranch = GetCardBranch(Db.SAPDB, invDoc.CARDCODE); // dim1
//        var curLineCnt = 0;
//        for (int i = 0; i < items.Count; i++)
//        {
//            var itemCode = items[i];
//            if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

//            // get the sum of the line order
//            var sumOfQty = (double)lines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).Sum(x => x.QUANTITY);
//            if (sumOfQty == 0) continue;

//            // initial set to 
//            sapDoc.Lines.SetCurrentLine(curLineCnt);
//            sapDoc.Lines.ItemCode = itemCode.ItemCode;
//            sapDoc.Lines.Quantity = sumOfQty;
//            sapDoc.Lines.WarehouseCode = targetWhs;

//            double price = GetGrPrice(Db.SAPDB, itemCode.ItemCode);
//            if (price > 0)
//            {
//                sapDoc.Lines.Price = price;
//            }

//            if (!string.IsNullOrWhiteSpace(cardBranch))  // dim 1
//            {
//                sapDoc.Lines.CostingCode = cardBranch; // dim 1
//            }

//            var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
//            if (itemInfo == null) continue;

//            // 20220313
//            // agency code
//            if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
//            {
//                sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
//            }

//            var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGR_ReasonCode");
//            if (reasonObj != null)
//            {
//                if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
//                {
//                    sapDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
//                }
//                if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
//                {
//                    sapDoc.Lines.AccountCode = reasonObj.GLCode;
//                }
//            }

//            if ($"{itemInfo.ManBtchNum}" == "Y")
//            {
//                // get all it line 
//                var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
//                //var batLineCount = 0;
//                for (int y = 0; y < batches.Count; y++)
//                {
//                    var batch = batches[y];
//                    if (batch == null) continue;

//                    if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
//                    if (batch.OrderQty == 0) continue;

//                    sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
//                    sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
//                    sapDoc.Lines.BatchNumbers.Add();
//                }
//            }

//            sapDoc.Lines.Add();
//            // continue next item line
//            curLineCnt++;
//        }

//        int addResult = sapDoc.Add();
//        if (addResult != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//        }

//        // when add result = 0
//        int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        PostedDocEntry = newKey.ToString(); // docentry from the object 
//        PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }

//}

// create good received for distributor before invoice
//public string CreateGoodsReceive_Dist(Bread_OINV_Ext invDoc, List<Bread_INV1_Ext> lines, string remarks)
//{
//    string modName = $"[BreadDiApi_Delivery][CreateGoodsReceive_Dist]";
//    try
//    {
//        string errmsg = "";
//        connectSAP();

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();
//        Documents sapDoc = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

//        // prepare the document header
//        string cardCode = invDoc.CARDCODE;
//        sapDoc.CardCode = cardCode;

//        // doc reference 2 -> special cut off the extra lenght
//        //if (!string.IsNullOrWhiteSpace(invDoc.CUSTREF))
//        //{
//        //    sapDoc.Reference2 = invDoc.CUSTREF.Substring(0, 11);
//        //}
//        // commented on 20220313
//        // use auto remark generated from api
//        //if (!string.IsNullOrWhiteSpace(Doc.Comments))
//        //{
//        //    sapDoc.Comments = $"{Doc.Comments}";
//        //}
//        if (!string.IsNullOrWhiteSpace(invDoc.REMARKS))
//        {
//            sapDoc.JournalMemo = $"{invDoc.REMARKS}";
//        }

//        if (!string.IsNullOrWhiteSpace(remarks))
//        {
//            sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarks;
//            sapDoc.Comments = $"{remarks}"; // 20220313 use auto remark from api
//        }

//        // cross reference
//        if (!string.IsNullOrWhiteSpace(Ref2))
//        {
//            sapDoc.Reference2 = Ref2;
//        }

//        if (lines == null)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
//            return errmsg;
//        }

//        var items = lines.GroupBy(x => x.ITEMCODE).Select(y => new
//        {
//            ItemCode = y.First().ITEMCODE,
//            OrderQty = y.Sum(c => c.QUANTITY)
//        }).Distinct().ToList();

//        int docSeries = GetGrDocSeries(Db.SAPDB);
//        if (docSeries > 0)
//        {
//            sapDoc.Series = docSeries;
//        }

//        // targetWhs
//        var targetWhs = GetEntryWhsCode(Db.SAPDB);

//        // branch infor 
//        var cardBranch = GetCardBranch(Db.SAPDB, invDoc.CARDCODE); // dim1
//        var curLineCnt = 0;
//        for (int i = 0; i < items.Count; i++)
//        {
//            var itemCode = items[i];
//            if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

//            // get the sum of the line order
//            var sumOfQty = (double)lines.Where(i => i.ITEMCODE.Equals(itemCode.ItemCode)).Sum(x => x.QUANTITY);
//            if (sumOfQty == 0) continue;

//            // initial set to 
//            sapDoc.Lines.SetCurrentLine(curLineCnt);
//            sapDoc.Lines.ItemCode = itemCode.ItemCode;
//            sapDoc.Lines.Quantity = sumOfQty;
//            sapDoc.Lines.WarehouseCode = targetWhs;

//            double price = GetGrPrice(Db.SAPDB, itemCode.ItemCode);
//            if (price > 0)
//            {
//                sapDoc.Lines.Price = price;
//            }

//            if (!string.IsNullOrWhiteSpace(cardBranch))  // dim 1
//            {
//                sapDoc.Lines.CostingCode = cardBranch; // dim 1
//            }

//            var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
//            if (itemInfo == null) continue;

//            // 20220313
//            // agency code
//            if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
//            {
//                sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
//            }

//            var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGR_ReasonCode");
//            if (reasonObj != null)
//            {
//                if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
//                {
//                    sapDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
//                }
//                if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
//                {
//                    sapDoc.Lines.AccountCode = reasonObj.GLCode;
//                }
//            }

//            if ($"{itemInfo.ManBtchNum}" == "Y")
//            {
//                // get all it line 
//                var batches = DocDetails.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
//                //var batLineCount = 0;
//                for (int y = 0; y < batches.Count; y++)
//                {
//                    var batch = batches[y];
//                    if (batch == null) continue;

//                    if (string.IsNullOrWhiteSpace(batch.Batch)) continue;
//                    if (batch.OrderQty == 0) continue;

//                    sapDoc.Lines.BatchNumbers.BatchNumber = batch.Batch;
//                    sapDoc.Lines.BatchNumbers.Quantity = batch.OrderQty;
//                    sapDoc.Lines.BatchNumbers.Add();
//                }
//            }

//            sapDoc.Lines.Add();
//            // continue next item line
//            curLineCnt++;
//        }

//        int addResult = sapDoc.Add();
//        if (addResult != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//        }

//        // when add result = 0
//        int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

//        PostedDocEntry = newKey.ToString(); // docentry from the object 
//        PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
//        return "";
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        return errmsg;
//    }

//}

// GetTrayItemCode
//public string GetTrayItemCode(string sapDb)
//{
//    try
//    {
//        var sql = $@"select U_SetupValue
//                     from {sapDb}..[@APPSETUP] t0 with(nolock) 
//                     where U_SetupName ='AppTrayItemCode'";

//        return Conn.Query<string>(sql).FirstOrDefault();
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        return "";
//    }
//}


//public double GetTrayPrice(string sapDb, string itemCode)
//{
//    try
//    {
//        //var sql = $@"select t0.Price
//        //            from 
//        //            {sapDb}..ITM1 t0 with (nolock)
//        //            inner join  
//        //            {sapDb}..[@APPSETUP]  t1 with (nolock) on t1.U_SetupValue = t0.PriceList
//        //            Where U_SetupName = 'AppDefGRPriceListID'
//        //            and ItemCode = '{itemCode}'";

//        var sql = $@" select t2.Price
//                    from {sapDb}..[@APPSETUP] t0 with(nolock)
//                    inner join {sapDb}..OPLN t1 with(nolock) on t0.U_SetupName = 'AppTrayPriceListName'  
//                           and t1.ListName = t0.U_SetupValue 
//                    inner join {sapDb}..ITM1 t2 with(nolock) on t2.ItemCode = '{itemCode}'
//                           and  t2.PriceList = t1.ListNum ";

//        return Conn.Query<double>(sql).FirstOrDefault();
//    }
//    catch (Exception e)
//    {
//        errmsg = $"{e.Message}\n{e.StackTrace}";
//        return -1;
//    }
//}

#endregion
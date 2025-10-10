using Dapper;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.WhsReturn;
using SAPbobsCOM;
//using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers.Delivery
{
    public class DeliveryDiApi_RetThruInv
    {
        const string CurSapTableName = "OIGE";
        public string PostedDocEntry { get; set; }
        public string PostedDocNum { get; set; }
        string Common_DBConnStr { get; set; } = string.Empty;

        FTAPP_WRTN_INV InvDoc { get; set; }
        List<FTAPP_WRTN1_INV> InvLines { get; set; }

        public string errmsg { get; set; }
        Company oCompany { get; set; }
        DbInfo Db { get; set; }

        SqlConnection Conn { get; set; } // for module connection

        public DeliveryDiApi_RetThruInv(string commom_dbConstr, DbInfo dbinfo, FTAPP_WRTN_INV doc, List<FTAPP_WRTN1_INV> lines)
        {
            Db = dbinfo;
            InvDoc = doc;
            InvLines = lines;
            Conn = new SqlConnection(commom_dbConstr);
            Common_DBConnStr = commom_dbConstr;
        }

        public string CreateGoodIssue()
        {
            string modName = $"[DeliveryDiApi_RetThruInv][CreateGoodIssue]";
            try
            {
                errmsg = connectSAP();
                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    return errmsg; 
                }

                errmsg = "";
                // OIGE

                string webdb = Db.WEBDB;
                if (!oCompany.InTransaction)
                {
                    oCompany.StartTransaction();
                }
                
                Documents sapDoc = (Documents) oCompany.GetBusinessObject(BoObjectTypes.oInventoryGenExit);

                // prepare the document header
                //if (!string.IsNullOrWhiteSpace(InvDoc.StoreCode))
                //{
                //    sapDoc.CardCode = InvDoc.StoreCode;
                //}
                //if (!string.IsNullOrWhiteSpace(InvDoc.StoreName))
                //{
                //    sapDoc.CardName = InvDoc.StoreName;
                //}

                var remarks = $"Bs. DLB# {InvDoc.DlbEntry}, Bs. Inv#{InvDoc.InvDocNum}, " +
                             $"Bs. Return# {InvDoc.RtnEntry}, Bs. CN#{InvDoc.CnNum}";

                if (!string.IsNullOrWhiteSpace(remarks))
                {
                    sapDoc.Comments = remarks;
                    sapDoc.UserFields.Fields.Item("U_CSUS_REMARKS").Value = remarks;
                }

                if (!string.IsNullOrWhiteSpace($"{InvDoc.InvDocEntry}"))
                {
                    sapDoc.UserFields.Fields.Item("U_SOENTRY").Value = $"{InvDoc.CnEntry}";
                }

                if (InvLines == null)
                {
                    if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = $"{modName}\nThere is no detail lines found for this request. Pls contact sys admin to help.\n";
                    return errmsg;
                }

                var items = InvLines.GroupBy(x => x.ItemCode).Select(y => new
                {
                    ItemCode = y.First().ItemCode,
                    VarientQty = y.Sum(c => c.VarientQty),
                    ToWhsCode = y.First().WhsCode,
                    GIQty = y.Sum(c => c.VarientQty)
                }).Distinct().ToList();

                var cardBranch = GetCardBranch(Db.SAPDB, InvDoc.StoreCode);
                var curLineCnt = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var itemCode = items[i];
                    if (string.IsNullOrWhiteSpace(itemCode.ItemCode)) continue;

                    // get the sum of the line order
                    var sumOfQty = InvLines.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).Sum(x => x.VarientQty);
                    if (sumOfQty <= 0) continue;

                    // initial set to 
                    sapDoc.Lines.SetCurrentLine(curLineCnt);
                    sapDoc.Lines.ItemCode = itemCode.ItemCode;
                    sapDoc.Lines.Quantity = (double)sumOfQty;
                    sapDoc.Lines.WarehouseCode = itemCode.ToWhsCode;

                    if (cardBranch != "")  // dim 1
                    {
                        sapDoc.Lines.CostingCode = cardBranch; // dim 1
                    }

                    var itemInfo = GetItemInfo(Db.WEBDB, itemCode.ItemCode);
                    if (itemInfo == null) continue;

                    // 20220313
                    // agency code
                    if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
                    {
                        sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
                    }

                    // 20220313
                    // get the GR reason code and GL account code 
                    var reasonObj = GetReason_GLCodes(Db.SAPDB, "AppGI_ReasonCode");  //"AppGR_ReasonCode");
                    if (reasonObj != null)
                    {
                        if (!string.IsNullOrWhiteSpace(reasonObj.ReasonCode))
                        {
                            sapDoc.Lines.UserFields.Fields.Item("U_CSUS_RCA").Value = $"{reasonObj.ReasonCode}";
                        }
                        if (!string.IsNullOrWhiteSpace(reasonObj.GLCode))
                        {
                            sapDoc.Lines.AccountCode = reasonObj.GLCode;
                        }
                    }

                    if ($"{itemInfo.ManBtchNum}" == "Y")
                    {
                        // get all it line 
                        var batches = InvLines.Where(i => i.ItemCode.Equals(itemCode.ItemCode)).ToList();
                        //var batLineCount = 0;
                        for (int y = 0; y < batches.Count; y++)
                        {
                            var batch = batches[y];
                            if (batch == null) continue;

                            if (string.IsNullOrWhiteSpace(batch.LotNo)) continue;
                            var qty = batch.VarientQty;
                            if (qty <= 0) continue;

                            sapDoc.Lines.BatchNumbers.BatchNumber = batch.LotNo;
                            sapDoc.Lines.BatchNumbers.Quantity = (double)qty;
                            sapDoc.Lines.BatchNumbers.Add();
                        }
                    }

                    // 20220313
                    // agency for gi
                    if (!string.IsNullOrWhiteSpace(itemInfo.CardCode))
                    {
                        sapDoc.Lines.CostingCode2 = $"{itemInfo.CardCode}";
                    }

                    sapDoc.Lines.Add();

                    // continue next item line
                    curLineCnt++;
                }

                int addResult = sapDoc.Add();
                if (addResult != 0)
                {
                    if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return $"{modName}\n{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                }

                // when add result = 0
                int newKey = Convert.ToInt32(oCompany.GetNewObjectKey());

                System.Runtime.InteropServices.Marshal.ReleaseComObject(sapDoc);
                if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                PostedDocEntry = newKey.ToString(); // docentry from the object 
                PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);

                // update the inv ret header 
                // and line details update 
                using var conn1 = new SqlConnection(Common_DBConnStr);
                if (conn1.State == System.Data.ConnectionState.Closed) conn1.Open();
                using( var trans = conn1.BeginTransaction())
                {
                    try
                    {
                        var update_sql = @$"Update {Db.WEBDB}..FTAPP_WRTN_INV 
                                        Set GIDocEntry = @GIDocEntry, 
                                            GIDocNum= @GIDocNum 
                                        Where id = @id";

                        conn1.Execute(update_sql, new
                        {
                            GIDocEntry = PostedDocEntry,
                            GIDocNum = PostedDocNum,
                            id = InvDoc.id
                        }, trans);

                        // update the inv ret header 

                        for (int r = 0; r < InvLines.Count; r++)
                        {
                            var line = InvLines[r];
                            if (line == null) continue;

                            var update_Inv1_sql = @$"Update {Db.WEBDB}..FTAPP_WRTN1_INV 
                                            Set GIDocEntry = @GIDocEntry, 
                                                GILineNum = @InvLine , 
                                                GIQty = @VarientQty
                                            Where id = @id";

                            conn1.Execute(update_Inv1_sql, new
                            {
                                GIDocEntry = PostedDocEntry,
                                InvLine = line.InvLine,
                                VarientQty = line.VarientQty,
                                id = line.Id
                            }, trans);
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        errmsg = $"INV line update: {e.Message}\n{e.StackTrace}";
                        return errmsg;
                    }
                }

                return "";
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
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

        OITM_Ext GetItemInfo(string webDb, string itemCode)
        {
            try
            {
                var sql = @"exec sp_GetItemSetup @webDb, @itemCode";
                return Conn.Query<OITM_Ext>(sql, new
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

        string GetCardBranch(string sapDb, string cardCode)
        {
            try
            {
                var sql = $@"SELECT t2.PrcCode [BranchCode]
                        FROM {sapDb}..OCRD t0 with (nolock) 
                            LEFT JOIN {sapDb}..OTER t1 with (nolock) on t0.Territory = t1.territryID
							LEFT JOIN {sapDb}..OPRC t2 with (nolock) on t0.U_COSTCTR = t2.PrcCode
                        WHERE CardCode= @cardCode";

                var result = Conn.Query<string>(sql, new
                {
                    cardCode = cardCode
                }).FirstOrDefault();

                if (result == null) // query the ocrd u_cost center
                {
                    sql = @$"select U_COSTCTR 
                            from {sapDb}..OCRD with (nolock)
                            where cardcode = @CardCode";

                    result = Conn.Query<string>(sql, new { CardCode = cardCode }).FirstOrDefault();
                }

                return result;
            }
            catch (Exception e)
            {
                errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
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

                return Conn.Query<GR_GI_Reacon_GLCode>(sql, new { setupname }).FirstOrDefault();
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

        void UpdateGIEntry(string giDocEntry, string cnDocEntry)
        {
            try
            {
                var updateSql = @$"Update {Db.WEBDB}..FTAPP_RCN Set GIENTRY = @giDocEntry Where docEntry = @cnDocEntry";
                Conn.Execute(updateSql, new
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
    }
}

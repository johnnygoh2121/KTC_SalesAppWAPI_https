using Dapper;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.SalesOrder;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers.DiApi
{
    public class BreadDiApi_Delivery_Dist_Transport2Inv
    {
        public string Errmsg { get; set; }
        public DbInfo Db { get; set; }

        public string SvrPath { get; set; }
        public string Docentry { get; set; }
        public string CurSapTableName { get; set; }

        //string TrRef2 { get; set; }
        //string TrRemarks { get; set; }

        string InvRemarks { get; set; }
        string InvRef2 { get; set; }

        Company oCompany { get; set; }
        
        //List<BreadDocDetail> DocDetails { get; set; } = null;
        SqlConnection Conn { get; set; } // for module connection        

        public string PostedDocEntry { get; set; }
        public string PostedDocNum { get; set; }

        string TransporterWhsCode { get; set; }

        // constructor 
        public BreadDiApi_Delivery_Dist_Transport2Inv(string crCommDbConnStr)
        {
            Conn = new SqlConnection(crCommDbConnStr);
        }

        // entry point 
        public string CreateInvoice_Dist(Bread_OINV_Ext invDoc,
                                                    List<Bread_INV1_Ext> lines, string transporterWhsCode)
        {
            TransporterWhsCode = transporterWhsCode;
            string modName = $"[BreadDiApi_Delivery_Dist_Transport2Inv][CreateInvoice_Dist]";
            try
            {
                // query the inv 
                // create the invoice in sap 
                // ---------------------------------------------------------------------------------
                #region Query the inv from portal 
                Errmsg = "";
                // parepare the invoice data table 
                var query = @$"SELECT T0.*
                            , T1.CARDCODE AS [SAPCARDCODE]
                            , T2.INVARINVSERIES AS [INVSERIES]
                            , T2.INVARCNSERIES AS [CNSERIES]
                            , CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE 
                                   ELSE U0.DEFWHS END AS [WHSCODE]
                            , T3.SERIESNAME
                            , U0.SLPCODE AS [SAPSLPCODE]
                            , T4.PRCCODE AS [DIM1] 
                            FROM {Db.WEBDB}..INV T0 
                                        INNER JOIN 
                                [{Db.SAPDB}].[DBO].[OCRD] T1 ON 
                                        --CASE WHEN ISNULL(T1.U_PORTALID,'') = '' THEN T1.CardCode 
                                        --ELSE T1.U_PORTALID END = T0.CARDCODE 
                                        T1.CARDCODE  = T0.CARDCODE    
                                        AND T1.CARDTYPE = 'C' 
                                        AND T1.FrozenFor = 'N' 
                            LEFT OUTER JOIN {Db.WEBDB}..SAPREC T2 ON T2.RECID = 1 
                            LEFT OUTER JOIN {Db.WEBDB}..USERS U0 ON U0.USERID = T0.UMODIFIED 
                            LEFT OUTER JOIN {Db.SAPDB}..[NNM1] T3 ON T3.SERIES = T2.INVARINVSERIES 
                            LEFT OUTER JOIN {Db.SAPDB}..[OPRC] T4 ON T4.PRCCODE = T1.U_COSTCTR AND T4.DimCode = '1' 
                            WHERE T0.DOCENTRY = '{Docentry}' ";

                var dt = GetDataTable(Conn, query);
                if (dt == null)
                {
                    return Errmsg;
                }

                var sapcard = dt.Rows[0]["SAPCARDCODE"].ToString();
                var priceList = Db.DEF_PRICELIST;

                query = $@"SELECT T1.*
                            , T1.PRICE AS[CUSTPRICE]
                            , T5.Price AS[SUPPLIERPRICE]
                            , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
                                   ELSE T6.U_CSUS_UOM END AS [CSUOM]
                            , ISNULL(T7.GLCN,'') AS [CNGL]
                            , T8.PRCCODE AS [DIM2] 
                            , T6.ManBtchNum [MANBATCHNUM]
                            FROM {Db.WEBDB}..INV T0 
                            INNER JOIN {Db.WEBDB}..INV1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
                            INNER JOIN [{Db.SAPDB}].[DBO].[OCRD] T2 ON T2.CARDCODE = T0.CARDCODE 
                                    AND T2.CARDTYPE = 'C' 
                                    AND T2.FrozenFor = 'N' AND T2.CardCode = '{sapcard}' 
                            LEFT OUTER JOIN [{Db.SAPDB}].[DBO].[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
                            LEFT OUTER JOIN [{Db.SAPDB}].[DBO].[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
                                    AND T4.PriceList = T2.ListNum 
                            LEFT OUTER JOIN [{Db.SAPDB}].[DBO].[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
                                    AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum 
                                                            ELSE '{priceList}' END
                            LEFT OUTER JOIN [{Db.SAPDB}].[DBO].[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
                            LEFT OUTER JOIN {Db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
                            LEFT OUTER JOIN [{Db.SAPDB}].[DBO].[OPRC] T8 ON T8.PRCCODE = T6.CardCode AND T8.DimCode = '2' 
                            WHERE T0.DOCENTRY = '{Docentry}'";

                var dt1 = GetDataTable(Conn, query);
                if (dt1 == null)
                {
                    return "Query inv header table null";
                }

                // connect SAP 
                string errmsg = "";
                connectSAP();

                errmsg = "";

                // start the sap doc posting transaction
                string webdb = Db.WEBDB;
                //oCompany.StartTransaction();


                // prepare create of the invoice 
                InvRef2 = "";
                InvRemarks = $"TRANSPORTER TO DIST INV {DateTime.Now:dd-MMM-yyyy}";
                #endregion
                // ---------------------------------------------------------------------------------

                #region create the invoice 
                SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                rs.DoQuery("SELECT * FROM [" + webdb + "].[DBO].[INV] WHERE DOCENTRY = " + Docentry + " AND DOCSTATUS IN ('D','V') ");
                if (rs.RecordCount <= 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    rs = null;
                    return "";
                }

                DataRow dr = dt.Rows[0];
                string seriesname = dt.Rows[0]["SERIESNAME"].ToString();
                double rounding = 0;
                int linenum = 0;
                double qty = 0, price = 0, uom = 0, csqty = 0, pcsqty = 0;
                string cnNo = dr["CUSTREF"].ToString();
                if (!double.TryParse(dr["ROUNDING"].ToString(), out rounding)) rounding = 0;

                // invoice 
                SAPbobsCOM.Documents oInv = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
                SAPbobsCOM.Documents oInv1 = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);

                var dim1 = dr["DIM1"].ToString();
                oInv.Series = int.Parse(dr["INVSERIES"].ToString());
                oInv.CardCode = dr["SAPCARDCODE"].ToString();
                oInv.DocDate = DateTime.Today;
                oInv.Comments = dr["REMARKS"].ToString();
                oInv.NumAtCard = cnNo;//dr["CUSTREF"].ToString();
                oInv.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
                oInv.UserFields.Fields.Item("U_SOENTRY").Value = int.Parse(dr["DOCENTRY"].ToString());

                if (dr["SAPSLPCODE"].ToString() != "")
                {
                    oInv.SalesPersonCode = int.Parse(dr["SAPSLPCODE"].ToString());
                }
                if (rounding != 0)
                {
                    oInv.Rounding = SAPbobsCOM.BoYesNoEnum.tYES;
                    oInv.RoundingDiffAmount = rounding;
                }

                // add in the remarks
                // 2022 03 14
                if (!string.IsNullOrWhiteSpace(InvRemarks))
                {
                    oInv.UserFields.Fields.Item("U_CSUS_REMARKS").Value = InvRemarks;
                    oInv.Comments = $"{InvRemarks}"; // 20220313 use auto remark from api
                }
                if (!string.IsNullOrWhiteSpace(InvRef2))
                {
                    oInv.Reference2 = InvRef2;
                }

                linenum = 0;
                foreach (DataRow row in dt1.Rows)
                {
                    linenum++;
                    if (linenum > 1) oInv.Lines.Add();
                    oInv.Lines.SetCurrentLine(linenum - 1);

                    oInv.Lines.ItemCode = row["ITEMCODE"].ToString();
                    if (!double.TryParse(row["QUANTITY"].ToString(), out qty)) qty = 0;
                    if (!double.TryParse(row["CUSTPRICE"].ToString(), out price)) price = 0;
                    if (!double.TryParse(row["CSUOM"].ToString(), out uom)) uom = 0;
                    var dim2 = row["DIM2"].ToString();
                    csqty = 0;
                    pcsqty = 0;
                    if (uom != 0)
                    {
                        if (uom != 1)
                        {
                            csqty = Math.Floor(qty / uom);
                        }
                    }
                    pcsqty = qty - (csqty * uom);

                    oInv.Lines.Quantity = qty;
                    oInv.Lines.UnitPrice = price;

                    // choose the user / transporter warehouse code to create the invoice
                    var whsValue = !string.IsNullOrWhiteSpace(TransporterWhsCode) ?
                        TransporterWhsCode : dr["WHSCODE"].ToString();

                    if (!string.IsNullOrWhiteSpace(whsValue))
                    {
                        oInv.Lines.WarehouseCode = whsValue;  //dr["WHSCODE"].ToString();
                    }

                    oInv.Lines.UserFields.Fields.Item("U_CTN").Value = csqty;
                    oInv.Lines.UserFields.Fields.Item("U_UNT").Value = pcsqty;

                    if (dim1 != "")
                    {
                        oInv.Lines.CostingCode = dim1;
                    }
                    if (dim2 != "")
                    {
                        oInv.Lines.CostingCode2 = dim2;
                    }
                    //oOrder.Lines.VatGroup = row["TAXCODE"].ToString();

                    // 20220217
                    // handle batch is item is batch manage
                    var ManByBatch = row["MANBATCHNUM"].ToString();
                    var lineNum = row["LINENUM"].ToString();
                    if (ManByBatch.Equals("Y"))
                    {
                        // query the batch from INV3 
                        // loop each line in Diapi
                        var batches = GetBatches(Docentry, lineNum, "INV3");
                        var batchLineCnt = 0;
                        for (int bid = 0; bid < batches.Count; bid++)
                        {
                            var batch = batches[bid];
                            if (batch == null) continue;

                            if (batchLineCnt > 0) oInv.Lines.BatchNumbers.Add();
                            oInv.Lines.BatchNumbers.BatchNumber = batch.BatchNo;
                            oInv.Lines.BatchNumbers.Quantity = (double)batch.Quantity;
                            batchLineCnt++;
                        }
                    }
                }

                string invEntry = "0";
                string cnEntry = "0";

                if (oInv.Add() != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    Errmsg = oCompany.GetLastErrorDescription();
                    return errmsg;
                }
                else
                {
                    oCompany.GetNewObjectCode(out invEntry);
                    if (oInv1.GetByKey(int.Parse(invEntry)))
                    {
                        rs.DoQuery("UPDATE [" + webdb + "].[DBO].[INV] SET DOCSTATUS = 'C', " +
                            "INVENTRY = " + invEntry + ",CNENTRY = " + cnEntry + "," +
                            " DOCNUM = '" + seriesname + oInv1.DocNum.ToString() + "', SAPINV = 'Y' " +
                            "WHERE DOCENTRY = " + Docentry);

                        PostedDocNum = $"{oInv1.DocNum}";
                        PostedDocEntry = $"{oInv1.DocEntry}";
                    }
                    else
                    {
                        oInv1 = null;
                        //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        Errmsg = oCompany.GetLastErrorDescription();
                        return errmsg;
                    }
                }

                // add file 
                // 2022-03-04
                var files = dr["FILES"].ToString();
                if (!string.IsNullOrWhiteSpace(files))
                {
                    var filesArry = files.Split(",");
                    for (int f = 0; f < filesArry.Length; f++)
                    {
                        var filePath = Path.Combine(SvrPath, filesArry[f]);
                        Errmsg += "\n" + Add_FileAttachment(int.Parse(invEntry), filePath, BoObjectTypes.oInvoices);
                    }
                }

                if (!string.IsNullOrWhiteSpace(Errmsg))
                {
                    return Errmsg;
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);

                oInv = null;
                oInv1 = null;
                #endregion

                if (!string.IsNullOrWhiteSpace(errmsg))
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return errmsg;
                }

                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                PostedDocEntry = invEntry.ToString(); // docentry from the object 
                PostedDocNum = GetDocNumberbyDoEntry(PostedDocEntry, CurSapTableName);
                return "";
            }
            catch (Exception e)
            {
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return Errmsg;
            }
        }


        public string Add_FileAttachment(int docEntry, string svyPhyPath, BoObjectTypes objectTypes)
        {
            try
            {
                Errmsg = "";
                string webdb = Db.WEBDB;

                Documents doc = (Documents)oCompany.GetBusinessObject(objectTypes);
                doc.GetByKey(docEntry);

                var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                int fileCnt = 0;

                if (File.Exists(svyPhyPath))
                {
                    attachedments.Lines.Add();
                    attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                    attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                    attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                    attachedments.Lines.Override = BoYesNoEnum.tYES;
                    fileCnt++;
                }

                if (attachedments.Add() == 0)
                {
                    int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                    doc.AttachmentEntry = iAttEntry;
                    doc.Update();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                    return "";
                }
                else
                {
                   // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    Errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                    return Errmsg;
                }
            }
            catch (Exception e)
            {
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return Errmsg;
            }
        }

        List<Bread_Batch> GetBatches(string docEntry, string linenum, string tableName)
        {
            try
            {
                var conn = new SqlConnection(Db.GetWebDbConnStr());
                var sql = $@"Select * 
                             from {Db.WEBDB}..{tableName} 
                             where DocEntry = @docEntry and LineNum = @lineNum";

                return conn.Query<Bread_Batch>(sql, new
                {
                    docEntry = docEntry,
                    lineNum = linenum
                }).ToList();
            }
            catch (Exception e)
            {
                Errmsg = $"{e.Message}{e.StackTrace}";
                return null;
            }
        }

        DataTable GetDataTable(SqlConnection conn, string query)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandTimeout = 0;

                SqlDataAdapter da = new SqlDataAdapter(cmd); // create data adapter                
                DataTable dt = new DataTable();
                da.Fill(dt); // this will query your database and return the result to your datatabled
                da.Dispose();
                return dt;
            }
            catch (Exception e)
            {
                Errmsg = $"{e.Message}\n{e.StackTrace}";
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
            }
            catch (Exception ex)
            {
                errmsg = ex.Message;
            }

            return errmsg;
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return "-1";
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return "";
            }
        }

        public double GetGrPrice(string sapDb, string itemCode)
        {
            try
            {
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return -1;
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
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }

        OITM_Ext GetItemInfo(string webDb, string itemCode)
        {
            try
            {
                var sql = @"sp_GetItemSetup @webDb, @itemCode";
                return Conn.Query<OITM_Ext>(sql, new
                {
                    webDb = webDb,
                    itemCode = itemCode
                }).FirstOrDefault();
            }
            catch (Exception e)
            {
                Errmsg = $"{e.Message}\n{e.StackTrace}";
                return null;
            }
        }
    }
}

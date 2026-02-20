using Dapper;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers.DiApi
{
    public class BreadDiApi_Trade
    {
        public string LastErrorMesage { get; set; }
        string DocRemarks { get; set; }
        string DocRef2 { get; set; }
        string SvrPath { get; set; }

        Company oCompany { get; set; }
        DbInfo Db { get; set; }

        public BreadDiApi_Trade(DbInfo db, string svrPath)
        {
            Db = db;
            SvrPath = svrPath;
        }

        //public BreadDiApi_Trade(DbInfo db, string svrPath, string remarks, string ref2)
        //{
        //    Db = db;
        //    SvrPath = svrPath;
        //    DocRemarks = remarks;
        //    DocRef2 = ref2;
        //}

        public string ConnectSAP()
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

        /// <summary>
        /// create invoice then Cn 
        /// use by seller soled to ktc store
        /// </summary>
        /// <param name="docentry"></param>
        /// <param name="dt">invoice table, INV </param>
        /// <param name="dt1">invoice lines tablem INV1</param>
        /// <param name="nocn">true for create CN </param>
        /// <returns></returns>
        public string CreateInvCN(string docentry, DataTable dt, DataTable dt1, bool nocn)
        {
            string errmsg = ConnectSAP();
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                return errmsg;
            }

            var docBatches_CN = GetBatches_AllLine(docentry, "CN3");
            var docBatches_INV = GetBatches_AllLine(docentry, "INV3");

            string cnEntry = "0", invEntry = "";
            string dim1 = "", dim2 = "";
            try
            {
                string webdb = Db.WEBDB; // AppUtilities.getWebDB();

                //oCompany.StartTransaction();

                SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                rs.DoQuery("SELECT * FROM [" + webdb + "].[DBO].[INV] WHERE DOCENTRY = " + docentry + " AND DOCSTATUS IN ('D','V') ");
                if (rs.RecordCount <= 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    rs = null;
                    return "";
                }

                DataRow headerFirstRow = dt.Rows[0];
                string seriesname = dt.Rows[0]["SERIESNAME"].ToString();
                double rounding = 0;
                int linenum = 0;
                double qty = 0, price = 0, uom = 0, csqty = 0, pcsqty = 0;
                string cnNo = headerFirstRow["CUSTREF"].ToString();
                if (!double.TryParse(headerFirstRow["ROUNDING"].ToString(), out rounding)) rounding = 0;

                // credit note 
                #region create credit note
                if (!nocn)
                {
                    SAPbobsCOM.Documents oCN = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);
                    SAPbobsCOM.Documents oCN1 = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);

                    dim1 = headerFirstRow["DIM1"].ToString();

                    oCN.Series = int.Parse(headerFirstRow["CNSERIES"].ToString());
                    oCN.CardCode = headerFirstRow["COMPANYID"].ToString();
                    oCN.DocDate = DateTime.Today;
                    oCN.Comments = headerFirstRow["REMARKS"].ToString();
                    oCN.NumAtCard = headerFirstRow["CARDNAME"].ToString();
                    oCN.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
                    oCN.UserFields.Fields.Item("U_SOENTRY").Value = int.Parse(headerFirstRow["DOCENTRY"].ToString());

                    if (rounding != 0)
                    {
                        oCN.Rounding = SAPbobsCOM.BoYesNoEnum.tYES;
                        oCN.RoundingDiffAmount = rounding;
                    }

                    // add in the remarks
                    // 2022 03 14
                    if (!string.IsNullOrWhiteSpace(DocRemarks))
                    {
                        oCN.UserFields.Fields.Item("U_CSUS_REMARKS").Value = DocRemarks;
                        oCN.Comments = $"{DocRemarks}"; // 20220313 use auto remark from api
                    }
                    if (!string.IsNullOrWhiteSpace(DocRef2))
                    {
                        oCN.Reference2 = DocRef2;
                    }

                    linenum = 0;
                    foreach (DataRow row in dt1.Rows)
                    {
                        linenum++;
                        if (linenum > 1) oCN.Lines.Add();
                        oCN.Lines.SetCurrentLine(linenum - 1);
                        oCN.Lines.ItemCode = row["ITEMCODE"].ToString();
                        if (!double.TryParse(row["QUANTITY"].ToString(), out qty)) qty = 0;
                        if (!double.TryParse(row["SUPPLIERPRICE"].ToString(), out price)) price = 0;
                        if (!double.TryParse(row["CSUOM"].ToString(), out uom)) uom = 0;
                        dim2 = row["DIM2"].ToString();

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

                        oCN.Lines.Quantity = qty;
                        oCN.Lines.UnitPrice = price;
                        oCN.Lines.WarehouseCode = headerFirstRow["WHSCODE"].ToString();
                        oCN.Lines.UserFields.Fields.Item("U_CTN").Value = csqty;
                        oCN.Lines.UserFields.Fields.Item("U_UNT").Value = pcsqty;

                        if (dim1 != "")
                        {
                            oCN.Lines.CostingCode = dim1;
                        }
                        if (dim2 != "")
                        {
                            oCN.Lines.CostingCode2 = dim2;
                        }
                        if (row["CNGL"].ToString() != "")
                        {
                            oCN.Lines.AccountCode = row["CNGL"].ToString();
                        }

                        // 20220217
                        // handle batch is item is batch manage
                        var ManByBatch = row["MANBATCHNUM"].ToString();
                        var lineNum = row["LINENUM"].ToString();
                        if (ManByBatch.Equals("Y"))
                        {
                            // query the batch from INV3 
                            // loop each line in Diapi
                            var batches = docBatches_INV.Where(r => r.LineNum == linenum && 
                                                                    r.TableName == "INV3"
                                                               ).ToList();    ///doc(docentry, lineNum, "INV3");


                            var batchLineCnt = 0;
                            for (int bid = 0; bid < batches.Count; bid++)
                            {
                                var batch = batches[bid];
                                if (batch == null) continue;

                                if (batchLineCnt > 0) oCN.Lines.BatchNumbers.Add();
                                oCN.Lines.BatchNumbers.BatchNumber = batch.BatchNo;
                                oCN.Lines.BatchNumbers.Quantity = (double)batch.Quantity;
                                batchLineCnt++;
                            }
                        }
                    }

                    if (oCN.Add() != 0)
                    {
                        //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        errmsg = oCompany.GetLastErrorDescription();
                        throw new Exception(errmsg);
                    }
                    else
                    {
                        oCompany.GetNewObjectCode(out cnEntry);
                    }

                    if (oCN1.GetByKey(int.Parse(cnEntry)))
                    {
                        cnNo = oCN1.DocNum.ToString();
                    }
                    else
                    {
                        oCN1 = null;
                        throw new Exception("Unable to retrieve posted document!");
                    }

                    // attached file if any 
                    // add file 
                    // 2022-03-04
                    var files0 = headerFirstRow["FILES"].ToString();
                    if (!string.IsNullOrWhiteSpace(files0))
                    {
                        var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                        var filesArry = files0.Split(",");
                        int fileCnt = 0;
                        for (int f = 0; f < filesArry.Length; f++)
                        {
                            var svyPhyPath = Path.Combine(SvrPath, filesArry[f]);
                            if (File.Exists(svyPhyPath))
                            {
                                // new add file                                                             
                                attachedments.Lines.Add();
                                attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                                attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                                attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                                attachedments.Lines.Override = BoYesNoEnum.tYES;
                                fileCnt++;
                            }
                        }

                        if (fileCnt > filesArry.Length)
                        {
                            if (attachedments.Add() == 0)
                            {
                                int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                                oCN1.AttachmentEntry = iAttEntry;
                                oCN1.Update();
                            }
                            else
                            {
                                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                                errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                                return errmsg;
                            }
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oCN);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oCN1);
                    oCN = null;
                    if (oCN1 != null) oCN1 = null;
                }

                #endregion create credit note

                // invoice 
                #region create invoice
                SAPbobsCOM.Documents oInv = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
                SAPbobsCOM.Documents oInv1 = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
                dim1 = headerFirstRow["DIM1"].ToString();
                oInv.Series = int.Parse(headerFirstRow["INVSERIES"].ToString());
                oInv.CardCode = headerFirstRow["SAPCARDCODE"].ToString();
                oInv.DocDate = DateTime.Today;
                oInv.Comments = headerFirstRow["REMARKS"].ToString();
                oInv.NumAtCard = cnNo;//dr["CUSTREF"].ToString();
                oInv.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
                oInv.UserFields.Fields.Item("U_SOENTRY").Value = int.Parse(headerFirstRow["DOCENTRY"].ToString());

                if (headerFirstRow["SAPSLPCODE"].ToString() != "")
                {
                    oInv.SalesPersonCode = int.Parse(headerFirstRow["SAPSLPCODE"].ToString());
                }
                if (rounding != 0)
                {
                    oInv.Rounding = SAPbobsCOM.BoYesNoEnum.tYES;
                    oInv.RoundingDiffAmount = rounding;
                }

                // add in the remarks
                // 2022 03 14
                if (!string.IsNullOrWhiteSpace(DocRemarks))
                {
                    oInv.UserFields.Fields.Item("U_CSUS_REMARKS").Value = DocRemarks;
                    oInv.Comments = $"{DocRemarks}"; // 20220313 use auto remark from api
                }
                if (!string.IsNullOrWhiteSpace(DocRef2))
                {
                    oInv.Reference2 = DocRef2;
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
                    dim2 = row["DIM2"].ToString();
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
                    oInv.Lines.WarehouseCode = headerFirstRow["WHSCODE"].ToString();
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
                        var batches = docBatches_INV.Where(r => r.TableName == "INV3" && r.LineNum == linenum).ToList(); //GetBatches(docentry, lineNum, "INV3");
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

                if (oInv.Add() != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = oCompany.GetLastErrorDescription();
                    throw new Exception(errmsg);
                }
                else
                {
                    oCompany.GetNewObjectCode(out invEntry);
                    if (oInv1.GetByKey(int.Parse(invEntry)))
                    {
                        //rs.DoQuery(
                        //    "UPDATE [" + webdb + "].[DBO].[INV] SET DOCSTATUS = 'C', INVENTRY = " + invEntry + ",CNENTRY = " + cnEntry + ", DOCNUM = '" + seriesname + oInv1.DocNum.ToString() + "', SAPINV = 'Y' WHERE DOCENTRY = " + docentry);
                    }
                    else
                    {
                        oInv1 = null;
                        throw new Exception("Unable to retrieve posted document!");
                    }
                }

                // attach file 
                // attache file if any 
                // add file 
                // 2022-03-04
                var files = headerFirstRow["FILES"].ToString();
                if (!string.IsNullOrWhiteSpace(files))
                {
                    var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                    var filesArry = files.Split(",");
                    int fileCnt = 0;
                    for (int f = 0; f < filesArry.Length; f++)
                    {
                        var svyPhyPath = Path.Combine(SvrPath, filesArry[f]);
                        if (File.Exists(svyPhyPath))
                        {
                            // new add file                                                             
                            attachedments.Lines.Add();
                            attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                            attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                            attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                            attachedments.Lines.Override = BoYesNoEnum.tYES;
                            fileCnt++;
                        }
                    }

                    if (fileCnt > filesArry.Length)
                    {
                        if (attachedments.Add() == 0)
                        {
                            int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                            oInv1.AttachmentEntry = iAttEntry;
                            oInv1.Update();
                        }
                        else
                        {
                           // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                            errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                            return errmsg;
                        }
                    }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                }

                #endregion create invoice

                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);

                oInv = null;
                oInv1 = null;                                  
                return errmsg;
            }
            catch (Exception ex)
            {
                errmsg = ex.Message;
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
        }

        /// <summary>
        /// create cn then invoice
        /// use by dist sold to ktc store
        /// </summary>
        /// <param name="docentry">doc entry</param>
        /// <param name="dt"></param>
        /// <param name="dt1"></param>
        /// <param name="nocn"></param>
        /// <returns></returns>
        public string createCNInv(string docentry, DataTable dt, DataTable dt1, 
            bool noinv, bool followLineWhs = false)
        {
            var docBatches_CN = GetBatches_AllLine(docentry, "CN3");
            var docBatches_INV = GetBatches_AllLine(docentry, "INV3");

            string errmsg = "";
            ConnectSAP();

            string cnEntry = "", invEntry = "";
            string dim1 = "", dim2 = "";
            try
            {
                #region create of credit note
                string webdb = Db.WEBDB; //AppUtilities.getWebDB();
                //oCompany.StartTransaction();
                SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                rs.DoQuery("SELECT * FROM [" + webdb + "].[DBO].[CN] WHERE DOCENTRY = " + docentry + " AND DOCSTATUS = 'D' ");
                if (rs.RecordCount <= 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    rs = null;
                    return "Error read data from RS";
                }

                DataRow dr = dt.Rows[0];
                string seriesname = dr["SERIESNAME"].ToString();

                SAPbobsCOM.Documents oCN = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);
                SAPbobsCOM.Documents oCN1 = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);

                Double rounding = 0;
                if (double.TryParse(dr["ROUNDING"].ToString(), out rounding)) rounding = 0;
                dim1 = dr["DIM1"].ToString();
                string reason = dr["REASON"].ToString();
                string remarks = dr["REMARKS"].ToString();
                string rem = remarks.Trim();
                if (rem.Length > 0) rem += System.Environment.NewLine;
                rem += reason;
                oCN.Series = int.Parse(dr["CNSERIES"].ToString());
                oCN.CardCode = dr["SAPCARDCODE"].ToString();
                oCN.DocDate = DateTime.Today;
                oCN.Comments = dr["REMARKS"].ToString();
                oCN.NumAtCard = dr["CUSTREF"].ToString();
                oCN.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
                oCN.UserFields.Fields.Item("U_CSUS_COG").Value = dr["DOCENTRY"].ToString();
                oCN.UserFields.Fields.Item("U_CSUS_REMARKS").Value = rem;

                oCN.UserFields.Fields.Item("U_SOENTRY").Value = int.Parse(dr["DOCENTRY"].ToString());

                string slpCode = dr["SLPCODE"].ToString();
                if (!string.IsNullOrWhiteSpace(slpCode))
                {
                    oCN.SalesPersonCode = int.Parse(slpCode);
                }

                if (rounding != 0)
                {
                    oCN.Rounding = SAPbobsCOM.BoYesNoEnum.tYES;
                    oCN.RoundingDiffAmount = rounding;
                }

                int linenum = 0;
                double qty = 0, price = 0, uom = 0, csqty = 0, pcsqty = 0;
                foreach (DataRow row in dt1.Rows)
                {
                    linenum++;
                    if (linenum > 1) oCN.Lines.Add();
                    oCN.Lines.SetCurrentLine(linenum - 1);
                    oCN.Lines.ItemCode = row["ITEMCODE"].ToString();
                    if (!double.TryParse(row["QUANTITY"].ToString(), out qty)) qty = 0;
                    if (!double.TryParse(row["CUSTPRICE"].ToString(), out price)) price = 0;
                    if (!double.TryParse(row["CSUOM"].ToString(), out uom)) uom = 0;
                    string taxcode = row["TAXCODE"].ToString();
                    dim2 = row["DIM2"].ToString();
                    csqty = 0;
                    pcsqty = 0;

                    if (uom != 0 && uom != 1)
                    {
                        csqty = Math.Floor(qty / uom);
                    }

                    pcsqty = qty - (csqty * uom);
                    oCN.Lines.Quantity = qty;
                    oCN.Lines.UnitPrice = price;

                    var whsCode = followLineWhs == true ? row["WHSCODE"].ToString() : dr["WHSCODE"].ToString();
                    oCN.Lines.WarehouseCode = whsCode;

                    var reasoncode = $"{row["U_CSUS_RC"]}";
                    if (!string.IsNullOrWhiteSpace(reasoncode))
                    {
                        oCN.Lines.UserFields.Fields.Item("U_CSUS_RC").Value = reasoncode;
                    }

                    oCN.Lines.UserFields.Fields.Item("U_CTN").Value = csqty;
                    oCN.Lines.UserFields.Fields.Item("U_UNT").Value = pcsqty;
                    if (taxcode.EndsWith("6"))
                    {
                        oCN.Lines.VatGroup = taxcode;
                    }
                    if (dim1 != "")
                    {
                        oCN.Lines.CostingCode = dim1;
                    }
                    if (dim2 != "")
                    {
                        oCN.Lines.CostingCode2 = dim2;
                    }

                    // BATCH MAN -- CN
                    // handle batch is item is batch manage
                    var ManByBatch = row["MANBTCHNUM"].ToString();
                    var lineNum = row["LINENUM"].ToString();

                    if (ManByBatch.Equals("Y"))
                    {
                        // query the batch from INV3 
                        // loop each line in Diapi
                        var batches = docBatches_CN.Where(r => r.LineNum == linenum && r.TableName == "CN3").ToList();  // GetBatches(docentry, lineNum, "CN3");
                        var batchLineCnt = 0;
                        for (int bid = 0; bid < batches.Count; bid++)
                        {
                            var batch = batches[bid];
                            if (batch == null) continue;

                            if (batchLineCnt > 0) oCN.Lines.BatchNumbers.Add();
                            oCN.Lines.BatchNumbers.BatchNumber = batch.BatchNo;
                            oCN.Lines.BatchNumbers.Quantity = (double)batch.Quantity;
                            batchLineCnt++;
                        }
                    }
                }

                if (oCN.Add() != 0)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = oCompany.GetLastErrorDescription();
                    return errmsg;
                }

                oCompany.GetNewObjectCode(out cnEntry);
                var isloadCn = oCN1.GetByKey(int.Parse(cnEntry));
                if (!isloadCn)
                {
                    //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    errmsg = oCompany.GetLastErrorDescription();
                    return errmsg;                 
                }
                else
                {
                    // perform save into the bread cn 
                    //rs.DoQuery("UPDATE [" + webdb + "].[DBO].[CN] SET DOCSTATUS = 'C', CNENTRY = " + cnEntry + ", DOCNUM = '" + seriesname + oCN1.DocNum.ToString() + "', SAPINV = 'Y' WHERE DOCENTRY = " + docentry);                    
                }


                // add credit not files 
                // attach file 
                // attache file if any 
                // add file 
                // 2022-03-04
                var files = dr["FILES"].ToString();
                if (!string.IsNullOrWhiteSpace(files))
                {
                    var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                    var filesArry = files.Split(",");
                    int fileCnt = 0;
                    for (int f = 0; f < filesArry.Length; f++)
                    {
                        var svyPhyPath = Path.Combine(SvrPath, filesArry[f]);
                        if (File.Exists(svyPhyPath))
                        {
                            // new add file                                                             
                            attachedments.Lines.Add();
                            attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                            attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                            attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                            attachedments.Lines.Override = BoYesNoEnum.tYES;
                            fileCnt++;
                        }
                    }

                    if (fileCnt > filesArry.Length)
                    {
                        if (attachedments.Add() == 0)
                        {
                            int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                            oCN1.AttachmentEntry = iAttEntry;
                            oCN1.Update();
                        }
                        else
                        {
                            //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                            errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                            return errmsg;
                        }
                    }
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                }
                #endregion create of credit note

                #region create of invoice 
                if (!noinv)
                {
                    SAPbobsCOM.Documents oInv = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
                    SAPbobsCOM.Documents oInv1 = (SAPbobsCOM.Documents)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);

                    oInv.Series = int.Parse(dr["INVSERIES"].ToString());
                    oInv.CardCode = dr["COMPANYID"].ToString();
                    oInv.DocDate = DateTime.Today;
                    oInv.Comments = dr["REMARKS"].ToString();
                    oInv.NumAtCard = dr["CARDNAME"].ToString();
                    oInv.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
                    oInv.UserFields.Fields.Item("U_CSUS_COG").Value = dr["DOCENTRY"].ToString();
                    oCN.UserFields.Fields.Item("U_SOENTRY").Value = int.Parse(dr["DOCENTRY"].ToString());

                    if (rounding != 0)
                    {
                        oInv.Rounding = SAPbobsCOM.BoYesNoEnum.tYES;
                        oInv.RoundingDiffAmount = rounding;
                    }

                    linenum = 0;
                    foreach (DataRow row in dt1.Rows)
                    {
                        linenum++;
                        if (linenum > 1) oInv.Lines.Add();
                        oInv.Lines.SetCurrentLine(linenum - 1);
                        oInv.Lines.ItemCode = row["ITEMCODE"].ToString();
                        if (!double.TryParse(row["QUANTITY"].ToString(), out qty)) qty = 0;
                        if (!double.TryParse(row["SUPPLIERPRICE"].ToString(), out price)) price = 0;
                        if (!double.TryParse(row["CSUOM"].ToString(), out uom)) uom = 0;
                        string taxcode = row["TAXCODE"].ToString();
                        dim2 = row["DIM2"].ToString();
                        csqty = 0;
                        pcsqty = 0;
                        if (uom != 0 && uom != 1)
                        {
                            csqty = Math.Floor(qty / uom);
                        }

                        pcsqty = qty - (csqty * uom);
                        oInv.Lines.Quantity = qty;
                        oInv.Lines.UnitPrice = price;
                        oInv.Lines.WarehouseCode = dr["WHSCODE"].ToString();
                        oInv.Lines.UserFields.Fields.Item("U_CTN").Value = csqty;
                        oInv.Lines.UserFields.Fields.Item("U_UNT").Value = pcsqty;
                        if (row["INGL"].ToString() != "")
                        {
                            oInv.Lines.AccountCode = row["INGL"].ToString();
                        }
                        if (taxcode.EndsWith("6"))
                        {
                            oInv.Lines.VatGroup = taxcode;
                        }
                        if (dim1 != "")
                        {
                            oInv.Lines.CostingCode = dim1;
                        }
                        if (dim2 != "")
                        {
                            oInv.Lines.CostingCode2 = dim2;
                        }
                        //oOrder.Lines.VatGroup = row["TAXCODE"].ToString();

                        // inv
                        // BATCH MAN
                        // handle batch is item is batch manage

                        var ManByBatch = row["MANBTCHNUM"].ToString();
                        var lineNum = row["LINENUM"].ToString();

                        if (ManByBatch.Equals("Y"))
                        {
                            // query the batch from INV3 
                            // loop each line in Diapi
                            var batches = docBatches_INV.Where(r => r.LineNum == linenum && r.TableName == "CN3").ToList(); // GetBatches(docentry, lineNum, "CN3");
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

                    if (oInv.Add() != 0)
                    {
                       // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        errmsg = oCompany.GetLastErrorDescription();
                        //throw new Exception(errmsg);
                        return errmsg;
                    }

                    oCompany.GetNewObjectCode(out invEntry);
                    var isloadInv = oInv1.GetByKey(int.Parse(invEntry));
                    if (!isloadInv)
                    {
                       // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        errmsg = oCompany.GetLastErrorDescription();
                        return errmsg;
                    }

                    //rs.DoQuery("UPDATE [" + webdb + "].[DBO].[CN] SET DOCSTATUS = 'C', INVENTRY = " + invEntry + ", SAPINV = 'Y' WHERE DOCENTRY = " + docentry );

                    // add credit not files 
                    // attach file 
                    // attache file if any 
                    // add file 
                    // 2022-03-04
                    var files0 = dr["FILES"].ToString();
                    if (!string.IsNullOrWhiteSpace(files0))
                    {
                        var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
                        var filesArry = files0.Split(",");
                        int fileCnt = 0;
                        for (int f = 0; f < filesArry.Length; f++)
                        {
                            var svyPhyPath = Path.Combine(SvrPath, filesArry[f]);
                            if (File.Exists(svyPhyPath))
                            {
                                // new add file                                                             
                                attachedments.Lines.Add();
                                attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
                                attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
                                attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
                                attachedments.Lines.Override = BoYesNoEnum.tYES;
                                fileCnt++;
                            }
                        }

                        if (fileCnt > filesArry.Length)
                        {
                            if (attachedments.Add() == 0)
                            {
                                int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
                                oCN1.AttachmentEntry = iAttEntry;
                                oCN1.Update();
                            }
                            else
                            {
                               // if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                                errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
                                return errmsg;
                            }
                        }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv1);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oInv);
                    oInv = null;
                    oInv1 = null;
                }

                #endregion create of invoice
                
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oCN1);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oCN);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);

                oCN = null;
                oCN1 = null;
                return "";
            }
            catch (Exception ex)
            {
                errmsg = ex.Message;
                //if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return errmsg;
            }
        }

        /// <summary>
        /// 20220217
        /// Query the line batch
        /// </summary>
        /// <param name="docEntry"></param>
        /// <param name="linenum"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        //List<Bread_Batch> GetBatches(string docEntry, string linenum, string tableName)
        //{
        //    try
        //    {
        //        var conn = new SqlConnection(Db.GetWebDbConnStr());
        //        var sql = $@"Select '{tableName}' [TableName], * 
        //                     from {Db.WEBDB}..{tableName} 
        //                     where DocEntry = @docEntry and LineNum = @lineNum";

        //        return conn.Query<Bread_Batch>(sql, new
        //        {
        //            docEntry = docEntry,
        //            lineNum = linenum
        //        }).ToList();
        //    }
        //    catch (Exception e)
        //    {
        //        LastErrorMesage = $"{e.Message}{e.StackTrace}";
        //        return null;
        //    }

        //}

        // get all batched 
        List<Bread_Batch> GetBatches_AllLine(string docEntry, string tableName)
        {
            var returnList = new List<Bread_Batch>();
            try
            {
                var conn = new SqlConnection(Db.GetWebDbConnStr());
                var sql = $@"Select '{tableName}' [TableName], * 
                             from {Db.WEBDB}..{tableName} 
                             where DocEntry = @docEntry ; ";

                var res = conn.Query<Bread_Batch>(sql, new
                {
                    docEntry
                }).ToList();

                if (res.Count > 0)
                {
                    returnList.AddRange(res);
                }
                return res;
            }
            catch (Exception e)
            {
                LastErrorMesage = $"{e.Message}{e.StackTrace}";
                return returnList;
            }
        }
    }

}

#region reserved code

// create payment 
//public string createPayment(DataTable dtHeader, DataTable dtAmount, DataTable dtInvoices, string userid)
//{
//    string errmsg = "";
//    connectSAP();

//    string webdb = Db.WEBDB; // AppUtilities.getWebDB();
//    try
//    {
//        oCompany.StartTransaction();
//        SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
//        try
//        {
//            foreach (DataRow row in dtHeader.Rows)
//            {
//                rs.DoQuery("SELECT * FROM [" + webdb + "].[DBO].[PAY] WHERE DOCENTRY = " + row["DOCENTRY"].ToString());
//                string docstatus = "";
//                if (rs.RecordCount > 0)
//                {
//                    rs.MoveFirst();
//                    docstatus = rs.Fields.Item("POSTED").Value.ToString();
//                }
//                if (docstatus == "P") continue;
//                SAPbobsCOM.Payments oPay = (SAPbobsCOM.Payments)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oIncomingPayments);
//                oPay.CardCode = row["CARDCODE"].ToString();
//                oPay.DocDate = DateTime.Today;
//                oPay.DueDate = DateTime.Today;
//                //oPay.LocalCurrency = SAPbobsCOM.BoYesNoEnum.tYES;

//                oPay.DocObjectCode = SAPbobsCOM.BoPaymentsObjectType.bopot_IncomingPayments;
//                oPay.DocType = SAPbobsCOM.BoRcptTypes.rCustomer;
//                oPay.UserFields.Fields.Item("U_TR").Value = row["DOCNUM"].ToString();
//                oPay.CounterReference = row["REFNO"].ToString();

//                decimal amount = 0;
//                int checkLine = 0;
//                foreach (DataRow rowA in dtAmount.Select("DOCENTRY = " + row["DOCENTRY"].ToString()))
//                {

//                    decimal lineamt = 0;
//                    if (!decimal.TryParse(rowA["TOTAL"].ToString(), out lineamt)) lineamt = 0;
//                    amount += lineamt;
//                    if (lineamt == 0) continue;
//                    string bank1ctry = rowA["BANK1COUNTRY"].ToString();
//                    oPay.CheckAccount = rowA["BANK2GL"].ToString();
//                    if (rowA["LINETYPE"].ToString().ToUpper() == "CHEQUE")//cheque
//                    {
//                        checkLine++;
//                        if (checkLine > 1) oPay.Checks.Add();
//                        oPay.Checks.SetCurrentLine(checkLine - 1);
//                        oPay.Checks.BankCode = rowA["BANK"].ToString();
//                        oPay.Checks.CheckNumber = int.Parse(rowA["LINEREF"].ToString());
//                        oPay.Checks.CheckSum = (double)lineamt;
//                        oPay.Checks.DueDate = DateTime.Parse(rowA["LINEDATE"].ToString());
//                        oPay.Checks.CheckAccount = rowA["BANK2GL"].ToString();
//                        if (bank1ctry != "")
//                        {
//                            oPay.Checks.CountryCode = bank1ctry;
//                        }
//                        else
//                        {
//                            oPay.Checks.CountryCode = "MY";
//                        }
//                    }
//                    else if (rowA["LINETYPE"].ToString().ToUpper() == "CASH")//cash
//                    {
//                        oPay.CashSum = (double)lineamt;
//                        oPay.CashAccount = rowA["BANK2GL"].ToString();

//                    }
//                    else //online bank in
//                    {
//                        oPay.TransferAccount = rowA["BANK2GL"].ToString();
//                        oPay.TransferDate = DateTime.Parse(rowA["LINEDATE"].ToString());
//                        oPay.TransferReference = rowA["LINEREF"].ToString();
//                        oPay.TransferSum = (double)lineamt;
//                    }

//                    rs.DoQuery("UPDATE [" + webdb + "].[DBO].[PAY2] SET BANK2 = '" + rowA["BANK2"].ToString() + "', CONFIRM = 'Y', BANKDATE = '" + DateTime.Parse(rowA["BANKDATE"].ToString()).ToString("yyyy-MM-dd") + "', BANKUSER = '" + userid + "', UPDDATE = GETDATE() WHERE DOCENTRY = " + rowA["DOCENTRY"].ToString() + " AND LINENUM = " + rowA["LINENUM"].ToString());
//                }
//                string updCmd = "IF NOT EXISTS (SELECT * FROM [" + webdb + "].[DBO].PAY2 WHERE DOCENTRY = " + row["DOCENTRY"].ToString() + " AND ISNULL(CONFIRM,'') <> 'Y' AND ISNULL(CANCEL,'') <> 'Y') BEGIN ";
//                updCmd += "UPDATE [" + webdb + "].[DBO].[PAY] SET POSTED = 'P' WHERE DOCENTRY = " + row["DOCENTRY"].ToString();
//                updCmd += " END ";
//                rs.DoQuery(updCmd);

//                decimal invAmount = 0;
//                int invLine = 0;
//                foreach (DataRow rowI in dtInvoices.Select("DOCENTRY = " + row["DOCENTRY"].ToString()))
//                {
//                    if (!decimal.TryParse(rowI["OPENAMT"].ToString(), out invAmount)) invAmount = 0;
//                    if (invAmount == 0) continue;
//                    if (amount <= 0) break;
//                    decimal paiSum = invAmount;
//                    if (amount < invAmount) paiSum = amount;
//                    invLine++;
//                    if (invLine > 1) oPay.Invoices.Add();
//                    oPay.Invoices.SetCurrentLine(invLine - 1);
//                    if (rowI["BASEENTRY"].ToString() == "")
//                    {
//                        oPay.Invoices.DocEntry = int.Parse(rowI["TRANSID"].ToString());
//                    }
//                    else
//                    {
//                        oPay.Invoices.DocEntry = int.Parse(rowI["BASEENTRY"].ToString());
//                    }

//                    BoRcptInvTypes docType = (BoRcptInvTypes)System.Enum.Parse(typeof(BoRcptInvTypes),
//                                                                                rowI["OBJECTCODE"].ToString());

//                    if (rowI["OBJECTCODE"].ToString() == "24" || rowI["OBJECTCODE"].ToString() == "30")
//                    {
//                        docType = SAPbobsCOM.BoRcptInvTypes.it_JournalEntry;
//                        oPay.Invoices.DocEntry = int.Parse(rowI["TRANSID"].ToString());
//                        oPay.Invoices.DocLine = int.Parse(rowI["TRANSLINE"].ToString());
//                    }
//                    oPay.Invoices.InvoiceType = docType;
//                    oPay.Invoices.SumApplied = (double)paiSum;

//                    rs.DoQuery("UPDATE [" + webdb + "].[DBO].[PAY1] SET BANKAMT = ISNULL(BANKAMT,0) + " + paiSum.ToString() + " WHERE DOCENTRY = " + rowI["DOCENTRY"].ToString() + " AND LINENUM = " + rowI["LINENUM"].ToString());

//                    amount = amount - invAmount;
//                }
//                if (amount > 0)
//                {

//                }
//                if (oPay.Add() != 0)
//                {
//                    errmsg = oCompany.GetLastErrorDescription();
//                    if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//                    oPay = null;
//                    return errmsg;
//                }
//                oPay = null;
//            }
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
//        }
//        catch (Exception ex)
//        {
//            errmsg = ex.Message;
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            rs = null;
//            return errmsg;
//        }

//        rs = null;
//        CollectGC();
//        return "";
//    }
//    catch (Exception ex)
//    {
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        errmsg = ex.Message;
//        return errmsg;
//    }
//}

//public string Add_FileAttachment(int docEntry, string svyPhyPath, BoObjectTypes objectTypes)
//{
//    try
//    {
//        string errmsg = "";
//        if (oCompany == null)
//        {
//            errmsg = connectSAP();
//        }
//        if (errmsg != "")
//        {
//            return errmsg;
//        }
//        if (!oCompany.Connected)
//        {
//            errmsg = connectSAP();
//        }
//        if (errmsg != "")
//        {
//            return errmsg;
//        }

//        errmsg = "";

//        string webdb = Db.WEBDB;
//        oCompany.StartTransaction();

//        Documents doc = (Documents)oCompany.GetBusinessObject(objectTypes);
//        doc.GetByKey(docEntry);

//        var attachedments = (Attachments2)oCompany.GetBusinessObject(BoObjectTypes.oAttachments2);
//        int fileCnt = 0;

//        if (File.Exists(svyPhyPath))
//        {
//            attachedments.Lines.Add();
//            attachedments.Lines.FileName = Path.GetFileNameWithoutExtension(svyPhyPath);
//            attachedments.Lines.FileExtension = Path.GetExtension(svyPhyPath).Substring(1);
//            attachedments.Lines.SourcePath = Path.GetDirectoryName(svyPhyPath);
//            attachedments.Lines.Override = BoYesNoEnum.tYES;
//            fileCnt++;
//        }

//        if (attachedments.Add() == 0)
//        {
//            int iAttEntry = int.Parse(oCompany.GetNewObjectKey());
//            doc.AttachmentEntry = iAttEntry;
//            doc.Update();
//            System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
//            System.Runtime.InteropServices.Marshal.ReleaseComObject(attachedments);
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
//            return "";
//        }
//        else
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = $"{oCompany.GetLastErrorCode()}\n{oCompany.GetLastErrorDescription()}\n";
//            return errmsg;
//        }
//    }
//    catch (Exception e)
//    {
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//        LastErrorMesage = $"{e.Message}\n{e.StackTrace}";
//        return LastErrorMesage;
//    }
//}

// create sales order 
//public string createOrders(string docentry, DataTable dt, DataTable dt1)
//{
//    string errmsg = "";
//    connectSAP();

//    try
//    {
//        string webdb = Db.WEBDB;  //AppUtilities.getWebDB();

//        oCompany.StartTransaction();

//        SAPbobsCOM.Recordset rs = (Recordset)oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
//        rs.DoQuery("SELECT * FROM [" + webdb + "].[DBO].[PO] WHERE DOCENTRY = " + docentry + " AND ISNULL(POSTED,'') <> 'Y'");
//        if (rs.RecordCount <= 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
//            rs = null;
//            return "";
//        }
//        Documents oOrder = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oOrders);

//        DataRow dr = dt.Rows[0];
//        oOrder.CardCode = dr["COMPANYID"].ToString();
//        oOrder.DocDate = DateTime.Today;
//        oOrder.Comments = dr["REMARKS"].ToString();
//        oOrder.DocType = SAPbobsCOM.BoDocumentTypes.dDocument_Items;
//        oOrder.NumAtCard = dr["DOCNUM"].ToString();
//        oOrder.DocDueDate = DateTime.Today.AddDays(1);
//        int linenum = 0;
//        double qty = 0, price = 0;
//        foreach (DataRow row in dt1.Rows)
//        {
//            linenum++;
//            if (linenum > 1) oOrder.Lines.Add();
//            oOrder.Lines.SetCurrentLine(linenum - 1);
//            oOrder.Lines.ItemCode = row["ITEMCODE"].ToString();
//            if (!double.TryParse(row["QUANTITY"].ToString(), out qty)) qty = 0;
//            if (!double.TryParse(row["PRICE"].ToString(), out price)) price = 0;

//            oOrder.Lines.Quantity = qty;
//            oOrder.Lines.UnitPrice = price;
//            //oOrder.Lines.VatGroup = row["TAXCODE"].ToString();
//        }

//        if (oOrder.Add() != 0)
//        {
//            if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//            errmsg = oCompany.GetLastErrorDescription();
//        }
//        else
//        {
//            string sapentry = "";
//            oCompany.GetNewObjectCode(out sapentry);
//            rs.DoQuery("UPDATE [" + webdb + "].[DBO].[PO] SET POSTED = 'Y', POSTEDDATE = GETDATE(), SAPENTRY = " + sapentry + " WHERE DOCENTRY = " + docentry);
//        }

//        System.Runtime.InteropServices.Marshal.ReleaseComObject(oOrder);
//        System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);

//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
//    }
//    catch (Exception ex)
//    {
//        errmsg = ex.Message;
//        if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
//    }
//    if (oCompany.InTransaction) oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);

//    //AppUtilities.garbageCollector();
//    CollectGC();
//    return errmsg;
//}



#endregion


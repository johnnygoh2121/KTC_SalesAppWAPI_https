using Dapper;
using KTC_SalesAppWAPI.DTOs;
using KTC_SalesAppWAPI.DTOs.Bread_Return;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.DiApi;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadReturn;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.DN;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadCogReturnController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadCogReturnController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string LastError = "";
        string _fileSavePath = "";

        public BreadCogReturnController(IConfiguration configuration, ILogger<BreadCogReturnController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _fileSavePath = _configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_BreadReturn dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetReturnCns":
                    {
                        return GetReturnCns(dto);
                    }
                case "GetReturnCns_Dist":
                    {
                        return GetReturnCns_Dist(dto);
                    }
                case "QueryRetCnDocByDocEntry":
                    {
                        return QueryRetCnDocByDocEntry(dto);
                    }
                case "LoadBread_DraftLines":
                    {
                        return LoadBread_DraftLines(dto);
                    }
                case "LoadBread_Dist_DraftLines":
                    {
                        return LoadBread_Dist_DraftLines(dto);
                    }
                case "SaveBreadDraftLines": // coming soon
                    {
                        return SaveBreadDraftLines(dto, "FTAPP_RCN1_Draft");
                    }
                case "Save_DistBreadDraftLines":
                    {
                        return Save_DistBreadDraftLines(dto);
                    }
                case "SaveBreadReturn":
                    {
                        return SaveBreadReturn(dto);
                    }
                case "LoadBread_RcnLines":
                    {
                        return LoadBread_RcnLines(dto);
                    }
                case "LoadBread_DistRcnLines":
                    {
                        return LoadBread_DistRcnLines(dto);
                    }
                case "Save_DistBreadReturn":
                    {
                        return Save_DistBreadReturn(dto);
                    }
                case "UpdateDistDocSign":
                    {
                        return UpdateDistDocSign(dto);
                    }
                case "LoadWhs_Bread":
                    {
                        return LoadWhs_Bread(dto);
                    }
                case "CreditMemo_Aging_Bread":
                    {
                        return CreditMemo_Aging_Bread(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult CreditMemo_Aging_Bread(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (dto.CNNumber <= 0)
                {
                    return BadRequest("Invalid cn number");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("the company name is invalid");
                }

                var sql = @"exec sp_SelecCreditMemo @sapDb, @DocNum";
                using var conn = new SqlConnection(_commDbConnStr);
                var result = conn.Query<ORIN>(sql, new { sapDb = db.SAPDB, DocNum = dto.CNNumber }).FirstOrDefault();

                // 20240805
                // read the card code billing address 
                // get the bill address 
                // get the s address type 
                // get shipment address
                var sp_queryAddress = @$"SELECT * 
                            FROM {db.SAPDB}..CRD1 WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                var bill_address = conn.Query<CRD1>(sp_queryAddress, new { SoDocStoreCard = result.CardCode }).FirstOrDefault();
                result.Address = bill_address.GetAddress();


                if (result == null) return NotFound();
                sql = "exec sp_SelecCreditMemoLine @sapDb, @docEntry";
                result.Lines = conn.Query<RIN1>(sql, new
                {
                    sapDb = db.SAPDB,
                    docEntry = result.DocEntry
                }).ToList();

                // load line batch is any 
                for (int i = 0; i < result.Lines.Count; i++)
                {
                    var line = result.Lines[i];
                    if ($"{line.ManBtchNum}" == "N") continue;
                    var sp_batch = @"exec sp_SelecDocLineBatch @sapDb, @baseEntry, @baseLineNum, @itemCode, @docType";

                    result.Lines[i].Batches = conn.Query<Batch>(sp_batch, new
                    {
                        sapDb = db.SAPDB,
                        baseEntry = result.DocEntry,
                        baseLineNum = line.LineNum,
                        itemCode = line.ItemCode,
                        docType = 14 // credit memo line
                    }).ToList();
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadWhs_Bread(Dto_BreadReturn dto)
        {
            try
            {
                // load sap available warehouse 
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Query company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Query company info is empty");
                }

                var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[OWHS] WITH (NOLOCK) 
                             WHERE Locked = 'N'";

                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var list = conn.Query<OWHS_Ext>(sql).ToList();
                    if (list == null) return NotFound();
                    return Ok(list);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult UpdateDistDocSign(Dto_BreadReturn dto)
        {
            try
            {
                // validation
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.SignFiles))
                {
                    return BadRequest("Invalid files names");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var conn = new SqlConnection(_commDbConnStr_bread);

                // query to get the prevous file, and update will later
                // 2021-07-18 add and update file from app 
                // check the exiting append in the file name behind
                var sqlDocFiles = $@"SELECT FILES
                                          FROM [{db.WEBDB}]..[{dto.DocType}] with (nolock)  
                                          WHERE DocEntry = @docEntry";

                var existingFiles = conn.ExecuteScalar<string>(sqlDocFiles, new { docEntry = dto.DocEntry });
                if (!string.IsNullOrWhiteSpace(existingFiles))
                {
                    existingFiles += "," + dto.SignFiles;
                }
                else
                {
                    existingFiles = dto.SignFiles;
                }

                // check connection 
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();

                // update the portal table
                var sql = @$"UPDATE [{db.WEBDB}]..[{dto.DocType}] 
                                SET FILES = @Signed
                                WHERE DocEntry = @docEntry";

                var res = conn.Execute(sql, new { docEntry = dto.DocEntry, Signed = existingFiles }, trans);
                if (res <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error update pic file path, subsi {db.COMPANYNAME}, Doc Entry: {dto.DocEntry} ");
                }

                // attach the file into SAP                
                // query the doc from table
                var queryDoc = @$"Select * from {db.WEBDB}..{dto.DocType} Where DocEntry = @docentry";
                var doc = conn.Query<dynamic>(queryDoc, new { docentry = dto.DocEntry }, trans).FirstOrDefault();
                if (doc == null)
                {
                    trans.Rollback();
                    return BadRequest("Error reading Doc from database, please try again [ADF]");
                }

                // getting sap doc entry
                var sapDocEntry = dto.DocType == "CN" ? (int)doc.CNENTRY : (int)doc.INVENTRY;

                var sapTable = dto.DocType == "CN" ? "ORIN" : "OINV";
                SAPbobsCOM.BoObjectTypes objType = dto.DocType == "CN" ?
                                                   SAPbobsCOM.BoObjectTypes.oCreditNotes :
                                                   SAPbobsCOM.BoObjectTypes.oInvoices;


                // attach the file into sap
                var helper = new BreadDiApi_Delivery(db);
                var physFilePath = Path.Combine(_fileSavePath, dto.SignFiles);
                string err = helper.Add_FileAttachment(sapDocEntry, physFilePath, objType);
                if (string.IsNullOrWhiteSpace(err))
                {
                    return Ok();
                }

                trans.Commit();
                return BadRequest(err);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Save_DistBreadReturn(Dto_BreadReturn dto)
        {
            try
            {
                // validation
                if (dto.CnDoc == null)
                {
                    return BadRequest("Invalid cn doc");
                }
                if (dto.CnDoc.Lines == null)
                {
                    return BadRequest("Invalid cn doc lines");
                }
                if (dto.CnDoc.Lines.Count == 0)
                {
                    return BadRequest("Invalid cn doc lines [Zl]");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // <-- whs user
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.UserName)) // <-- whs user
                {
                    return BadRequest("Invalid user name");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                // query to get the dist user profile
                var conn = new SqlConnection(_commDbConnStr_bread);
                var queryDistUserProfile = @$"Select * 
                                              from {db.WEBDB}..USERS with (nolock) 
                                              Where CompanyID = @companyId";

                var distProfile = conn.Query<Bread_User>(queryDistUserProfile, new
                {
                    companyId = dto.CnDoc.COMPANYID
                }).FirstOrDefault();

                if (distProfile != null && !string.IsNullOrWhiteSpace($"{distProfile.USERID}"))
                {
                    dto.CnDoc.UCREATED = $"{distProfile.USERID}";
                    dto.CnDoc.UMODIFIED = $"{distProfile.USERID}";
                }

                // create portal cn
                // insert the cn line 
                // save draft of save submit 
                var breadDocHelper = new Bread_CNDocHelper();
                long docEntry = -1;
                if (dto.CnDoc.DOCENTRY == 0)
                {
                    docEntry = breadDocHelper.CreateBread_CN(dto.CnDoc, db, _commDbConnStr_bread);
                }
                else
                {
                    docEntry = breadDocHelper.UpdateDraft_CN(dto.CnDoc, db, _commDbConnStr_bread);
                }

                if (docEntry == -1)
                {
                    return BadRequest(breadDocHelper.LastErrorMessage);
                }

                // if save draft then return doc entry
                if (dto.SaveType == "draft")
                {
                    var draft_replied = new BreadDocReplied
                    {
                        DocEntry = docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };
                    return Ok(draft_replied);
                }

                // create dist sap cn 
                // create SAP CN then KTC store invoice                
                var errorMsg = HandlerDistCreateCN_KDist(conn, db, docEntry);
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    return BadRequest(errorMsg);
                }

                // replied the success create of the inv doc 
                // but wait posting to SAP
                var sql3 = $@"select t0.DocNum 
                                    from {db.SAPDB}..ORIN t0 with (nolock)
                                    inner join 
                                        {db.WEBDB}..CN t1 with (nolock) on t1.CNENTRY = t0.DocEntry 
                                    Where t1.DOCENTRY = @docentry";

                var cnNum = conn.ExecuteScalar<string>(sql3, new { docentry = docEntry });
                var replied = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = cnNum
                };

                // remove the draft entry from the share table
                var removeDraftSql = @$"Delete from {db.WEBDB}..FTAPP_DIST_RCN1_DRAFT
                                  Where UserCode = @userCode and DiCardCode = @diCardCode ";

                conn.Execute(removeDraftSql, new
                {
                    userCode = dto.UserCode,
                    diCardCode = dto.CnDoc.OwnerCode
                });

                var queryCn = $@"select * from {db.WEBDB}..CN Where DocEntry = @docEntry";
                var portalCn = conn.Query<Bread_CN_Ext>(queryCn, new { docEntry }).FirstOrDefault();
                if (portalCn != null)
                {
                    dto.CnDoc.DOCENTRY = docEntry;
                    dto.CnDoc.DOCNUM = cnNum;
                    dto.CnDoc.ReceiverCode = dto.UserCode;
                    dto.CnDoc.ReceiverName = dto.UserName;
                    dto.CnDoc.SAPINV = portalCn.SAPINV;
                    dto.CnDoc.CNENTRY = portalCn.CNENTRY;
                    BuildRCN(db, dto.CnDoc, conn);
                }
                else
                {
                    LastError = $"Miss update of RCN for portal cn doc entry: {db.WEBDB}, {docEntry}";
                    _logger.LogError(LastError);
                }

                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void BuildRCN(DbInfo db, Bread_CN_Ext head, SqlConnection conn)
        {
            try
            {
                // massage the docentry 
                if (head.Lines != null)
                {
                    for (int i = 0; i < head.Lines.Count; i++)
                    {
                        head.Lines[i].DOCENTRY = head.DOCENTRY;
                    }
                }


                if (head.Batches != null)
                {
                    for (int i = 0; i < head.Batches.Count; i++)
                    {
                        head.Batches[i].DocEntry = head.DOCENTRY;
                    }
                }

                head.Subsi = db.COMPANYNAME;
                head.SubsiId = db.COMPANYID;
                head.DOCSTATUS = "C";


                var dto = new Dto_BreadReturn
                {
                    Subsi = db.COMPANYNAME,
                    CnDoc = head,
                    BreadLines = head.Lines,
                };

                SaveBreadReturn(dto);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        string HandlerDistCreateCN_KDist(SqlConnection conn, DbInfo db, long docEntry)
        {
            try
            {
                LastError = "";
                // parepare the invoice data table 
                var query = $@"SELECT T0.*
                            , T1.CARDCODE AS [SAPCARDCODE]
                            , T2.CNARINVSERIES AS [INVSERIES]
                            , T2.CNARCNSERIES AS [CNSERIES]
                            , CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE ELSE U0.DEFWHS END AS [WHSCODE]
                            , T3.SERIESNAME
                            , T4.PRCCODE AS [DIM1] 
                            , T0.FILES [FILES]
                            , T1.SlpCode [SLPCODE]
                            FROM {db.WEBDB}..CN T0 INNER JOIN {db.SAPDB}..[OCRD] T1 ON 
                                                        --ISNULL(T1.U_PORTALID, T0.CARDCODE) = T0.CARDCODE 
                                                        T1.CardCode = t0.CardCode    
                                                        AND T1.CARDTYPE  = 'C' 
                                                        AND T1.FrozenFor = 'N'

                            LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 ON T2.RECID = '1'                             
                            LEFT OUTER JOIN {db.SAPDB}.[DBO].[NNM1] T3 ON T3.SERIES = T2.CNARCNSERIES 
                            LEFT OUTER JOIN {db.SAPDB}..[OPRC] T4 ON T4.PRCCODE = T1.U_COSTCTR AND T4.DimCode = '1' 
                            LEFT OUTER JOIN {db.WEBDB}..USERS U0 ON U0.USERID = T0.UMODIFIED 
                            WHERE T0.DOCENTRY = '{docEntry}'";


                var cn_dt = GetDataTable(conn, query);
                if (cn_dt == null)
                {
                    return LastError;
                }
                if (cn_dt.Rows.Count == 0)
                {
                    return LastError;
                }

                var sapcard = cn_dt.Rows[0]["SAPCARDCODE"].ToString();
                var priceList = db.DEF_PRICELIST;

                query = $@"SELECT T1.*
                            , T9.WHSCODE [WHSCODE] 
                            , T1.PRICE AS[CUSTPRICE]
                            , T5.Price AS[SUPPLIERPRICE]
                            , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 ELSE T6.U_CSUS_UOM END AS [CSUOM]
                            , ISNULL(T7.GLCODE,'') AS [INGL]
                            , T8.PRCCODE AS [DIM2]  
                            , T6.ManBtchNum [MANBTCHNUM]
                            , T9.ReasonCode [U_CSUS_RC]
                            FROM {db.WEBDB}..CN T0 INNER JOIN {db.WEBDB}..CN1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
                            INNER JOIN {db.SAPDB}..[OCRD] T2 ON ISNULL(T2.U_PORTALID, T0.CARDCODE) = T0.CARDCODE 
                                                AND T2.CARDTYPE = 'C' 
                                                AND T2.FrozenFor = 'N' 
                                                AND T2.CardCode = '{sapcard}'
                            LEFT OUTER JOIN {db.SAPDB}..[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
                            LEFT OUTER JOIN {db.SAPDB}..[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
                                                AND T4.PriceList = T2.ListNum 
                            LEFT OUTER JOIN {db.SAPDB}..[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
                                                AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum ELSE '{priceList}' END
                            LEFT OUTER JOIN {db.SAPDB}..[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
                            LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
                            LEFT OUTER JOIN {db.SAPDB}..[OPRC] T8 ON T8.PRCCODE = T6.CardCode AND T8.DimCode = '2' 
                            LEFT OUTER JOIN {db.WEBDB}..TrcnLineDetails T9 ON T9.DOCENTRY = T1.DOCENTRY 
                                                AND T9.LINENUM = T1.LINENUM
                                                AND T9.MODULE = 'DistBreadTrcn'
                            WHERE T0.DOCENTRY = '{docEntry}'";

                var cn1_dt = GetDataTable(conn, query);
                if (cn1_dt == null)
                {
                    return LastError;
                }
                if (cn1_dt.Rows.Count == 0)
                {
                    return LastError;
                }

                var diapiHelper = new BreadDiApi_Trade(db, _fileSavePath);
                return diapiHelper.createCNInv($"{docEntry}", cn_dt, cn1_dt, true, true); // just create invoice , no cn
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return LastError;
            }
        }

        DataTable GetDataTable(SqlConnection conn, string query)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd); // create data adapter                
                DataTable dt = new DataTable();
                da.Fill(dt); // this will query your database and return the result to your datatabled
                da.Dispose();
                return dt;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }

        }

        IActionResult SaveBreadReturn(Dto_BreadReturn dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid SubSi");
            }
            if (dto.CnDoc == null)
            {
                return BadRequest("invalid cn doc");
            }
            if (dto.BreadLines == null)
            {
                return BadRequest("invalid cn lines");
            }
            if (dto.BreadLines.Count == 0)
            {
                return BadRequest("invalid cn lines (0)");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Invalid dbi");
            }

            // delete head and lines 
            using (var conn = new SqlConnection(_commDbConnStr_bread))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var deleteSql = @$"delete from {db.WEBDB}..FTAPP_RCN Where DocEntry = @docEntry;
                                           delete from {db.WEBDB}..FTAPP_RCN1 Where DocEntry = @docEntry;
                                           delete from {db.WEBDB}..FTAPP_RCN3 Where DocEntry = @docEntry; ";

                        conn.Execute(deleteSql, new { docEntry = dto.CnDoc.DOCENTRY }, trans);

                        // insert the head for RCN 
                        var inserthead = @$" INSERT INTO {db.WEBDB}..FTAPP_RCN (
                                                         Subsi
                                                       , SubsiId
                                                       , OwnerCode
                                                       , OwnerName
                                                       , DOCENTRY
                                                       , DOCSTATUS
                                                       , BASEDOCNUM
                                                       , DOCNUM
                                                       , COMPANYID
                                                       , CARDCODE
                                                       , CARDNAME                                                      
                                                       , CURRENCY
                                                       , DOCRATE
                                                       , CUSTREF
                                                       , BILLADD1
                                                       , BILLADD2
                                                       , BILLADD3
                                                       , BILLADD4
                                                       , BILLADD5
                                                       , TEL
                                                       , FAX
                                                       , CONTACT
                                                       , TOTALBD
                                                       , TAXSUM
                                                       , ROUNDING
                                                       , DOWNPAYMENT
                                                       , DOCTOTAL
                                                       , PRICEID
                                                       , REMARKS
                                                       , REASON
                                                       , UCREATED                                                       
                                                       , UMODIFIED                                                       
                                                       , PAIDTODATE
                                                       , INVENTRY
                                                       , CNENTRY
                                                       , SAPINV
                                                       , FILES
                                                       ,TransporterCode
                                                       ,ReceiverCode
                                                       ,ReceiverName
                                                       ,ReceivedDt ";

                        var insertdetail = @$" ) values ( @Subsi
                                                  ,@SubsiId
                                                  ,@OwnerCode
                                                  ,@OwnerName
                                                  ,@DOCENTRY
                                                  ,@DOCSTATUS
                                                  ,@BASEDOCNUM
                                                  ,@DOCNUM
                                                  ,@COMPANYID
                                                  ,@CARDCODE
                                                  ,@CARDNAME                                                      
                                                  ,@CURRENCY
                                                  ,@DOCRATE
                                                  ,@CUSTREF
                                                  ,@BILLADD1
                                                  ,@BILLADD2
                                                  ,@BILLADD3
                                                  ,@BILLADD4
                                                  ,@BILLADD5
                                                  ,@TEL
                                                  ,@FAX
                                                  ,@CONTACT
                                                  ,@TOTALBD
                                                  ,@TAXSUM
                                                  ,@ROUNDING
                                                  ,@DOWNPAYMENT
                                                  ,@DOCTOTAL
                                                  ,@PRICEID
                                                  ,@REMARKS
                                                  ,@REASON
                                                  ,@UCREATED                                                       
                                                  ,@UMODIFIED                                                       
                                                  ,@PAIDTODATE
                                                  ,@INVENTRY
                                                  ,@CNENTRY
                                                  ,@SAPINV
                                                  ,@FILES
                                                  ,@TransporterCode
                                                  ,@ReceiverCode
                                                  ,@ReceiverName
                                                  ,GETDATE() ";

                        // dynamic sql to add date 
                        if (dto.CnDoc.DOCDATE != default)
                        {
                            inserthead += ",DOCDATE ";
                            insertdetail += ",@DOCDATE ";
                        }

                        if (dto.CnDoc.BASEDOCDATE != default)
                        {
                            inserthead += ",BASEDOCDATE ";
                            insertdetail += ",@BASEDOCDATE ";
                        }

                        if (dto.CnDoc.DCREATED != default)
                        {
                            inserthead += ",DCREATED ";
                            insertdetail += ",@DCREATED ";
                        }

                        if (dto.CnDoc.DMODIFIED != default)
                        {
                            inserthead += ",DMODIFIED ";
                            insertdetail += ",@DMODIFIED ";
                        }

                        var combineSql = $"{inserthead}{insertdetail}) ";
                        conn.Execute(combineSql, dto.CnDoc, trans);

                        SaveBreadLines(dto.BreadLines, conn, trans, db.WEBDB, dto.CnDocEntry, "FTAPP_RCN1");
                        if (dto.CnDoc.Batches != null)
                        {
                            var insertBatch = @$"Insert into {db.WEBDB}..FTAPP_RCN3 ( 
                                        DOCENTRY
                                      , LINENUM
                                      , LINENUM2
                                      , BATCHNO
                                      , QUANTITY 
                                    ) values (
                                       ,@DOCENTRY
                                       ,@LINENUM
                                       ,@LINENUM2
                                       ,@BATCHNO
                                       ,@QUANTITY
                                    )";

                            conn.Execute(insertBatch, dto.CnDoc.Batches, trans);
                        }

                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        LastError = $"{e.Message}\n{e.StackTrace}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }
                }   
            }

            //prepare good issue for return
            var diHelper = new BreadDiApi_Delivery(db, dto.CnDoc, dto.BreadLines, _commDbConnStr_bread, -1, "OIGE"
                                                        , SAPbobsCOM.BoObjectTypes.oInventoryGenExit);

            var remarks = $"GOODS ISSUES FROM TRCN #{dto.CnDoc.SapCnDocNum}, {DateTime.Now:dd-MMM-yyyy}";
            var err = diHelper.CreateGoodIssueAndUpdate_CN(remarks, dto.CnDoc.CNENTRY);

            // no success
           if (!string.IsNullOrWhiteSpace(err))
            {
                _logger.LogError(err); // 20221202 // login the err message 

                // sleep for 1 second and repost again
                // 20221215 add in to repost 
                System.Threading.Thread.Sleep(888);
                err = diHelper.CreateGoodIssueAndUpdate_CN(remarks, dto.CnDoc.CNENTRY);
                if (!string.IsNullOrWhiteSpace(err))
                {
                    // delete the saved rcn data 
                    using (var conn1 = new SqlConnection(_commDbConnStr_bread))
                    {
                        conn1.Open();
                        using( var trans1 = conn1.BeginTransaction())
                        {
                            try
                            {
                                var deleteSql1 = @$"delete from {db.WEBDB}..FTAPP_RCN Where DocEntry = @docEntry;
                                                    delete from {db.WEBDB}..FTAPP_RCN1 Where DocEntry = @docEntry; 
                                                    delete from {db.WEBDB}..FTAPP_RCN3 Where DocEntry = @docEntry; ";

                                var res1 = conn1.Execute(deleteSql1, new { docEntry = dto.CnDoc.DOCENTRY } , trans1);

                                trans1.Commit();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"to delete after SAP fail, {e.Message}\n{e.StackTrace}");
                                _logger.LogError(err);
                                trans1.Rollback();
                            }
                        }
                    }

                    _logger.LogError(err); // 20221202 // login the err message 
                    return BadRequest(err);
                }
            }

            // if success 
            // check it GI created 
            // 20240805
            
            using (var conn2 = new SqlConnection(_commDbConnStr_bread)) 
            {
                var sp_checkGiDocEntry = @$"select GIENTRY 
                                            from {db.WEBDB}..FTAPP_RCN 
                                            where DOCENTRY = @docentry ";

                var isGIEntry = conn2.ExecuteScalar<int>(sp_checkGiDocEntry, new { docentry = dto.CnDoc.DOCENTRY });
                if (isGIEntry <= 0)
                {
                    // remove the save RCN record 
                    if (conn2.State == ConnectionState.Closed) conn2.Open();
                    using( var trans2 = conn2.BeginTransaction() ) 
                    {
                        var deleteSql2 = @$"delete from {db.WEBDB}..FTAPP_RCN  Where DocEntry = @docEntry;
                                            delete from {db.WEBDB}..FTAPP_RCN1 Where DocEntry = @docEntry; 
                                            delete from {db.WEBDB}..FTAPP_RCN3 Where DocEntry = @docEntry; ";

                        var res = conn2.Execute(deleteSql2, new { docentry = dto.CnDoc.DOCENTRY }, trans2);
                        trans2.Commit();
                        return BadRequest($"Error create the Good issue for this CN#{dto.CnDoc.DOCENTRY}, Please try again.");
                    }
                }
            }

            // else process as per normal 

            // update SAP Cn with Good issue # as ref2 
            //var cnRemarks = $"GOODS ISSUES #{diHelper.PostedDocNum} FOR TRCN, {DateTime.Now:dd-MMM-yyyy}";
            //err = diHelper.UpdateDocRef2(dto.CnDoc.CNENTRY, SAPbobsCOM.BoObjectTypes.oCreditNotes, diHelper.PostedDocNum, cnRemarks);

            var replied = new BreadDocReplied
            {
                DocEntry = dto.CnDoc.DOCENTRY,
                IsSuccess = true,
                LastErrorMessage = "",
                DocNum = diHelper.PostedDocNum // GI posted document
            };

            return Ok(replied);
        }

        IActionResult Save_DistBreadDraftLines(Dto_BreadReturn dto)
        {
            try
            {
                if (dto.BreadLines == null)
                {
                    return BadRequest("draft line empty");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Di CardCode empty");
                }

                if (string.IsNullOrWhiteSpace(dto.DiCardCode))
                {
                    return BadRequest("Di CardCode empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }

                // delete the draft 
                var deletedraftSql = @$"Delete 
                                        from {db.WEBDB}..FTAPP_DIST_RCN1_DRAFT 
                                        Where UserCode = @userCode 
                                        and   DiCardCode = @diCardCode ";

                var conn = new SqlConnection(_commDbConnStr_bread);
                conn.Execute(deletedraftSql, new { userCode = dto.UserCode, diCardCode = dto.DiCardCode });

                // insert the draft with guid 
                // insert the table 
                for (int i = 0; i < dto.BreadLines.Count; i++)
                {
                    var line = dto.BreadLines[i];
                    if (line == null) continue;

                    var insertHead = @$" INSERT INTO {db.WEBDB}..FTAPP_DIST_RCN1_DRAFT (
                                             DOCENTRY
                                           , LINENUM
                                           , BASEENTRY
                                           , BASELINE
                                           , ITEMCODE
                                           , ITEMNAME
                                           , QUANTITY
                                           , PRICE
                                           , TAXCODE
                                           , TAXPERC
                                           , TAXSUM
                                           , LINETOTAL
                                           , LINETYPE                                        
                                           , Remark
                                           , LotNo
                                           , WhsCode
                                           , Reason
                                           , UomQty
                                           , AgencyCode
                                           , CodeBars
                                           , LineGuid
                                           , QtyInPcs
                                           , CnIssueQty
                                           , ReceivedQty
                                           , VarianceQty
                                           , ManBtchNum , RtnQty, UserCode, DiCardCode ";

                    var insertTail = @") values( @DOCENTRY
                                    ,@LINENUM
                                    ,@BASEENTRY
                                    ,@BASELINE
                                    ,@ITEMCODE
                                    ,@ITEMNAME
                                    ,@QUANTITY
                                    ,@PRICE
                                    ,@TAXCODE
                                    ,@TAXPERC
                                    ,@TAXSUM
                                    ,@LINETOTAL
                                    ,@LINETYPE                                        
                                    ,@Remark
                                    ,@LotNo
                                    ,@WhsCode
                                    ,@Reason
                                    ,@UomQty
                                    ,@AgencyCode
                                    ,@CodeBars
                                    ,@LineGuid
                                    ,@QtyInPcs
                                    ,@CnIssueQty
                                    ,@ReceivedQty
                                    ,@VarianceQty 
                                    ,@ManBtchNum ,  @RtnQty,  @UserCode, @DiCardCode";

                    if (line.ExpDate != default)
                    {
                        insertHead += ",ExpDate";
                        insertTail += ",@ExpDate";
                    }
                    if (line.MfrDt != default)
                    {
                        insertHead += ",MfrDt";
                        insertTail += ",@MfrDt";
                    }

                    var combine_sq = $"{insertHead} {insertTail})";
                    conn.Execute(combine_sq, line);

                    // check batch and insert batch 
                    //if (line.Batches != null && line.Batches.Count > 0)
                    //{
                    //    // insert the batch in RCN3
                    //    var sqlBatch = @$"INSERT INTO {db.WEBDB}..FTAPP_RCN3 ( 
                    //                     DOCENTRY 
                    //                   , LINENUM 
                    //                   , LINENUM2
                    //                   , BATCHNO
                    //                   , QUANTITY   
                    //                 ) values (
                    //                     @DOCENTRY
                    //                    ,@LINENUM 
                    //                    ,@LINENUM2
                    //                    ,@BATCHNO
                    //                    ,@QUANTITY ) ";

                    //    conn.Execute(sqlBatch, line.Batches);
                    //}
                }

                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult SaveBreadDraftLines(Dto_BreadReturn dto, string saveTable)
        {

            if (dto.BreadLines == null)
            {
                return BadRequest("draft line empty");
            }
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("subsi is empty");
            }
            if (dto.CnDocEntry < 0)
            {
                return BadRequest("doc entry empty");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
            if (db == null)
            {
                return BadRequest("dbi is empty");
            }

            // delete the draft 
            var deletedraftSql = @$"Delete from {db.WEBDB}..{saveTable} Where DocEntry = @docentry";
            using var conn = new SqlConnection(_commDbConnStr_bread);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                conn.Execute(deletedraftSql, new { docentry = dto.CnDocEntry }, trans);

                // insert the table 
                SaveBreadLines(dto.BreadLines, conn, trans, db.WEBDB, dto.CnDocEntry, "FTAPP_RCN1_DRAFT");
                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void SaveBreadLines(List<Bread_CN1_Ext> lines, SqlConnection conn, SqlTransaction trans,
            string webDb, long docEntry, string saveTable)
        {
            try
            {
                // insert the table 
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line == null) continue;

                    var insertHead = @$" INSERT INTO {webDb}..{saveTable} (
                                             DOCENTRY
                                           , LINENUM
                                           , BASEENTRY
                                           , BASELINE
                                           , ITEMCODE
                                           , ITEMNAME
                                           , QUANTITY
                                           , PRICE
                                           , TAXCODE
                                           , TAXPERC
                                           , TAXSUM
                                           , LINETOTAL
                                           , LINETYPE                                        
                                           , Remark
                                           , LotNo
                                           , WhsCode
                                           , Reason
                                           , UomQty
                                           , AgencyCode
                                           , CodeBars
                                           , LineGuid
                                           , QtyInPcs
                                           , CnIssueQty
                                           , ReceivedQty
                                           , VarianceQty
                                           , ManBtchNum , RtnQty";

                    var insertTail = @") values( @DOCENTRY
                                    ,@LINENUM
                                    ,@BASEENTRY
                                    ,@BASELINE
                                    ,@ITEMCODE
                                    ,@ITEMNAME
                                    ,@QUANTITY
                                    ,@PRICE
                                    ,@TAXCODE
                                    ,@TAXPERC
                                    ,@TAXSUM
                                    ,@LINETOTAL
                                    ,@LINETYPE                                        
                                    ,@Remark
                                    ,@LotNo
                                    ,@WhsCode
                                    ,@Reason
                                    ,@UomQty
                                    ,@AgencyCode
                                    ,@CodeBars
                                    ,@LineGuid
                                    ,@QtyInPcs
                                    ,@CnIssueQty
                                    ,@ReceivedQty
                                    ,@VarianceQty 
                                    ,@ManBtchNum ,  @RtnQty";

                    if (line.ExpDate != default)
                    {
                        insertHead += ",ExpDate";
                        insertTail += ",@ExpDate";
                    }
                    if (line.MfrDt != default)
                    {
                        insertHead += ",MfrDt";
                        insertTail += ",@MfrDt";
                    }

                    var combine_sq = $"{insertHead} {insertTail})";
                    conn.Execute(combine_sq, line, trans);

                    // check batch and insert batch 
                    if (line.Batches != null && line.Batches.Count > 0)
                    {
                        // insert the batch in RCN3
                        var sqlBatch = @$"INSERT INTO {webDb}..FTAPP_RCN3 ( 
                                             DOCENTRY 
                                           , LINENUM 
                                           , LINENUM2
                                           , BATCHNO
                                           , QUANTITY   
                                         ) values (
                                             @DOCENTRY
                                            ,@LINENUM 
                                            ,@LINENUM2
                                            ,@BATCHNO
                                            ,@QUANTITY ) ";

                        conn.Execute(sqlBatch, line.Batches, trans);
                    }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult LoadBread_DistRcnLines(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("subsi is empty");
                }
                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("doc entry empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }

                var query = $@"select * , t1.ReasonCode [Reason]
                                    from {db.WEBDB}..CN1 t0 with (nolock) Inner join 
                                        {db.WEBDB}..TrcnLineDetails t1  with (nolock) 
                                                    on t1.DocEntry = t0.DOCENTRY and t0.LINENUM = t1.LineNum
                                and t1.Module = 'DistBreadTrcn'
                                and t0.DOCENTRY = @CnEntry";

                var res = new SqlConnection(_commDbConnStr_bread)
                    .Query<Bread_CN1_Ext>(query, new { CnEntry = dto.CnDocEntry }).ToList();

                if (res.Count == 0) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadBread_RcnLines(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("subsi is empty");
                }
                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("doc entry empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }

                var query = $@"Select *
                               From {db.WEBDB}..FTAPP_RCN1 with (nolock)
                               Where DocEntry = @CnEntry";

                var res = new SqlConnection(_commDbConnStr_bread)
                    .Query<Bread_CN1_Ext>(query, new { CnEntry = dto.CnDocEntry }).ToList();

                if (res.Count == 0) return NotFound();
                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult LoadBread_Dist_DraftLines(Dto_BreadReturn dto)
        {
            // FTAPP_DIST_RCN1_DRAFT
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("subsi is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid usercode empty");
                }
                if (string.IsNullOrWhiteSpace(dto.DiCardCode))
                {
                    return BadRequest("invalid Di card code empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }

                var query = $@"Select *
                               From {db.WEBDB}..FTAPP_DIST_RCN1_DRAFT with (nolock)
                               Where UserCode = @UserCode and DiCardCode = @DiCardCode";

                var res = new SqlConnection(_commDbConnStr_bread)
                    .Query<Bread_CN1_Ext>(query, new { UserCode = dto.UserCode, DiCardCode = dto.DiCardCode }).ToList();

                if (res.Count == 0) return NotFound();

                res.ForEach(l =>
                {
                    if (l.LineGuid == default) l.LineGuid = Guid.NewGuid();
                });

                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }


        IActionResult LoadBread_DraftLines(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("subsi is empty");
                }
                if (dto.CnDocEntry < 0)
                {
                    return BadRequest("doc entry empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("dbi is empty");
                }

                var query = $@"Select *
                               From {db.WEBDB}..FTAPP_RCN1_DRAFT with (nolock)
                               Where DocEntry = @CnEntry";

                var res = new SqlConnection(_commDbConnStr_bread)
                    .Query<Bread_CN1_Ext>(query, new { CnEntry = dto.CnDocEntry }).ToList();

                if (res.Count == 0) return NotFound();

                res.ForEach(l =>
                   {
                       if (l.LineGuid == default) l.LineGuid = Guid.NewGuid();
                   });

                return Ok(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult QueryRetCnDocByDocEntry(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid SubSi");
                }
                if (dto.CnDocEntry <= 0)
                {
                    return BadRequest("invalid CN doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var sp_findCn = @"exec sp_QueryRetCnDocByDocEntry @webDb, @CnDocEntry";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var foundCn = conn.Query<Bread_CN_Ext>(sp_findCn, new
                {
                    webDb = db.WEBDB,
                    CnDocEntry = dto.CnDocEntry
                }).FirstOrDefault();

                if (foundCn == null) return NotFound();

                // get all it line
                var queryLine = @$"exec sp_GetBreadCnLines_S0 @webDb, @cnDocEntry";
                foundCn.Lines = conn.Query<Bread_CN1_Ext>(queryLine, new
                {
                    webDb = db.WEBDB,
                    cnDocEntry = foundCn.DOCENTRY
                }).ToList();

                //queryLine = @$"Select * from {db.WEBDB}..TrcnLineDetails with (nolock) where DocEntry =  @cnDocEntry";

                queryLine = $@"exec sp_GetBreadCnLines_S1 @webDb, @docEntry";
                foundCn.LineDetails = conn.Query<TrcnLineDetails>(queryLine, new
                {
                    webDb = db.WEBDB,
                    docEntry = foundCn.DOCENTRY
                }).ToList();

                return Ok(foundCn);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetReturnCns(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }
                //@webDb as nvarchar(120), 
                //@userCode as varchar(120),
                //@startDt as datetime, 
                //@endDt as datetime

                var sql = $@"exec Sp_GetReturnRCns @webDb, @userCode, @startDt, @endDt";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var cns = conn.Query<Bread_CN_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

                if (cns.Count == 0) return NotFound();
                return Ok(cns);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetReturnCns_Dist(Dto_BreadReturn dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("invalid user code");
                }
                if (dto.StartDt == default)
                {
                    return BadRequest("invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("invalid end date");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }
                //@webDb as nvarchar(120), 
                //@userCode as varchar(120),
                //@startDt as datetime, 
                //@endDt as datetime

                var sql = $@"exec Sp_GetReturnRCns_Dist @webDb, @userCode, @startDt, @endDt";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var cns = conn.Query<Bread_CN_Ext>(sql, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    startDt = dto.StartDt,
                    endDt = dto.EndDt
                }).ToList();

                if (cns.Count == 0) return NotFound();
                return Ok(cns);

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

    }
}

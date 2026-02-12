using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread.BreadCog;
using KTC_SalesAppWAPI.DTOs.COG;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.DiApi;
using KTC_SalesAppWAPI.Models.BreadReturn;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.Cdn;
using KTC_SalesAppWAPI.Models.COG;
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
    public class BreadCogController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<BreadCogController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";

        public BreadCogController(IConfiguration configuration, ILogger<BreadCogController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_BreadCog dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "VerifyBreadCogItemCode":
                    {
                        return VerifyBreadCogItemCode(dto);
                    }
                case "LoadReasonCode_Bread":
                    {
                        return LoadReasonCode_Bread(dto);
                    }
                case "Bread_CreateDirectCN": // seller create the cn 
                    {
                        return Bread_CreateDirectCN(dto);
                    }
                case "Bread_CreateDirectCN_Dist": // seller create the cn 
                    {
                        return Bread_CreateDirectCN_Dist(dto);
                    }
                case "UpdateCogSignDocfiles_Bread":
                    {
                        return UpdateCogSignDocfiles_Bread(dto);
                    }
                case "Load_Dist_CN":
                    {
                        return Load_Dist_CN(dto);
                    }
                case "Load_Dist_INV":
                    {
                        return Load_Dist_INV(dto);
                    }
                case "LoadCns":
                    {
                        return LoadCns(dto);
                    }
                case "Load_BreadCnLines":
                    {
                        return Load_BreadCnLines(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }


        IActionResult Load_BreadCnLines(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }

                if (dto.DocEntry <= 0)
                {
                    return BadRequest("invalid doc entry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db infor");
                }

                var sp = @"exec sp_GetBreadCnLines @webDb, @cnDocEntry";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var lines = conn.Query<Bread_CN1_Ext>(sp, new
                {
                    webDb = db.WEBDB,
                    cnDocEntry = dto.DocEntry
                }).ToList();

                if (lines.Count == 0) return NotFound();
                return Ok(lines);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadCns(Dto_BreadCog dto)
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
                if (string.IsNullOrWhiteSpace(dto.UserType))
                {
                    return BadRequest("Invalid user type");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var sql = dto.UserType == "DI" ? $@"exec sp_GetCns_Dist @webDb, @userCode, @startDt, @endDt" :
                                                 $@"exec sp_GetCns_Seller  @webDb, @userCode, @startDt, @endDt";

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


        IActionResult Load_Dist_INV(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (dto.DocEntry <= 0)
                {
                    return BadRequest("invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var query = @$"select * from {db.WEBDB}..INV with (nolock) Where docentry = @docEntry";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var inv = conn.Query<Bread_CN_Ext>(query, new
                {
                    docEntry = dto.DocEntry
                }).FirstOrDefault();

                if (inv == null) return NotFound();

                // load it line 
                query = @$"select * from {db.WEBDB}..INV1 with (nolock) Where docentry = @docEntry";
                inv.Lines = conn.Query<Bread_CN1_Ext>(query, new
                {
                    docEntry = dto.DocEntry
                }).ToList();

                for (int i = 0; i < inv.Lines.Count; i++)
                {

                    var line = inv.Lines[i];
                    if (line == null) continue;

                    var sp_query = $@"select batchNO, Quantity from {db.WEBDB}..INV3 
                                       Where docentry = @docEntry 
                                       and LINENUM = @lineNum  ";

                    inv.Lines[i].Batches = conn.Query<Bread_Batch>(sp_query, new
                    {
                        docEntry = line.DOCENTRY,
                        lineNum = line.LINENUM
                    }).ToList();
                }

                return Ok(inv);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Load_Dist_CN(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                if (dto.DocEntry <= 0)
                {
                    return BadRequest("invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("invalid db info");
                }

                var query = @$"select * from {db.WEBDB}..CN with (nolock) Where docentry = @docEntry";
                var conn = new SqlConnection(_commDbConnStr_bread);
                var cn = conn.Query<Bread_CN_Ext>(query, new
                {
                    docEntry = dto.DocEntry
                }).FirstOrDefault();

                if (cn == null) return NotFound();

                // load it line 
                query = @$"select * from {db.WEBDB}..CN1 with (nolock) Where docentry = @docEntry";
                cn.Lines = conn.Query<Bread_CN1_Ext>(query, new
                {
                    docEntry = dto.DocEntry
                }).ToList();

                // load the cn batch information 
                for (int i = 0; i < cn.Lines.Count; i++)
                {
                    var line = cn.Lines[i];
                    if (line == null) continue;

                    var sp_query = $@"select batchNO, Quantity 
                                        From {db.WEBDB}..CN3 
                                       Where docentry = @docEntry 
                                       and LINENUM = @lineNum  ";

                    cn.Lines[i].Batches = conn.Query<Bread_Batch>(sp_query, new
                    {
                        docEntry = line.DOCENTRY,
                        lineNum = line.LINENUM
                    }).ToList();
                }

                return Ok(cn);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult UpdateCogSignDocfiles_Bread(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid docentry");
                }
                if (string.IsNullOrWhiteSpace(dto.DocType))
                {
                    return BadRequest("Invalid doc type");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db infor");
                }

                var propQuery = $@"select * 
                                   from {db.WEBDB}..FPROP with (nolock) 
                                   where DocEntry = @docEntry
                                          and DocType = @docType
                                   order by Id desc ";

                var conn = new SqlConnection(_commDbConnStr_bread);
                var prop = conn.Query<FPROP>(propQuery,
                    new
                    {
                        docEntry = dto.DocEntry,
                        docType = dto.DocType
                    }).FirstOrDefault(); // always take the last file

                if (prop == null)
                {
                    return BadRequest("attached file no found, please try again");
                }

                // select the object type
                SAPbobsCOM.BoObjectTypes sapDocType = SAPbobsCOM.BoObjectTypes.oCreditNotes;
                string portalTabName = "CN";
                switch (dto.DocType)
                {
                    case "OINV":
                        {
                            portalTabName = "INV";
                            sapDocType = SAPbobsCOM.BoObjectTypes.oInvoices;
                            break;
                        }
                    case "INV":
                        {
                            portalTabName = "INV";
                            break;
                        }
                    case "ORIN":
                        {
                            portalTabName = "CN";
                            sapDocType = SAPbobsCOM.BoObjectTypes.oCreditNotes;
                            break;
                        }
                }

                // portal invoice , distributor
                if ($"{dto.DocType}".ToUpper() == "INV" || $"{dto.DocType}".ToUpper() == "CN")
                {
                    // update the portal inv will do 
                    // update the cn or INV table for the file 
                    var query = $@"Select FILES 
                                    from {db.WEBDB}..{portalTabName} with (nolock) 
                                    where DocEntry = @num ";

                    var docfiles = conn.ExecuteScalar<string>(query, new
                    {
                        num = dto.DocEntry
                    });

                    var fileInfo = new FileInfo(prop.FilePath);
                    var fileName = fileInfo.Name;

                    // add up the files
                    if (!string.IsNullOrWhiteSpace(docfiles))
                    {
                        docfiles += "," + fileName;
                    }
                    else
                    {
                        docfiles = fileName;
                    }

                    var updateSql = $@"update {db.WEBDB}..{portalTabName} 
                                       set FILES = @docfiles 
                                       where DocEntry = @num";

                    var updateResult = conn.Execute(updateSql, new
                    {
                        docfiles = docfiles,
                        num = dto.DocEntry
                    });

                    return Ok();
                }


                // seller and dist ktc store
                // query sap docentry 
                var getDocEntry = $@"select DocEntry
                                     from {db.SAPDB}..{dto.DocType} with (nolock) 
                                     where U_SOENTRY = @DocEntry";

                var sapDocEntry = conn.ExecuteScalar<int>(getDocEntry, new { DocEntry = dto.DocEntry });

                if (sapDocEntry <= 0)
                {
                    return BadRequest("unable to get doc entry from SAP query");
                }

                var diHelper = new BreadDiApi_Delivery(db);
                var res = diHelper.Add_FileAttachment(sapDocEntry, prop.FilePath, sapDocType);

                if (string.IsNullOrWhiteSpace(res))
                {
                    var fileInfo = new FileInfo(prop.FilePath);
                    var fileName = fileInfo.Name;

                    // update the cn or INV table for the file 
                    var query = $@"Select FILES 
                                    from {db.WEBDB}..{portalTabName} 
                                    with (nolock) where DocEntry = @num";

                    var docfiles = conn.ExecuteScalar<string>(query, new
                    {
                        num = dto.DocEntry
                    });

                    // add up the files
                    if (!string.IsNullOrWhiteSpace(docfiles))
                    {
                        docfiles += "," + fileName;
                    }
                    else
                    {
                        docfiles = fileName;
                    }

                    // update the target file 
                    var updateSql = $@"update {db.WEBDB}..{portalTabName} 
                                       set FILES = @docfiles 
                                       where DocEntry = @num";

                    var updateResult = conn.Execute(updateSql, new
                    {
                        docfiles = docfiles,
                        num = dto.DocEntry
                    });

                    // update the fprop column                   
                    var updateFprop = @$"Update {db.WEBDB}..FPROP 
                                        set UpdatedSap = 'Y', UpdatedDt = GETDATE() 
                                        where Id = @id ";

                    conn.Execute(updateFprop, new { id = prop.Id });
                    return Ok();
                }

                return BadRequest(res);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult Bread_CreateDirectCN_Dist(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.CnDoc == null)
                {
                    return BadRequest("Invalid cn doc head");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid doc update type");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.Line == null)
                {
                    return BadRequest("Invalid logger information");
                }

                if (string.IsNullOrWhiteSpace(dto.IsKtcStore))
                {
                    return BadRequest("Invalid store infor");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // portal cn  ?
                // cn ktc store -> invoice dist
                long cn_docEntry = -1;
                var bread_CnDocHelper = new Bread_CNDocHelper();
                if (dto.CnDoc.DOCENTRY == 0)
                {
                    cn_docEntry = bread_CnDocHelper.CreateBread_CN(dto.CnDoc, db, _commDbConnStr_bread);
                }
                else
                {
                    cn_docEntry = bread_CnDocHelper.UpdateDraft_CN(dto.CnDoc, db, _commDbConnStr_bread);
                }

                // save draft done
                if ($"{dto.UpdateType}".ToLower() == "draft")
                {
                    // create sap cn 
                    var draft_rep = new BreadDocReplied
                    {
                        DocEntry = cn_docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };
                    return Ok(draft_rep);
                }

                var conn = new SqlConnection(_commDbConnStr_bread);
                if (dto.IsKtcStore == "Y") // handler create sap cn to store, create invoice to dist and create portal invoice to dist
                {
                    // else create SAP Cn KTC store , then invoice Distributor in SAP
                    // create SAP CN then KTC store invoice                    
                    var errorMsg = HandlerDistCreate_Inv_KStore_CN(int.Parse($"{cn_docEntry}"), db, conn);
                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        _logger.LogError(errorMsg);
                        return BadRequest(errorMsg);
                    }

                    // replied the success create of the inv doc
                    // but wait posting to SAP
                    var sql = $@"select t0.DocNum 
                                    from {db.SAPDB}..ORIN t0 with (nolock)
                                    inner join 
                                        {db.WEBDB}..CN t1 with (nolock) on t1.CNENTRY = t0.DocEntry 
                                    Where t1.DOCENTRY = @docentry";

                    var cnNum = conn.ExecuteScalar<string>(sql, new { docentry = cn_docEntry });
                    var replied = new BreadDocReplied
                    {
                        DocEntry = cn_docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = cnNum
                    };
                    return Ok(replied);
                }

                #region Distributor own store cn  
                var updateDocStatusQuery = @$"update {db.WEBDB}..CN set DOCSTATUS = 'C' Where DocEntry = @docEntry";
                conn.Execute(updateDocStatusQuery, new { docEntry = cn_docEntry });

                // replied app for doc submitted
                var sql3 = $@"select t0.DocNum 
                            from  {db.WEBDB}..CN t0 with (nolock)                                    
                            Where t0.DOCENTRY = @docentry";
                var cnno = conn.ExecuteScalar<string>(sql3, new { docentry = cn_docEntry });

                // create sap cn 
                var created_replied = new BreadDocReplied
                {
                    DocEntry = cn_docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = cnno
                };

                return Ok(created_replied);
                #endregion
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        string HandlerDistCreate_Inv_KStore_CN(int docEntry, DbInfo db, SqlConnection conn)
        {
            try
            {
                LastError = "";
                // parepare the invoice data table 
                var query = $@"SELECT T0.*
                                     ,T1.CARDCODE AS [SAPCARDCODE]
                                     ,T2.CNARINVSERIES AS [INVSERIES]
                                     ,T2.CNARCNSERIES AS [CNSERIES]
                                     ,CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE 
                                           ELSE U0.DEFWHS END AS [WHSCODE]
                                     , T3.SERIESNAME
                                     , T4.PRCCODE AS [DIM1] 
                                     , T0.FILES [FILES]   
                                     , U0.SLPCODE [SLPCODE] 
                                FROM {db.WEBDB}..CN T0 INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T1 ON 
                                                T1.CARDCODE = T0.CARDCODE 
                                            AND T1.CARDTYPE = 'C' 
                                            AND T1.FrozenFor = 'N' 
                                LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 ON T2.RECID = 1 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[NNM1] T3 ON 
                                                    T3.SERIES = T2.CNARCNSERIES 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T4 ON 
                                                    T4.PRCCODE = T1.U_COSTCTR 
                                                AND T4.DimCode = '1' 
                                LEFT OUTER JOIN {db.WEBDB}..USERS U0 ON 
                                                    U0.USERID = T0.UMODIFIED 
                                WHERE T0.DOCENTRY = '{docEntry}'";

                var cn_dt = GetDataTable(conn, query);
                if (cn_dt == null)
                {
                    return LastError;
                }

                var sapcard = cn_dt.Rows[0]["SAPCARDCODE"].ToString();
                var priceList = db.DEF_PRICELIST;

                query = $@"SELECT T1.*
                                , T1.PRICE AS[CUSTPRICE]
                                , T5.Price AS[SUPPLIERPRICE]
                                , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
                                        ELSE T6.U_CSUS_UOM END AS [CSUOM]
                                , ISNULL(T7.GLCODE,'') AS [INGL]
                                , T8.PRCCODE AS [DIM2]  
                                , ISNULL(ManBtchNum, 'N') [MANBTCHNUM]
                                , T9.ReasonCode [U_CSUS_RC]
                            FROM {db.WEBDB}..CN T0 
                                INNER JOIN {db.WEBDB}..CN1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
                                INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T2 ON T2.U_PORTALID = T0.CARDCODE 
                                    AND T2.CARDTYPE = 'C' 
                                    AND T2.FrozenFor = 'N' 
                                    AND T2.CardCode = '{sapcard}'
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
                                    AND T4.PriceList = T2.ListNum 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
                                    AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum ELSE '{priceList}' END 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
                            LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T8 ON T8.PRCCODE = T6.CardCode 
                                    AND T8.DimCode = '2' 
                            LEFT OUTER JOIN [{db.WEBDB}]..TrcnLineDetails T9 ON T9.DOCENTRY = T1.DOCENTRY
                                    AND T9.LINENUM = T1.LINENUM 
                                    AND T9.MODULE = 'BreadTrcn'
                                                                           
                            WHERE T0.DOCENTRY ='{docEntry}'";

                var cn1_dt = GetDataTable(conn, query);
                if (cn1_dt == null)
                {
                    return LastError;
                }

                var diapiHelper = new BreadDiApi_Trade(db, _localAttchPath);
                return diapiHelper.createCNInv($"{docEntry}", cn_dt, cn1_dt, false); // just create invoice , no cn
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return LastError;
            }
        }

        IActionResult Bread_CreateDirectCN(Dto_BreadCog dto)
        {
            try
            {
                // 20240816 
                // add in memory control 
                // check the user in dlb creation 
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    goto ByPassTransCheck;
                    //return BadRequest("bad user login, please log out app, " +
                    //    "and login again to refresh login token. Thanks");
                }

                // check the memory for the key exist
                if (Program.UserTransToken_BreadCreateCn == null)
                    Program.UserTransToken_BreadCreateCn = new Dictionary<string, bool>();

                // check user token in list
                var isListed = Program.UserTransToken_BreadCreateCn.ContainsKey(dto.UserCode);

                if (isListed) // yes in 
                {
                    bool inTran = Program.UserTransToken_BreadCreateCn[dto.UserCode];
                    if (inTran)
                    {
                        return BadRequest("Creation in process, please wait for moment. Thanks.");
                    }
                    else
                    {
                        Program.UserTransToken_BreadCreateCn[dto.UserCode] = true;
                    }
                }
                else // no then add in and set true 
                {
                    Program.UserTransToken_BreadCreateCn.Add(dto.UserCode, true); // add and set to intrans
                }

            ByPassTransCheck:

                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (dto.CnDoc == null)
                {
                    return BadRequest("Invalid cn doc head");
                }
                if (string.IsNullOrWhiteSpace(dto.UpdateType))
                {
                    return BadRequest("Invalid doc update type");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid SubSi");
                }
                if (dto.Line == null)
                {
                    return BadRequest("Invalid logger information");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                // handler update or create new CN
                long docEntry = -1;
                var breadDocHelper = new Bread_CNDocHelper();
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

                // update the transport code if is distributor return 
                var conn = new SqlConnection(_commDbConnStr_bread);
                if ($"{dto.UserType}".ToLower() == "tr")
                {
                    var updateTrnsCode = @$"Update {db.WEBDB}..CN Set TransporterCode = @userCode 
                                        Where DOCENTRY = @docEntry";
                    var updateRes = conn.Execute(updateTrnsCode, new { userCode = dto.UserCode, docEntry = docEntry });
                }

                // save draft done
                if ($"{dto.UpdateType}".ToLower() == "draft")
                {
                    // create sap cn 
                    var replied = new BreadDocReplied
                    {
                        DocEntry = docEntry,
                        IsSuccess = true,
                        LastErrorMessage = "",
                        DocNum = ""
                    };
                    return Ok(replied);
                }

                // create SAP CN 
                var seriesName = "";
                var errorMsg = HandlerCreate_RetCN(conn, db, docEntry, out seriesName);

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    _logger.LogError($"Doc# {docEntry} saved draft,\n" + errorMsg);
                    return BadRequest($"Doc# {docEntry} saved draft,\n" + errorMsg);
                }

                BreadInvCnChecker.CommDbConnStr_Bread = _commDbConnStr_bread;
                var foundCn = BreadInvCnChecker.GetPostedCN(db, docEntry);
                var foundInv = BreadInvCnChecker.GetPostedInv(db, docEntry);

                if (foundCn == null)
                {
                    // if cn no created 
                    // reset the inv doc to draft and 
                    var update_cn = @$"Update {db.WEBDB}..CN 
                                        set DOCSTATUS = 'D'
                                            , DOCNUM = ''
                                            , SAPINV = null 
                                            , INVENTRY = null 
                                            , CNENTRY = null 
                                        WHERE DOCENTRY = @docEntry;";

                    var upd = conn.Execute(update_cn, new { docEntry = docEntry });
                    return BadRequest("Server busy, please try submit again. doc save as draft Thanks. [E963CN]");
                }


                var sapInvEntry = foundInv == null ? "" : foundInv.DocEntry.ToString();

                // reupdate the cn CM Entry again 
                var update_cn1 = @$"Update {db.WEBDB}..CN 
                                        set DOCSTATUS = 'C'
                                            , DOCNUM = @portalDocNum
                                            , SAPINV = 'Y'  
                                            , CNENTRY = @sapCnDocEntry 
                                            , INVENTRy = @sapInvEntry
                                        WHERE DOCENTRY = @docEntry;";

                var newConn = new SqlConnection(_commDbConnStr_bread);
                var updateRes1 = newConn.Execute(update_cn1, new
                {
                    portalDocNum = $"{seriesName}{foundCn.DocNum}",
                    sapCnDocEntry = foundCn.DocEntry,
                    sapInvEntry = sapInvEntry,
                    docEntry = docEntry,
                });

                // create sap cn 
                var replied3 = new BreadDocReplied
                {
                    DocEntry = docEntry,
                    IsSuccess = true,
                    LastErrorMessage = "",
                    DocNum = $"{foundCn.DocNum}"
                };

                return Ok(replied3);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(dto.UserCode) && Program.UserTransToken_BreadCreateCn.Count > 0)
                {
                    Program.UserTransToken_BreadCreateCn.Remove(dto.UserCode);
                }
            }
        }     

        // seller create return cn for KTC store
        string HandlerCreate_RetCN(SqlConnection conn, DbInfo db, long docEntry, out string seriesName)
        {
            LastError = "";
            seriesName = "";
            try
            {
                // parepare the invoice data table 
                var query = $@"SELECT T0.*
                                     ,T1.CARDCODE AS [SAPCARDCODE]
                                     ,T2.CNARINVSERIES AS [INVSERIES]
                                     ,T2.CNARCNSERIES AS [CNSERIES]
                                     ,CASE WHEN ISNULL(U0.DEFWHS,'') = '' THEN T2.WHSCODE 
                                           ELSE U0.DEFWHS END AS [WHSCODE]
                                     , T3.SERIESNAME
                                     , T4.PRCCODE AS [DIM1] 
                                     , T0.FILES [FILES]   
                                     , U0.SLPCODE [SLPCODE]
                                FROM {db.WEBDB}..CN T0 
                                INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T1 ON 
                                                T1.CARDCODE = T0.CARDCODE 
                                            AND T1.CARDTYPE = 'C' 
                                            AND T1.FrozenFor = 'N' 
                                LEFT OUTER JOIN {db.WEBDB}..SAPREC T2 ON T2.RECID = 1 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[NNM1] T3 ON 
                                                    T3.SERIES = T2.CNARCNSERIES 
                                LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T4 ON 
                                                    T4.PRCCODE = T1.U_COSTCTR 
                                                AND T4.DimCode = '1' 
                                LEFT OUTER JOIN {db.WEBDB}..USERS U0 ON 
                                                    U0.USERID = T0.UMODIFIED 
                                WHERE T0.DOCENTRY = '{docEntry}'";

                var cn_dt = GetDataTable(conn, query);
                if (cn_dt == null)
                {
                    return LastError;
                }

                seriesName = $"{cn_dt.Rows[0]["SERIESNAME"]}";

                var sapcard = cn_dt.Rows[0]["SAPCARDCODE"].ToString();
                var priceList = db.DEF_PRICELIST;

                query = $@"SELECT T1.*
                                , T1.PRICE AS[CUSTPRICE]
                                , T5.Price AS[SUPPLIERPRICE]
                                , CASE WHEN ISNULL(T6.U_CSUS_UOM,0) = 0 THEN 1 
                                        ELSE T6.U_CSUS_UOM END AS [CSUOM]
                                , ISNULL(T7.GLCODE,'') AS [INGL]
                                , T8.PRCCODE AS [DIM2]  
                                , ISNULL(ManBtchNum, 'N') [MANBTCHNUM]
                                , T9.ReasonCode [U_CSUS_RC]
                            FROM {db.WEBDB}..CN T0 
                                INNER JOIN {db.WEBDB}..CN1 T1 ON T1.DOCENTRY = T0.DOCENTRY 
                                INNER JOIN [{db.SAPDB}].[DBO].[OCRD] T2 ON ISNULL(T2.U_PORTALID, T0.CardCode) = T0.CARDCODE 
                                    AND T2.CARDTYPE = 'C' 
                                    AND T2.FrozenFor = 'N' 
                                    AND T2.CardCode = '{sapcard}'
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OCRD] T3 ON T3.CardCode = T0.COMPANYID 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T4 ON T4.ItemCode = T1.ITEMCODE 
                                    AND T4.PriceList = T2.ListNum 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[ITM1] T5 ON T5.ItemCode = T1.ITEMCODE 
                                    AND T5.PriceList = CASE WHEN '{priceList}' = 0 THEN T3.ListNum ELSE '{priceList}' END 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OITM] T6 ON T6.ItemCode = T1.ITEMCODE 
                            LEFT OUTER JOIN {db.WEBDB}..ITEMMASTER T7 ON T7.ITEMCODE = T1.ITEMCODE 
                            LEFT OUTER JOIN [{db.SAPDB}].[DBO].[OPRC] T8 ON T8.PRCCODE = T6.CardCode 
                                    AND T8.DimCode = '2' 
                            LEFT OUTER JOIN [{db.WEBDB}]..[TrcnLineDetails] T9 ON T9.DOCENTRY = T1.DOCENTRY
                                    AND T9.LineNum = T1.LINENUM

                            WHERE T0.DOCENTRY ='{docEntry}'";

                var cn1_dt = GetDataTable(conn, query);
                if (cn1_dt == null)
                {
                    return LastError;
                }

                var diapiHelper = new BreadDiApi_Trade(db, _localAttchPath);
                var error = diapiHelper.createCNInv($"{docEntry}", cn_dt, cn1_dt, true); // just create cn

                return error;
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

        IActionResult LoadReasonCode_Bread(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("db info query error, or invalid subsi");
                }

                var sql = "exec sp_SelectReasonCode_Bread @webDb";

                var conn = new SqlConnection(_commDbConnStr_bread);
                var res = conn.Query<ReasonCode>(sql,
                    new
                    {
                        webDb = db.WEBDB
                    }).ToList();

                if (res != null && res.Count > 0) return Ok(res);
                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }

        IActionResult VerifyBreadCogItemCode(Dto_BreadCog dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    return BadRequest("The card code invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    return BadRequest("The query code is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("The usercode is empty");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_bread, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid company name as info");
                }

                var conn = new SqlConnection(_commDbConnStr_bread); // open the db connection

                var checkCode = dto.Code.TrimStart('0');
                OITM_Ext item = null;

                // 1st try exact item code 
                var sq_item = @"exec sp_BreadCog_QueryOItm_ExactIC @erpDb, @code";
                OITM_Ext firstTry = conn.Query<OITM_Ext>(sq_item, new
                {
                    erpDb = db.SAPDB,
                    code = checkCode
                }).FirstOrDefault();

                if (firstTry != null)
                {
                    item = firstTry;
                    goto processNext;
                }

                // try query with like statement
                sq_item = @"exec sp_BreadCog_QueryOItm  @erpDb, @code";
                var items = conn.Query<OITM_Ext>(sq_item, new
                {
                    erpDb = db.SAPDB,
                    code = checkCode
                }).ToList();

                if (items.Count == 0)
                {
                    return BadRequest($"Scan code: {dto.Code}\nNot found in current setup.");
                }

                if (items.Count > 1)
                {
                    // return the list for user selection
                    var newDto1 = new Dto_TrcnItem
                    {
                        Message = "MultipleItemFound",
                        Items = items,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto1);
                }

                if (items.Count == 1)
                {
                    item = items[0];
                }

                // condition for item checking 
                // check the item valid in sap 
                if (item == null)
                {
                    return BadRequest($"Scan code: {dto.Code}\nNot found in current setup.");
                }

            processNext:

                if (item.frozenFor == "Y")
                {
                    var message = $"Scan code: {dto.Code}\n" +
                                 $"For {item.ItemCode}\n" +
                                 $"{item.ItemName}\nwas set FROZEN in system.\n\nNo return allowed.";

                    var availItems = GetAvailableItems(db);
                    if (availItems.Count == 0)
                    {
                        return BadRequest(message);
                    }

                    var newDto3 = new Dto_TrcnItem
                    {
                        Message = message,
                        Items = availItems,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto3);
                }

                if (item.validFor == "N")
                {
                    var message = $"Scan code: {dto.Code}\n" +
                                 $"for {item.ItemCode}\n" +
                                 $"{item.ItemName}\nwas set INVALID in system.\n\nNo return allowed.";

                    var availItems = GetAvailableItems(db);
                    if (availItems.Count == 0)
                    {
                        return BadRequest(message);
                    }

                    var newDto2 = new Dto_TrcnItem
                    {
                        Message = message,
                        Items = availItems,
                        IsSuccess = false,
                        Item = null
                    };
                    return Ok(newDto2);
                }

                // get the current price list
                var sq_lastOrder = @"exec sp_BreadCog_QueryCurrentItem @erpDb, @itemCode, @cardCode";
                var itemOrder = conn.Query<CogItem>(sq_lastOrder, new
                {
                    erpDb = db.SAPDB,
                    itemCode = item.ItemCode,
                    cardCode = dto.CardCode
                }).FirstOrDefault();

                if (itemOrder == null)
                {
                    // query the non ktcw store
                    sq_lastOrder = @"exec sp_BreadCog_QueryCurrentItem_NonKtcStore @webDb, @itemCode, @cardCode";
                    itemOrder = conn.Query<CogItem>(sq_lastOrder, new
                    {
                        webDb = db.WEBDB,
                        itemCode = item.ItemCode,
                        cardCode = dto.CardCode
                    }).FirstOrDefault();

                    if (itemOrder == null)
                    {
                        var message = $"Scan code: {dto.Code}\n" +
                             $"for {item.ItemCode}\n" +
                             $"{item.ItemName}\n having zero price list setup for Store: {dto.CardCode} " +
                             $"in system.\n\nNo return allowed.";
                        return BadRequest(message);
                    }
                }

                var newDto = new Dto_TrcnItem
                {
                    Message = "CurrentPriceList",
                    Items = null,
                    IsSuccess = true,
                    Item = itemOrder
                };

                return Ok(newDto); // return based on current price list
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        List<OITM_Ext> GetAvailableItems(DbInfo db)
        {
            try
            {
                var sql = $@"SELECT * 
                            FROM {db.SAPDB}..OITM with (nolock)
                            WHERE INVNTITEM = 'Y' 
                                AND ISNULL(U_NONSTK,'') = 'Y' 
                                AND SellItem = 'Y' 
                                AND FROZENFOR = 'N'";

                return new SqlConnection(_commDbConnStr).Query<OITM_Ext>(sql).ToList();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return null;
            }
        }
    }
}

using Dapper;
using KTC_SalesAppWAPI.DTOs.IBTReceipt;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.IBTReceipt;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Pick_IBT;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.IBT
{
    [Route("[controller]")]
    [ApiController]
    public class IBTReceiptController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<IBTReceiptController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";
        string _webHostAddrEndPoint = "";

        public IBTReceiptController(IConfiguration configuration, ILogger<IBTReceiptController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
            _webHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;

        }
        [HttpPost]
        public IActionResult PostAsync(DTO_IBTReceipt dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "GetListTransferedIBT": // base RIB 
                    {
                        return GetListTransferedIBT(dto);
                    }
                case "GetTransferDocAndLines":
                    {
                        return GetTransferDocAndLines(dto);
                    }
                case "GetIBTItems":
                    {
                        return GetIBTItems(dto);
                    }
                case "ReceiptIBT":
                    {
                        return ReceiptIBT(dto);
                    }
                case "GetTransferDocAndLines_ByRib":
                    {
                        return GetTransferDocAndLines_ByRib(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult GetTransferDocAndLines_ByRib(DTO_IBTReceipt dto)
        {
            try
            {
                if (dto.RibDocEntry < 0)
                {
                    return BadRequest("invalid RIB doc entry");
                }

                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid db1");
                }

                var ribDoc_query = @$"Select t1.IBTDocEntry,  t0.* 
                                    from {db.WEBDB}..RIB  t0 
                                    inner join {db.WEBDB}..FTAPP_IBTRIB_Link t1 on t1.RIBDocEntry = t0.DocEntry
                                    Where DocEntry = @ribEntry";

                using var conn = new SqlConnection(_commDbConnStr);
                var ribDoc = conn.Query<RIB>(ribDoc_query, new { ribEntry = dto.RibDocEntry }).FirstOrDefault();
                if (ribDoc == null)
                {
                    return BadRequest($"RIB #{dto.RibDocEntry} was no found, please try again later. Thanks");
                }


                // query the IBT head 
                // get the head                
                var sp_query = @"exec sp_GetTransferedIBTViaRIB @webDb, @transitNoEntry";
                var ibtHead = conn.Query<KTC_SalesAppWAPI.Models.Pick_IBT.IBT>(sp_query, new
                {
                    webDb = db.WEBDB,
                    transitNoEntry = ribDoc.IBTDocEntry
                }).FirstOrDefault();

                if (ibtHead == null)
                {
                    return BadRequest($"Invalid IBT doc num / or IBT #{dto.TransferDocNum} no found" +
                        $"\nin {dto.Subsi}, please try again later.");
                }

                // get the line 
                var sp_query_line = @"exec sp_GetTransferedIBTLines @webDb, @ibtDocEntry ";
                var ibtLines = conn.Query<IBT1>(sp_query_line, new
                {
                    webDb = db.WEBDB,
                    ibtDocEntry = ibtHead.DOCENTRY
                }).ToList();

                if (ibtLines.Count == 0)
                {
                    return BadRequest($"Invalid IBT doc num / or IBT #{dto.TransferDocNum} lines no found" +
                        $"\nin {dto.Subsi}, please try again later.");
                }

                ibtHead.Lines = new List<IBT1>(ibtLines);

                // chec any batch 
                for (int i = 0; i < ibtHead.Lines.Count; i++)
                {
                    var line = ibtHead.Lines[i];
                    if (line == null) continue;

                    //load in the variety or barcode 
                    var barcode_sql = $@"SELECT * 
                                 FROM {db.SAPDB}..OBCD WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var barcodes = conn.Query<OBCD_Ext>(barcode_sql, new { itemcode = line.ITEMCODE }).ToList();
                    if (barcodes.Count > 0)
                    {
                        ibtHead.Lines[i].BarCodes = new List<OBCD_Ext>(barcodes);
                    }

                    // for batch processing
                    if ($"{line.ManBtchNum}" == "Y")
                    {
                        var sp_queryBatch = @" exec sp_GetTransferedIBTLinesBatches @webDb, @ibtEntry, @lineNum ";
                        var batches = conn.Query<IBT2>(sp_queryBatch, new
                        {
                            webDb = db.WEBDB,
                            ibtEntry = line.DOCENTRY,
                            lineNum = line.LINENUM
                        }).ToList();

                        if (batches.Count == 0) continue;

                        ibtHead.Lines[i].Batches = new List<IBT2>(batches);
                    }
                }

                // load in the rib line
                var query_riblines = @$"Select t1.ManBtchNum [ManBtchNum], t0.* 
                                        from {db.WEBDB}..RIB1 t0 
                                        inner join {db.SAPDB}..OITM t1 on t1.ItemCode = t0.ItemCode
                                        Where t0.docentry = @docentry ";

                var ribLines = conn.Query<RIB1>(query_riblines, new { docentry = ribDoc.DOCENTRY }).ToList();

                // massage the line with batch if any 
                var newLines = new List<RIB1>();
                for (int i = 0; i < ribLines.Count; i++)
                {
                    var line = ribLines[i];
                    if (line == null) continue;

                    if (line.ManBtchNum != "Y")
                    {
                        if (ibtHead.Lines != null)
                        {
                            var foundOrigLine = ibtHead.Lines.Where(x => x.ITEMCODE == line.ITEMCODE).FirstOrDefault();
                            if (foundOrigLine != null)
                            {
                                line.ReceiptQty = line.QUANTITY;
                                line.ReceiptQtyCs = line.QUANTITYCS;
                                line.ReceiptQtyPc = line.QUANTITYPC;

                                line.QUANTITY = foundOrigLine.QTY1;
                                line.QUANTITYCS = Math.Floor(foundOrigLine.QTY1 / foundOrigLine.UOMQTY);
                                line.QUANTITYPC = foundOrigLine.QTY1;
                            }
                        }

                        newLines.Add(line); // add inot the replied line
                        continue;
                    }
                    // handler batch
                    // load the batch from rib 2 
                    var query_line_batches = @$"Select * 
                                                from {db.WEBDB}..RIB2 Where docentry = @docentry 
                                                and linenum = @linenum ";

                    var lineBatches = conn.Query<RIB2>(query_line_batches, new
                    {
                        docentry = line.DOCENTRY,
                        linenum = line.LINENUM
                    }).ToList();

                    if (lineBatches.Count == 0) continue;

                    // loop the batch and craete the line for eahc batch
                    // subdive a line into several line by atch and it qty 
                    for (int y = 0; y < lineBatches.Count; y++)
                    {
                        var batch = lineBatches[y];
                        if (batch == null) continue;

                        var newCloneLine = (RIB1)line.Clone();


                        newCloneLine.ReceiptQty = batch.QUANTITY;
                        newCloneLine.ReceiptQtyCs = Math.Floor(batch.QUANTITY / line.UOMQTY);
                        newCloneLine.ReceiptQtyPc = batch.QUANTITY;

                        newCloneLine.LOTNO = batch.BATCHNO;
                        newCloneLine.EXPDATE = batch.EXPDATE;
                        newCloneLine.MFRDATE = batch.MFRDATE;

                        if (ibtHead.Lines != null)
                        {
                            var foundOrigLine = ibtHead.Lines.Where(x => x.ITEMCODE == line.ITEMCODE).FirstOrDefault();
                            if (foundOrigLine != null)
                            {


                                newCloneLine.QUANTITY = foundOrigLine.QTY1;
                                newCloneLine.QUANTITYCS = Math.Floor(foundOrigLine.QTY1 / foundOrigLine.UOMQTY);
                                newCloneLine.QUANTITYPC = foundOrigLine.QTY1;
                            }
                        }

                        newLines.Add(newCloneLine); // add inot the replied line
                    }
                }


                var dtoreplied = new
                {
                    RibLines = newLines,
                    IbtHead = ibtHead
                };

                return Ok(dtoreplied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ReceiptIBT(DTO_IBTReceipt dto)
        {
            try
            {
                if (dto.IBTReceipt == null)
                {
                    return BadRequest("Invalid IBT receipt head");
                }
                if (dto.IBTReceipt.Lines == null)
                {
                    return BadRequest("Invalid IBT receipt lines");
                }
                if (dto.IBTReceipt.Lines.Count == 0)
                {
                    return BadRequest("Invalid IBT receipt lines, zero");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid QueryKeys");
                }
                if (dto.ReqDocNum < 0)
                {
                    return BadRequest("Invalid request doc num");
                }
                if (dto.IbtEntry < 0)
                {
                    return BadRequest("Invalid ibt docentry");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var qp_query = @$"Select * from {db.SAPDB}..OWTQ where DocNum =  @docnum";
                using var conn = new SqlConnection(_commDbConnStr);
                var reqdoc = conn.Query<OWTQ_Ext>(qp_query, new { docnum = dto.ReqDocNum }).FirstOrDefault();
                if (reqdoc == null)
                {
                    return BadRequest("Invalid request document load, please try again later. Thanks");
                }

                if (reqdoc.DocStatus == "C")
                {
                    return BadRequest(@$"{db.COMPANYNAME}\n
                                        Transfer request doc {dto.ReqDocNum} closes, new post not allowed");
                }

                // update the line 
                for (int l = 0; l < dto.IBTReceipt.Lines.Count; l++)
                {
                    dto.IBTReceipt.Lines[l].BASEENTRY = reqdoc.DocEntry;                   
                }

                dto.IBTReceipt.POSTENTRY = reqdoc.DocEntry;
                dto.IBTReceipt.POSTDATE = reqdoc.DocDate;

                // http://10.0.0.12:82/Receiveibt/Z15/POST
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : _webHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}Receiveibt/{db.COMPANYID}/POST");

                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(dto.IBTReceipt);

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                // when success
                if (response.IsSuccessful)
                {
                    var replied = JsonConvert.DeserializeObject<IBTReceiptReplied>(response.Content);
                    if (replied != null)
                    {
                        // save the the linkage 
                        var newLine = new FTAPP_IBTRIB_Link
                        {
                            IBTDocEntry = dto.IbtEntry,
                            RIBDocEntry = int.Parse(replied.DocEntry)
                        };

                        // remove the last save
                        var delete_query = @$"delete {db.WEBDB}..FTAPP_IBTRIB_Link 
                                              Where 
                                              IBTDocEntry = @IBTDocEntry and RIBDocEntry = @RIBDocEntry";
                        conn.Execute(delete_query, new
                        {
                            IBTDocEntry = dto.IbtEntry,
                            RIBDocEntry = int.Parse(replied.DocEntry)
                        });

                        // insert the new save 
                        var insert_query = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTRIB_Link (
                                             IBTDocEntry
                                           , RIBDocEntry 
                                         ) VALUES (
                                             @IBTDocEntry
                                           , @RIBDocEntry )";

                        conn.Execute(insert_query, newLine);
                        return Ok(replied);
                    }
                    return Ok("Server updated, but replied empty.");
                }

                // when fail
                var replied_fail = JsonConvert.DeserializeObject<IBTReceiptReplied>(response.Content); // IBTReceiptReplied
                if (replied_fail == null)
                {
                    return BadRequest("Server update fail, please try again.");
                }
                return Ok(replied_fail);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetIBTItems(DTO_IBTReceipt dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("invalid subsi id .");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid transfer doc num");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                // get the line 
                var sp_query_line = @"exec sp_GetTransferedIBTLines @webDb, @ibtDocEntry ";
                var ibtLines = conn.Query<IBT1>(sp_query_line, new
                {
                    webDb = db.WEBDB,
                    ibtDocEntry = dto.DocEntry
                }).ToList();

                if (ibtLines.Count == 0)
                {
                    return BadRequest($"Invalid IBT doc num / or IBT #{dto.TransferDocNum} lines no found" +
                        $"\nin {dto.Subsi}, please try again later.");
                }

                // chec any batch 
                for (int i = 0; i < ibtLines.Count; i++)
                {
                    var line = ibtLines[i];
                    if (line == null) continue;

                    //load in the variety or barcode 
                    var barcode_sql = $@"SELECT * 
                                 FROM {db.SAPDB}..OBCD] WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var barcodes = conn.Query<OBCD_Ext>(barcode_sql, new { itemcode = line.ITEMCODE }).ToList();
                    if (barcodes.Count > 0)
                    {
                        ibtLines[i].BarCodes = new List<OBCD_Ext>(barcodes);
                    }

                    // for batch processing
                    if ($"{line.ManBtchNum}" == "Y")
                    {
                        var sp_queryBatch = @" exec sp_GetTransferedIBTLinesBatches @webDb, @ibtEntry, @lineNum ";
                        var batches = conn.Query<IBT2>(sp_queryBatch, new
                        {
                            webDb = db.WEBDB,
                            ibtEntry = line.DOCENTRY,
                            lineNum = line.LINENUM
                        }).ToList();

                        if (batches.Count == 0) continue;
                        ibtLines[i].Batches = new List<IBT2>(batches);
                    }
                }

                return Ok(ibtLines);
            }
            catch (Exception e)
            {

                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetTransferDocAndLines(DTO_IBTReceipt dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubsiId))
                {
                    return BadRequest("invalid subsi id .");
                }
                if (dto.TransferDocNum < 0)
                {
                    return BadRequest("Invalid transfer doc num");
                }
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SubsiId);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                // get the head
                using var conn = new SqlConnection(_commDbConnStr);
                var sp_query = @"exec sp_GetTransferedIBT @webDb, @transferDocNum";
                var ibtHead = conn.Query<KTC_SalesAppWAPI.Models.Pick_IBT.IBT>(sp_query, new
                {
                    webDb = db.WEBDB,
                    transferDocNum = dto.TransferDocNum
                }).FirstOrDefault();

                if (ibtHead == null)
                {
                    return BadRequest($"Invalid IBT doc num / or IBT #{dto.TransferDocNum} no found" +
                        $"\nin {dto.Subsi}, please try again later.");
                }

                // get the line 
                var sp_query_line = @"exec sp_GetTransferedIBTLines @webDb, @ibtDocEntry ";
                var ibtLines = conn.Query<IBT1>(sp_query_line, new
                {
                    webDb = db.WEBDB,
                    ibtDocEntry = ibtHead.DOCENTRY
                }).ToList();

                if (ibtLines.Count == 0)
                {
                    return BadRequest($"Invalid IBT doc num / or IBT #{dto.TransferDocNum} lines no found" +
                        $"\nin {dto.Subsi}, please try again later.");
                }

                ibtHead.Lines = new List<IBT1>(ibtLines);

                // chec any batch 
                for (int i = 0; i < ibtHead.Lines.Count; i++)
                {
                    var line = ibtHead.Lines[i];
                    if (line == null) continue;

                    //load in the variety or barcode 
                    var barcode_sql = $@"SELECT * 
                                 FROM {db.SAPDB}..OBCD WITH (NOLOCK)
                                 WHERE itemcode = @itemcode";

                    var barcodes = conn.Query<OBCD_Ext>(barcode_sql, new { itemcode = line.ITEMCODE }).ToList();
                    if (barcodes.Count > 0)
                    {
                        ibtHead.Lines[i].BarCodes = new List<OBCD_Ext>(barcodes);
                    }

                    // for batch processing
                    if ($"{line.ManBtchNum}" == "Y")
                    {
                        var sp_queryBatch = @" exec sp_GetTransferedIBTLinesBatches @webDb, @ibtEntry, @lineNum ";
                        var batches = conn.Query<IBT2>(sp_queryBatch, new
                        {
                            webDb = db.WEBDB,
                            ibtEntry = line.DOCENTRY,
                            lineNum = line.LINENUM
                        }).ToList();

                        if (batches.Count == 0) continue;

                        ibtHead.Lines[i].Batches = new List<IBT2>(batches);
                    }
                }

                return Ok(ibtHead);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetListTransferedIBT(DTO_IBTReceipt dto)
        {
            try
            {
                if (dto.StartDt == default)
                {
                    return BadRequest("Invalid start date");
                }
                if (dto.EndDt == default)
                {
                    return BadRequest("Invalid end date");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                var sp_qr = @"exec sp_GetListIBT_RIBReceipt @webDb, @startDT,  @endDT";
                using var conn = new SqlConnection(_commDbConnStr);
                var transferredIBTs = conn.Query<RIB>(sp_qr, new
                {
                    webDb = db.WEBDB,
                    startDT = dto.StartDt,
                    endDT = dto.EndDt
                }).ToList();


                if (transferredIBTs.Count == 0) return NotFound();
                return Ok(transferredIBTs);
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

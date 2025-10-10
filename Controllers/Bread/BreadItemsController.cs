using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread.Items;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.BreadTrade;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class BreadItemsController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<PortalLoginBreadController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr_Bread;

        public BreadItemsController(IConfiguration configuration, ILogger<PortalLoginBreadController> logger)
        {
            _configuration = configuration;
            _logger = logger;            
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
        }

        [HttpPost]
        public IActionResult Post(Dto_BreadItems dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadItems":
                    {
                        return LoadItems(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");                        
                    }
            }
        }

        IActionResult LoadItems (Dto_BreadItems dto)
        {            
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode)) // reserver for future use
                {
                    return BadRequest("Invalid user code");
                }
                
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sp_query = @"exec sp_LoadItemMasters @webDb, @companyId, @userCode";
                using var conn = new SqlConnection(_commDbConnStr_Bread);
                var items = conn.Query<OITM_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    companyId = db.COMPANYNAME,
                    userCode = dto.UserCode
                }, commandTimeout:0).ToList();

                if (items.Count == 0) return NotFound();

                // load in the preset barcode
                LoadBarcodes(items, conn, db);

                // load the item details for bread pricing
                if (!string.IsNullOrWhiteSpace(dto.CardCode))
                {
                    // 2022 02 14
                    LoadItemDetails(items, conn, db, dto.UserCode, dto.CardCode, dto.IsKtcStore);
                }

                return Ok(items);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // massage each line with barcode 
        List<OITM_Ext> LoadBarcodes (List<OITM_Ext> items, SqlConnection conn, DbInfo db)
        {
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var line = items[i];
                    if (line == null) continue;
                    
                    // load the barcode table if any 
                    var sp_loadBarCodes = @"exec sp_SelectOBCDBread @erpDb, @itemCode";
                    line.BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                        new
                        {
                            erpDb = db.SAPDB,
                            itemCode = line.ItemCode
                        }, commandTimeout:0).ToList();

                    // add more barcode 
                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.ItemCode,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    items[i].BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.CodeBars,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    items[i].BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.SuppCatNum,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    items[i].BarCodes.Add(supcatnum);
                }

                return items;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return items;
            }
        }

        List<OITM_Ext> LoadItemDetails(
            List<OITM_Ext> items, SqlConnection conn, DbInfo db, string companyId, string cardCode, bool isKtcStore)
        {
            try
            {
                // query the price and info from SAP tables
                if (isKtcStore)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] == null) continue;

                        // load the barcode table if any 
                        var sp_LoadBreadItemDetail = @"exec sp_ItemDetail_SapKtcStore @webDb, @companyid, @cardCode, @itemCode";
                        items[i].BreadItemDetail = conn.Query<Bread_ItemDetail>(sp_LoadBreadItemDetail,
                            new
                            {
                                webDb = db.WEBDB,
                                companyid = companyId,
                                cardCode = cardCode,
                                itemCode = items[i].ItemCode
                            }, commandTimeout:0).FirstOrDefault();
                    }

                    return items;
                }
                
                // query the price and info from portal tables
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null) continue;

                    // load the barcode table if any 
                    var sp_LoadBreadItemDetail =
                        @"exec sp_ItemDetail_PortalNonKtcStore @webDb, @companyid, @cardCode, @itemCode";

                    items[i].BreadItemDetail = conn.Query<Bread_ItemDetail>(sp_LoadBreadItemDetail,
                        new
                        {
                            webDb = db.WEBDB,
                            companyid = companyId,
                            cardCode = cardCode,
                            itemCode = items[i].ItemCode
                        }, commandTimeout:0).FirstOrDefault();
                }

                return items;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return items;
            }
        }
    }
}

using Dapper;
using KTC_SalesAppWAPI.Models.Bread;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers
{
    public class BreadRequestHelper
    {
        public string LastErrorMessage { get; set; } = string.Empty;
        public string MidwareDbConnectStr { get; set; }
        public bool IsLoadSuccess { get; set; }

        SqlConnection sqlConnection { get; set; } = null;
        SqlTransaction sqlTransaction { get; set; } = null;

        // insert new request , doc and it lines
        public void BeginTransaction()
        {
            try
            {
                sqlConnection = new SqlConnection(MidwareDbConnectStr);
                sqlConnection.Open();
                sqlTransaction = sqlConnection.BeginTransaction();
                IsLoadSuccess = true;
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                IsLoadSuccess = false;
            }
        }

        public int InsertRequest(FTAPP_MWRequest request)
        {
            try
            {
                string query = @"INSERT INTO FTAPP_MWRequest (
                                 Request 
                                ,SapUser 
                                ,SapPassword 
                                ,RequestTime 
                                ,PhoneRegID 
                                ,Status 
                                ,Guid 
                                ,AttachFileCnt 
                                ) VALUES( 
                                 @Request 
                                ,@SapUser 
                                ,@SapPassword 
                                ,GETDATE()
                                ,@PhoneRegID 
                                ,@Status 
                                ,@Guid 
                                ,@AttachFileCnt  )";

                return sqlConnection.Execute(query, request, sqlTransaction);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                RollBack();
                return -1;
            }
        }

        public int InsertDocHeader(FTAPP_MWDocHeader head)
        {
            try
            {
                /* 
                 * 
                 *  public DateTime DocDate { get; set; }
                     public DateTime TaxDate { get; set; }
                     public DateTime DueDate { get; set; }
                 */

                string sql = @"DocType 
                           ,CardCode 
                           ,CardName 
                           ,DocAmt 
                           ,DocNum                                                      
                            ,Ref2  
                           ,Comments  
                           ,JrnlMemo 
                           ,NumberAtCard 
                           ,Guid  
                           ,Submitted  
                           ,SapDocNo  
                           ,ContactPerson 
                           ,ShipAddress 
                           ,BillAddress  
                           ,DiscountByPercent  
                           ,DiscountByValue  
                           ,TaxAmt  
                           ,NumberFileAttached  
                           ,DocSeries  
                           ,Currency  
                           ,SalesPerson  
                           ,TotalBeforeDis  
                           ,IsPostDraft , ODCardCode  , ODCardName, BaseITEntry, TruckNo, UsedTrayQty ";                                          

                var sqlValue = @" @DocType  
                                   ,@CardCode 
                                   ,@CardName  
                                   ,@DocAmt  
                                   ,@DocNum                           
                                   ,@Ref2  
                                   ,@Comments  
                                   ,@JrnlMemo  
                                   ,@NumberAtCard  
                                   ,@Guid  
                                   ,@Submitted  
                                   ,@SapDocNo  
                                   ,@ContactPerson  
                                   ,@ShipAddress  
                                   ,@BillAddress  
                                   ,@DiscountByPercent  
                                   ,@DiscountByValue  
                                   ,@TaxAmt  
                                   ,@NumberFileAttached  
                                   ,@DocSeries  
                                   ,@Currency  
                                   ,@SalesPerson 
                                   ,@TotalBeforeDis
                                   ,@IsPostDraft , @ODCardCode  , @ODCardName, @BaseITEntry, @TruckNo, @UsedTrayQty ";

                if (head.TaxDate != default)
                {
                    sql += " , TaxDate ";
                    sqlValue += ", @TaxDate  ";

                }

                if (head.DocDate != default)
                {
                    sql += " , DocDate ";
                    sqlValue += ", @DocDate  ";
                }

                if (head.DueDate != default)
                {
                    sql += " , DueDate ";
                    sqlValue += ", @DueDate  ";
                }

                var combineSql = $"INSERT INTO FTAPP_MWDocHeader ( {sql} ) VALUES ( {sqlValue} )";

                return sqlConnection.Execute(combineSql, head, sqlTransaction);
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                RollBack();
                return -1;
            }
        }

        public int InsertDocDetailsLine(List<FTAPP_MWDocDetails> lines)
        {
            try
            {
                var counter = 0;
                for (int x = 0; x < lines.Count; x ++)
                {
                    var line = lines[x];
                    if (line == null) continue;
                    line.LineNum = x;

                    string insertLineSql = @"
                                LineNum
                              , HeaderGuid
                              , DocNumber
                              , ItemCode
                              , ItemName
                              , OrderQty
                              , Price
                              , LineAmount
                              , TotalBeforeDiscount
                              , DisByPercent
                              , DisByValue
                              , TaxCode
                              , TaxAmt
                              , ToWhsCode
                              , FromWhsCode
                              , SelectedUom                             
                              , Pricelist
                              , TotalBeforeDis
                              , TaxName
                              , TaxRate
                              , GrossPrice
                              , LineTotal
                              , ItemGuid , Remarks, Batch";

                    // , DeliveryDate, , ExpiredDate 

                    var sql_value = @"@LineNum
                                   ,@HeaderGuid
                                   ,@DocNumber
                                   ,@ItemCode
                                   ,@ItemName
                                   ,@OrderQty
                                   ,@Price
                                   ,@LineAmount
                                   ,@TotalBeforeDiscount
                                   ,@DisByPercent
                                   ,@DisByValue
                                   ,@TaxCode
                                   ,@TaxAmt
                                   ,@ToWhsCode
                                   ,@FromWhsCode
                                   ,@SelectedUom              
                                   ,@Pricelist
                                   ,@TotalBeforeDis
                                   ,@TaxName
                                   ,@TaxRate
                                   ,@GrossPrice
                                   ,@LineTotal
                                   ,@ItemGuid , @Remarks, @Batch";

                    if (line.DeliveryDate != default)
                    {
                        insertLineSql += ", DeliveryDate";
                        sql_value += ",@DeliveryDate";
                    }

                    if (line.ExpiredDate != default)
                    {
                        insertLineSql += ", ExpiredDate";
                        sql_value += ",@ExpiredDate";
                    }

                    var sqlCombine = $@"INSERT INTO FTAPP_MWDocDetails ({insertLineSql}) values ({sql_value}) ";
                    sqlConnection.Execute(sqlCombine, line, sqlTransaction);
                    counter++;
                }
               
                if (counter == lines.Count)
                {
                    return 1;
                }
                return -1;
            }
            catch (Exception e)
            {
                LastErrorMessage = $"{e.Message}\n{e.StackTrace}";
                RollBack();
                return -1;
            }
        }


        bool RollBack()
        {
            sqlTransaction?.Rollback();
            return true;
        }

        public bool Commit()
        {
            sqlTransaction?.Commit();
            return true;
        }

        // insert header
        // insert lines

    }
}



using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KTC_SalesAppWAPI.Helpers
{
    public class PickInvoicePostChecker
    {

        //public string CheckBeforePost2(List<SO1> lines, List<FTAPP_Box> boxes)
        //{
        //    const string _Message = "[Server Message]";
        //    var message = _Message;

        //    try
        //    {
        //        // combine all the box content
        //        var boxesContent = new List<FTAPP_Box1>();
        //        for (int b = 0; b < boxes.Count; b++)
        //        {
        //            var box = boxes[b];
        //            if (box == null) continue;
        //            if (box.Contents == null) continue;
        //            boxesContent.AddRange(box.Contents);
        //        }

        //        // loop for each line to check the qty
        //        for (int id = 0; id < lines.Count; id++)
        //        {
        //            var line = lines[id];
        //            if (line == null) continue;

        //            var sumOfQtyCs = boxesContent.Where(b => b.BaseEntry == line.DOCENTRY &&
        //                                                       b.BaseLine == line.LINENUM &&
        //                                                       b.ItemCode == line.ITEMCODE &&
        //                                                       b.Packaging == "CS").Sum(s => s.Qty);

        //            var sumOfQtyCsInPcs = sumOfQtyCs * line.UOMQTY;
        //            var sumOfQtyPc = boxesContent.Where(b => b.BaseEntry == line.DOCENTRY &&
        //                                                   b.BaseLine == line.LINENUM &&
        //                                                   b.ItemCode == line.ITEMCODE &&
        //                                                   b.Packaging == "PC").Sum(s => s.Qty);

        //            var boxIds = boxes.Select(x=>x.box)



        //            decimal sumInPcs = sumOfQtyPc + sumOfQtyCsInPcs;

        //            if (sumInPcs > line.PICKEDQTY)
        //            {
        //                message += $"\nPlease recheck the item : " +
        //                            $"{line.ITEMNAME}\n{line.ITEMCODE}\n{line.SUPPCATNUM}\nLine: {line.LINENUM}," +
        //                            $"\nPicked: {line.PICKEDQTY:N2} pc" +
        //                            $"\n-- No tally in box(s) {boxIds} -- Qty: {sumInPcs:N2} pc\n ------\n";
        //            }
        //        }

        //        if (message.Equals(_Message)) return string.Empty;
        //        return message;
        //    }
        //    catch (Exception e)
        //    {
        //        return e.Message;
        //    }
        //}

        public string CheckBeforePost(List<SO1> lines, List<FTAPP_Box> boxes)
        {
            const string _Message = "[Server Message]";
            var message = _Message;

            try
            {
                for (int l = 0; l < lines.Count; l++)
                {
                    var line = lines[l];
                    if (line == null) continue;
                    if (line.PICKEDQTY == 0) continue;

                    decimal sumInPcs = 0;
                    string boxIds = "";

                    for (int bid = 0; bid < boxes.Count; bid++)
                    {
                        var box = boxes[bid];
                        if (box == null) continue;
                        if (box.Contents == null) continue;

                        var sumOfQtyCs = box.Contents.Where(b => b.BaseEntry == line.DOCENTRY &&
                                                               b.BaseLine == line.LINENUM &&
                                                               b.ItemCode == line.ITEMCODE &&
                                                               b.Packaging == "CS").Sum(s => s.Qty);

                        var sumOfQtyCsInPcs = sumOfQtyCs * line.UOMQTY;
                        var sumOfQtyPc = box.Contents.Where(b => b.BaseEntry == line.DOCENTRY &&
                                                               b.BaseLine == line.LINENUM &&
                                                               b.ItemCode == line.ITEMCODE &&
                                                               b.Packaging == "PC").Sum(s => s.Qty);

                        sumInPcs += sumOfQtyPc + sumOfQtyCsInPcs;
                        boxIds += $"{box.BoxId},";
                    }

                    if (sumInPcs > line.PICKEDQTY)
                    {
                        message += $"\nPlease recheck the item : " +
                                    $"{line.ITEMNAME}\n{line.ITEMCODE}\n{line.SUPPCATNUM}\nLine: {line.LINENUM}," +
                                    $"\nPicked: {line.PICKEDQTY:N2} pc" +
                                    $"\n-- No tally in box(s) {boxIds} -- Qty: {sumInPcs:N2} pc\n ------\n";
                    }
                }

                if (message.Equals(_Message)) return string.Empty;
                return message;
            }
            catch (Exception e)
            {
                //LastErrorMessage = e.Message;
                return e.Message;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI.Helpers
{
    public class TupperwareCartonScanLogic
    {
        public string LastErrorMessage { get; set; }


        public string GetPossibleCodes(string code)
        {
            var arr = ParseCode(code);

            if (arr == null) return "'" + code + "'";
            if (arr.Length == 0) return "'" + code + "'";

            return "'" + $"{string.Join("','", arr)}" + "'";
        }

        string[] ParseCode(string code)
        {
            var posibleCodes = new List<string>();
            posibleCodes.Add(GetCode(code)); // will return last 3 char trim off
            posibleCodes.Add(RemoveRightC(code, 5));
            posibleCodes.Add(RemoveRightC(code, 6));

            return posibleCodes.ToArray();
        }

        string RemoveRightC(string code, int removeCharNo)
        {
            try
            {
                if (removeCharNo > code.Length)
                {
                    return code;
                }

                var result = code.Substring(0, code.Length - removeCharNo);
                return result;
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                return code;
            }
        }

        string GetCode(string code)
        {
            try
            {
                var codeArr = code.ToCharArray();
                if (codeArr == null) return code;
                if (codeArr.Length == 0) return code;
                var result = code;

                //if (code.Length > 17) // max char cap
                //{
                //    return code;
                //}

                char[] combineFirstThreeC = { codeArr[0], codeArr[1], codeArr[2] };
                var firstThreeC = new string(combineFirstThreeC);

                if (firstThreeC == "000")
                {
                    //4th & 5th digit to 11 and take 8 digits(4th - 11th)
                    codeArr[3] = '1';
                    codeArr[4] = '1';

                    char[] newChars = { codeArr[3], codeArr[4], codeArr[5], codeArr[6], codeArr[7], codeArr[8], codeArr[9], codeArr[10] };

                    result = new string(newChars);
                    return result;
                }

                char[] combineFirstTwoC = { codeArr[0], codeArr[1] };
                var firstTwo = new string(combineFirstTwoC);

                if (firstTwo == "00")
                {
                    //3rd & 4th digit to 11 and take 8 digits(3rd - 10th digits)
                    codeArr[2] = '1';
                    codeArr[3] = '1';

                    char[] newC = { codeArr[2], codeArr[3], codeArr[4], codeArr[5], codeArr[6], codeArr[7], codeArr[8], codeArr[9] };
                    result = new string(newC);
                    return result;
                }

                if (firstTwo != "00" )
                {
                    // If left <> 00 then Remove Right 3 digits else, if not match JDE 2nd item
                    result = RemoveRightC(code, 3);
                    return result;
                }

                if (firstTwo == "99" )
                {
                    //If left = 99 then Remove left 2 digits and Remove Right 6 digits
                    result = code.Substring(2, code.Length - 2); // remove left 
                    result = result.Substring(result.Length - 6); // remove right 6 digit 
                    return result;
                }

                return result;

            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                return code;
            }
        }
    }
}

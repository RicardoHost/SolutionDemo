using Demo.Common.Unitity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Demo.Main
{
    public class Program
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                var input = Console.ReadLine();
                Console.Write(decimal.Parse(input).ToString("000"));
            }
        }

        public interface XsxhModel
        {
            public string xsxh { set; get; }
        }
        public class Model: XsxhModel
        {
            public string xsxh { set; get; }
        }

        public class XsxhComparer : IComparer<XsxhModel>
        {
            public int Compare(XsxhModel model0,XsxhModel model1)
            {
                var xsxh0 = model0.xsxh;
                var xsxh1 = model1.xsxh;
                if (xsxh0 == "F目100" && xsxh1 == "F目11")
                {
                    
                }
                if (string.IsNullOrEmpty(xsxh0) && string.IsNullOrEmpty(xsxh1))
                {
                    return 0;
                }
                else if (string.IsNullOrEmpty(xsxh0))
                {
                    return -1;
                }
                else if (string.IsNullOrEmpty(xsxh1))
                {
                    return 1;
                }
                else
                {
                    try
                    {
                        var result = 0;
                        var len0 = xsxh0.Length;
                        var len1 = xsxh1.Length;
                        var len = Math.Min(len0,len1);
                        for (var i = 0; i < 2; i++)
                        {
                            result = xsxh0[i].CompareTo(xsxh1[i]);
                            if (result != 0)
                            {
                                return result;
                            }
                        }
                        xsxh0 = xsxh0.Substring(2);
                        var xsxhArr0 = xsxh0.Split(".", StringSplitOptions.RemoveEmptyEntries);
                        xsxh1 = xsxh1.Substring(2);
                        var xsxhArr1 = xsxh1.Split(".",StringSplitOptions.RemoveEmptyEntries);
                        len = Math.Min(xsxhArr0.Count(),xsxhArr1.Count());
                        for (var i = 0; i < len; i++)
                        {
                            var val0 = int.Parse(xsxhArr0[i]);
                            var val1 = int.Parse(xsxhArr1[i]);
                            result = val0.CompareTo(val1);
                            if (result != 0)
                            {
                                return result;
                            }
                        }
                        if (len0 < len1)
                        {
                            result = -1;
                        }
                        else if (len0 > len1)
                        {
                            result = 1;
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                }
            }
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Demo.Main
{
    /// <summary>
    /// 百度AI识别结果(Json字符串)格式转换
    /// </summary>
    public class BaiduAIRecognizeResultConverter
    {
        public BaiduAIRecognizeResultModel Convert(string input)
        {
            var result = new BaiduAIRecognizeResultModel() { details = new List<string[]>()};
            var fileNames = Directory.GetFiles(input).OrderBy(x => x).ToList();
            for (int i = 0; i < fileNames.Count; i++)
            {
                var item = fileNames[i];
                var content = File.ReadAllText(item);
                try
                {
                    var jObject = JObject.Parse(content);
                    var jResult = jObject["vat_result"]["words_result"];

                    if (i == fileNames.Count - 1)
                    {
                        result.number = jResult["InvoiceNum"].ToString();
                        result.date = jResult["InvoiceDate"].ToString();
                        result.amount = jResult["TotalAmount"].ToString();
                        result.tax = jResult["TotalTax"].ToString();
                        result.drawer = jResult["NoteDrawer"].ToString();
                        result.figuers = jResult["AmountInFiguers"].ToString();
                        result.figuersWords = jResult["AmountInWords"].ToString();
                        result.comment = jResult["Remarks"].ToString();
                    }

                    //名称列
                    var nameColumns = jResult["CommodityName"];
                    //规格型号列
                    var specificationsColumns = jResult["CommodityType"];
                    //单位列
                    var unitColumns = jResult["CommodityUnit"];
                    //数量列
                    var numberColumns = jResult["CommodityNum"];
                    //单价列
                    var priceColumns = jResult["CommodityPrice"];
                    //金额列
                    var amountColumns = jResult["CommodityAmount"];
                    //税率列
                    var taxRateColumns = jResult["CommodityTaxRate"];
                    //税额列
                    var taxColumns = jResult["CommodityTax"];

                    var nameColumnsDic = ToDictionary(nameColumns);
                    var specificationsColumnsDic = ToDictionary(specificationsColumns);
                    var unitColumnsDic = ToDictionary(unitColumns);
                    var numberColumnsDic = ToDictionary(numberColumns);
                    var priceColumnsDic = ToDictionary(priceColumns);
                    var amountColumnsDic = ToDictionary(amountColumns);
                    var taxRateColumnsDic = ToDictionary(taxRateColumns);
                    var taxColumnsDic = ToDictionary(taxColumns);
                    var count = Math.Max(nameColumns.Count(), specificationsColumns.Count());
                    for (var j = 1; j <= count; j++)
                    {
                        var name = GetDictionaryValue(nameColumnsDic, j);
                        var specification = GetDictionaryValue(specificationsColumnsDic, j);
                        var unit = GetDictionaryValue(unitColumnsDic, j);
                        var number = GetDictionaryValue(numberColumnsDic, j);
                        var price = GetDictionaryValue(priceColumnsDic, j);
                        var amount = GetDictionaryValue(amountColumnsDic, j);
                        var taxRate = GetDictionaryValue(taxRateColumnsDic, j);
                        var tax = GetDictionaryValue(taxColumnsDic, j);
                        if (!unitColumnsDic.ContainsKey(j))
                        {
                            var row = result.details[result.details.Count - 1];
                            SetArray(name, specification, unit, number, amount, taxRate, tax, row);
                        }
                        else
                        {
                            var row = new string[8];
                            SetArray(name, specification, unit, number, amount, taxRate, tax, row);
                            result.details.Add(row);
                        }
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"文件{item}内容解析异常,需检查是否是合法Json格式!");
                    break;
                }
            }

            result.details.ForEach(x =>
            {
                Console.WriteLine($"{x[0]} {x[1]} {x[2]} {x[3]} {x[4]} {x[5]} {x[6]} {x[7]}");
            });
            return result;
        }
        /// <summary>
        /// JToken转为字段(row为键,word为值)
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private static Dictionary<int,string> ToDictionary(JToken jObject)
        {
            return jObject.ToDictionary(x => int.Parse(x["row"].ToString()), x => x["word"].ToString());
        }
        /// <summary>
        /// 从字典获取对应值
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static string GetDictionaryValue(Dictionary<int,string> dic,int i)
        {
            return dic.GetValueOrDefault(i, string.Empty);
        }
        /// <summary>
        /// 设置数组属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="specification"></param>
        /// <param name="unit"></param>
        /// <param name="number"></param>
        /// <param name="amount"></param>
        /// <param name="taxRate"></param>
        /// <param name="tax"></param>
        /// <param name="row"></param>
        private static void SetArray(string name, string specification, string unit, string number, string amount, string taxRate, string tax, string[] row)
        {
            row[0] += name;
            row[1] += specification;
            row[2] += unit;
            row[3] += number;
            row[4] += amount;
            row[5] += tax;
            row[6] += taxRate;
            row[7] += tax;
        }
    }

    public class BaiduAIRecognizeResultModel
    {
        /// <summary>
        /// 发票号码
        /// </summary>
        public string number { set; get; }

        /// <summary>
        /// 开票日期
        /// </summary>
        public string date { set; get; }

        /// <summary>
        /// 金额合计
        /// </summary>
        public string amount { set; get; }

        /// <summary>
        /// 税额合计
        /// </summary>
        public string tax { set; get; }

        /// <summary>
        /// 价税合计
        /// </summary>
        public string figuers { set; get; }

        /// <summary>
        /// 价税合计(大写)
        /// </summary>
        public string figuersWords { set; get; }

        /// <summary>
        /// 备注
        /// </summary>
        public string comment { set; get; }

        /// <summary>
        /// 开票人
        /// </summary>
        public string drawer { set; get; }

        public List<string[]> details { set; get; }
    }
}

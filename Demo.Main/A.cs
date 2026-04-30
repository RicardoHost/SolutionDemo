using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Main
{
    public class BaiduAIRecognizer
    {
        private const string ApiKey = "f2fOI0qGrPQEoMa4fRqjZcOJ";
        private const string SecretKey = "7U18PuWT7x0fajEgNW0UquasJ7YZXPR1";
        private const string VatOcrUrl = "https://aip.baidubce.com/rest/2.0/ocr/v1/vat_invoice";
        private const string GeneralOcrUrl = "https://aip.baidubce.com/rest/2.0/ocr/v1/accurate_basic";
        public static void PrintUsage()
        {
            Console.WriteLine("用法: InvoiceOCR.exe <发票文件>");
            Console.WriteLine();
            Console.WriteLine("支持格式: .pdf, .png, .jpg, .jpeg, .bmp");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  InvoiceOCR.exe invoice.pdf");
            Console.WriteLine("  InvoiceOCR.exe invoice.png");
        }


        public static async Task Recognize()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║      增值税发票识别系统 - C# .NET Core      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
            Console.WriteLine();

            //if (args.Length == 0)
            //{
            //    PrintUsage();
            //    return;
            //}

            string filePath = "D:\\Downloads\\四川省商投建筑工程有限责任公司-11月-7387327.69.pdf";//args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误: 文件不存在 - {filePath}");
                return;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            Console.WriteLine($"输入文件: {Path.GetFullPath(filePath)}");
            Console.WriteLine($"文件类型: {extension}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("获取百度Access Token...");
                string accessToken = await GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("获取Access Token失败");
                    return;
                }
                Console.WriteLine("✓ Access Token获取成功");
                Console.WriteLine();

                if (extension == ".pdf")
                {
                    Console.WriteLine("正在将PDF转换为图片...");
                    var imagePaths = await ConvertPdfToImagesAsync(filePath);

                    if (imagePaths == null || imagePaths.Count == 0)
                    {
                        Console.WriteLine("PDF转换失败");
                        return;
                    }

                    Console.WriteLine($"PDF共 {imagePaths.Count} 页，已转换为图片");
                    Console.WriteLine();

                    var allResults = new JArray();

                    for (int i = 0; i < imagePaths.Count; i++)
                    {
                        Console.WriteLine($"正在识别第 {i + 1}/{imagePaths.Count} 页...");

                        var ocrResult = await RecognizeVatImageAsync(imagePaths[i], accessToken);

                        if (ocrResult != null && ocrResult["error_code"] == null)
                        {
                            var pageResult = new JObject
                            {
                                ["page"] = i + 1,
                                ["image"] = imagePaths[i],
                                ["vat_result"] = ocrResult,
                                ["merged_words"] = ExtractVatFields(ocrResult)
                            };
                            allResults.Add(pageResult);
                            PrintVatResult(ocrResult, i + 1);
                            var pageJson = new JObject { ["page"] = i + 1, ["vat_result"] = ocrResult };
                            //SaveFormattedJson(pageJson, imagePaths[i]);
                            SaveJson(pageJson, imagePaths[i]);
                        }
                        else
                        {
                            Console.WriteLine($"  第 {i + 1} 页VAT识别失败，尝试通用OCR...");
                            var generalResult = await RecognizeGeneralImageAsync(imagePaths[i], accessToken);

                            if (generalResult != null && generalResult["error_code"] == null)
                            {
                                var wordsResult = generalResult["words_result"] as JArray;
                                if (wordsResult != null)
                                {
                                    var mergedResult = MergeWrappedText(wordsResult);
                                    var pageResult = new JObject
                                    {
                                        ["page"] = i + 1,
                                        ["image"] = imagePaths[i],
                                        ["words_result"] = mergedResult["words_result"],
                                        ["merged_words"] = mergedResult["merged_words"],
                                        ["words_result_num"] = mergedResult["words_result_num"]
                                    };
                                    allResults.Add(pageResult);
                                    PrintPageResult(pageResult, i + 1);
                                }
                            }
                            else
                            {
                                string errorMsg = ocrResult?["error_msg"]?.ToString() ?? "Unknown error";
                                Console.WriteLine($"  第 {i + 1} 页识别失败: {errorMsg}");
                            }
                        }
                        Console.WriteLine();
                    }

                    var finalResult = new JObject
                    {
                        ["source_file"] = filePath,
                        ["total_pages"] = imagePaths.Count,
                        ["pages"] = allResults,
                        ["total_blocks"] = allResults.Children().Sum(p => (int?)p["words_result_num"] ?? 0)
                    };

                    SaveJson(finalResult, filePath);
                }
                else
                {
                    Console.WriteLine("正在识别...");
                    var ocrResult = await RecognizeVatImageAsync(filePath, accessToken);

                    if (ocrResult != null && ocrResult["error_code"] == null)
                    {
                        Console.WriteLine("✓ VAT发票识别成功!");
                        Console.WriteLine();
                        PrintVatResult(ocrResult, 1);
                        var singleJson = new JObject { ["vat_result"] = ocrResult };
                        SaveFormattedJson(singleJson, filePath);

                        var result = new JObject
                        {
                            ["source_file"] = filePath,
                            ["vat_result"] = ocrResult,
                            ["merged_words"] = ExtractVatFields(ocrResult)
                        };
                        SaveJson(result, filePath);
                    }
                    else
                    {
                        var generalResult = await RecognizeGeneralImageAsync(filePath, accessToken);
                        if (generalResult != null && generalResult["error_code"] == null)
                        {
                            Console.WriteLine("✓ 通用OCR识别成功!");
                            Console.WriteLine();
                            var mergedResult = MergeWrappedText(generalResult["words_result"] as JArray);
                            PrintMergedResult(mergedResult);

                            var result = new JObject
                            {
                                ["source_file"] = filePath,
                                ["words_result"] = mergedResult["words_result"],
                                ["merged_words"] = mergedResult["merged_words"],
                                ["words_result_num"] = mergedResult["words_result_num"]
                            };
                            SaveJson(result, filePath);
                        }
                        else
                        {
                            string errorMsg = ocrResult?["error_msg"]?.ToString() ?? "Unknown error";
                            Console.WriteLine($"识别失败: {errorMsg}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
        static async Task<string> GetAccessTokenAsync()
        {
            string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={ApiKey}&client_secret={SecretKey}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync(url, null);
                    string content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    if (json["access_token"] != null)
                        return json["access_token"].ToString();

                    Console.WriteLine($"获取Token失败: {json["error_description"] ?? content}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"请求错误: {ex.Message}");
                    return null;
                }
            }
        }

        static async Task<List<string>> ConvertPdfToImagesAsync(string pdfPath)
        {
            try
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "pdf2imgs.py");
                string script = @"# -*- coding: utf-8 -*-
                import fitz
                import sys
                import json
                import os

                pdf_path = sys.argv[1]
                output_dir = os.path.dirname(pdf_path)

                try:
                    doc = fitz.open(pdf_path)
                    images = []
                    for i in range(len(doc)):
                        page = doc.load_page(i)
                        pix = page.get_pixmap(matrix=fitz.Matrix(150/72, 150/72))
                        output_path = os.path.join(output_dir, f'page_{i+1}.png')
                        pix.save(output_path)
                        images.append(output_path)
                    doc.close()
                    print(json.dumps(images))
                except Exception as e:
                    print(json.dumps({'error': str(e)}))
                ";
                if(!File.Exists(scriptPath)) File.Create(scriptPath);
                File.WriteAllText(scriptPath, script);

                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" \"{pdfPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    try
                    {
                        var images = JArray.Parse(output);
                        var list = new List<string>();
                        foreach (var img in images)
                            list.Add(img.ToString());
                        return list;
                    }
                    catch
                    {
                        return null;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF转换异常: {ex.Message}");
                return null;
            }
        }

        static async Task<JObject> RecognizeVatImageAsync(string imagePath, string accessToken)
        {
            string url = $"{VatOcrUrl}?access_token={accessToken}";

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("image", base64Image)
                });

                try
                {
                    var response = await client.PostAsync(url, content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(responseContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VAT识别异常: {ex.Message}");
                    return null;
                }
            }
        }

        static async Task<JObject> RecognizeGeneralImageAsync(string imagePath, string accessToken)
        {
            string url = $"{GeneralOcrUrl}?access_token={accessToken}";

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("image", base64Image),
                    new KeyValuePair<string, string>("detect_direction", "true"),
                    new KeyValuePair<string, string>("probability", "true")
                });

                try
                {
                    var response = await client.PostAsync(url, content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(responseContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"通用OCR识别异常: {ex.Message}");
                    return null;
                }
            }
        }

        static JObject MergeWrappedText(JArray wordsResult)
        {
            var mergedList = new List<JObject>();
            var mergedWords = new Dictionary<string, string>();

            if (wordsResult == null)
            {
                return new JObject
                {
                    ["words_result"] = new JArray(),
                    ["merged_words"] = new JObject(),
                    ["words_result_num"] = 0
                };
            }

            var allText = new StringBuilder();
            foreach (var item in wordsResult)
            {
                var word = item["words"]?.ToString() ?? "";
                allText.Append(word);
            }

            string fullText = allText.ToString();
            ExtractAllFields(mergedWords, fullText);

            mergedList.Add(new JObject
            {
                ["merged_text"] = fullText.Length > 200 ? fullText.Substring(0, 200) + "..." : fullText,
                ["line"] = 0
            });

            Console.WriteLine($"[1] {fullText.Substring(0, Math.Min(80, fullText.Length))}...");
            Console.WriteLine();

            var resultObj = new JObject
            {
                ["words_result"] = JArray.FromObject(mergedList),
                ["raw_words_result"] = wordsResult,
                ["merged_words"] = JObject.FromObject(mergedWords),
                ["words_result_num"] = mergedList.Count
            };

            return resultObj;
        }

        static JObject ExtractVatFields(JObject vatResult)
        {
            var fields = new JObject();

            var wordsResult = vatResult["words_result"] as JObject;
            if (wordsResult == null) return fields;

            var mappings = new Dictionary<string, string[]>
            {
                ["发票号码"] = new string[] { "InvoiceNum" },
                ["开票日期"] = new string[] { "InvoiceDate" },
                ["购买方名称"] = new string[] { "PurchaserName" },
                ["销售方名称"] = new string[] { "SellerName" },
                ["购买方税号"] = new string[] { "PurchaserRegisterNum" },
                ["销售方税号"] = new string[] { "SellerRegisterNum" },
                ["金额"] = new string[] { "TotalAmount" },
                ["税额"] = new string[] { "TotalTax" },
                ["价税合计小写"] = new string[] { "AmountInFiguers" },
                ["价税合计大写"] = new string[] { "AmountInWords" },
                ["开票人"] = new string[] { "NoteDrawer" },
                ["复核"] = new string[] { "Checker" },
                ["收款人"] = new string[] { "Payee" },
                ["备注"] = new string[] { "Remarks" }
            };

            foreach (var mapping in mappings)
            {
                foreach (string key in mapping.Value)
                {
                    if (wordsResult[key] != null)//&& !fields.ContainsKey(mapping.Key)
                    {
                        fields[mapping.Key] = wordsResult[key].ToString();
                    }
                }
            }

            var itemList = new StringBuilder();

            var commodityName = wordsResult["CommodityName"] as JArray;
            var commodityType = wordsResult["CommodityType"] as JArray;
            var commodityUnit = wordsResult["CommodityUnit"] as JArray;
            var commodityNum = wordsResult["CommodityNum"] as JArray;
            var commodityPrice = wordsResult["CommodityPrice"] as JArray;
            var commodityAmount = wordsResult["CommodityAmount"] as JArray;
            var commodityTaxRate = wordsResult["CommodityTaxRate"] as JArray;
            var commodityTax = wordsResult["CommodityTax"] as JArray;

            int maxItems = 0;
            if (commodityName != null) maxItems = Math.Max(maxItems, commodityName.Count);
            if (commodityType != null) maxItems = Math.Max(maxItems, commodityType.Count);
            if (commodityUnit != null) maxItems = Math.Max(maxItems, commodityUnit.Count);
            if (commodityNum != null) maxItems = Math.Max(maxItems, commodityNum.Count);

            for (int i = 0; i < maxItems; i++)
            {
                var item = new StringBuilder();

                string name = commodityName != null && commodityName.Count > i ?
                    commodityName[i]?["word"]?.ToString() ?? "" : "";
                string spec = commodityType != null && commodityType.Count > i ?
                    commodityType[i]?["word"]?.ToString() ?? "" : "";
                string unit = commodityUnit != null && commodityUnit.Count > i ?
                    commodityUnit[i]?["word"]?.ToString() ?? "" : "";
                string qty = commodityNum != null && commodityNum.Count > i ?
                    commodityNum[i]?["word"]?.ToString() ?? "" : "";
                string price = commodityPrice != null && commodityPrice.Count > i ?
                    commodityPrice[i]?["word"]?.ToString() ?? "" : "";
                string amount = commodityAmount != null && commodityAmount.Count > i ?
                    commodityAmount[i]?["word"]?.ToString() ?? "" : "";
                string taxRate = commodityTaxRate != null && commodityTaxRate.Count > i ?
                    commodityTaxRate[i]?["word"]?.ToString() ?? "" : "";
                string tax = commodityTax != null && commodityTax.Count > i ?
                    commodityTax[i]?["word"]?.ToString() ?? "" : "";

                if (!string.IsNullOrEmpty(name))
                {
                    item.Append(name);
                    if (!string.IsNullOrEmpty(spec)) item.Append($" ({spec})");
                    if (!string.IsNullOrEmpty(unit)) item.Append($" {unit}");
                    if (!string.IsNullOrEmpty(qty)) item.Append($" ×{qty}");
                    if (!string.IsNullOrEmpty(price)) item.Append($" ¥{price}");
                    if (!string.IsNullOrEmpty(amount)) item.Append($" 金额:{amount}");
                    if (!string.IsNullOrEmpty(taxRate)) item.Append($" 税率:{taxRate}");
                    if (!string.IsNullOrEmpty(tax)) item.Append($" 税额:{tax}");

                    if (itemList.Length > 0) itemList.Append(" | ");
                    itemList.Append(item.ToString());
                }
            }

            if (itemList.Length > 0)
            {
                fields["商品明细"] = itemList.ToString();
                fields["商品条目数"] = maxItems.ToString();
            }

            return fields;
        }

        static void ExtractAllFields(Dictionary<string, string> fields, string text)
        {
            var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>
            {
                ["发票号码"] = new System.Text.RegularExpressions.Regex(@"发票号码[：:\s]*([A-Za-z0-9]{20,})"),
                ["开票日期"] = new System.Text.RegularExpressions.Regex(@"开票日期[：:\s]*(\d{4}年\d{1,2}月\d{1,2}日)"),
                ["价税合计小写"] = new System.Text.RegularExpressions.Regex(@"[（(]小写[）)]*[￥¥]?\s*([\d,]+\.?\d*)"),
                ["金额"] = new System.Text.RegularExpressions.Regex(@"金额[：:\s]*￥?\s*([\d,]+\.?\d*)"),
                ["税额"] = new System.Text.RegularExpressions.Regex(@"税额[：:\s]*￥?\s*([\d,]+\.?\d*)"),
                ["开票人"] = new System.Text.RegularExpressions.Regex(@"开票人[：:\s]*([^\s\n\r]+)"),
                ["备注"] = new System.Text.RegularExpressions.Regex(@"备注[：:\s]*([^\n\r]+)"),
                ["购买方税号"] = new System.Text.RegularExpressions.Regex(@"购买方[^\n\r]{0,20}([A-Za-z0-9]{15,20})"),
                ["销售方税号"] = new System.Text.RegularExpressions.Regex(@"销售方[^\n\r]{0,20}([A-Za-z0-9]{15,20})"),
                ["数量"] = new System.Text.RegularExpressions.Regex(@"数量[：:\s]*([\d.]+)"),
                ["单价"] = new System.Text.RegularExpressions.Regex(@"单价[：:\s]*￥?\s*([\d,.]+)"),
                ["税率"] = new System.Text.RegularExpressions.Regex(@"税率[：:\s]*([\d.%]+)")
            };

            foreach (var pattern in patterns)
            {
                var match = pattern.Value.Match(text);
                if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    string value = match.Groups[1].Value.Trim();
                    if (!fields.ContainsKey(pattern.Key))
                    {
                        fields[pattern.Key] = value;
                    }
                }
            }

            var productMatches = System.Text.RegularExpressions.Regex.Matches(text, @"\*([^\*\n\r]{4,60})");
            if (productMatches.Count > 0)
            {
                var products = new HashSet<string>();
                foreach (System.Text.RegularExpressions.Match m in productMatches)
                {
                    string product = m.Groups[1].Value.Trim();
                    if (product.Length > 3 && product.Length < 50)
                    {
                        if (!product.Contains("税率") && !product.Contains("金额") && !product.Contains("数量") &&
                            !product.Contains("单价") && !product.Contains("合计"))
                        {
                            products.Add(product);
                        }
                    }
                }

                if (products.Count > 0)
                {
                    fields["商品列表"] = string.Join(" | ", products.Take(10));
                }
            }

            if (!fields.ContainsKey("购买方名称"))
            {
                var buyerMatch = System.Text.RegularExpressions.Regex.Match(text, @"名称[：:\s]*([^统一信息备注税号\n\r]{5,30}公司)");
                if (buyerMatch.Success)
                {
                    string name = buyerMatch.Groups[1].Value.Trim();
                    if (!name.Contains("购买方") && !name.Contains("销售方"))
                        fields["购买方名称"] = name;
                }
            }

            if (!fields.ContainsKey("销售方名称"))
            {
                var sellerMatch = System.Text.RegularExpressions.Regex.Match(text, @"销售方.*?名称[：:\s]*([^统一信息备注\n\r]{5,30}公司)");
                if (sellerMatch.Success)
                    fields["销售方名称"] = sellerMatch.Groups[1].Value.Trim();
            }
        }

        static void PrintVatResult(JObject result, int pageNum)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"                    第 {pageNum} 页");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            var fields = ExtractVatFields(result);
            if (fields.HasValues)
            {
                Console.WriteLine("--- 增值税发票字段 ---");
                foreach (var prop in fields.Properties())
                {
                    string value = prop.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"{prop.Name}: {value}");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("未能解析VAT发票字段");
                Console.WriteLine($"原始响应: {result.ToString().Substring(0, Math.Min(500, result.ToString().Length))}");
            }
        }

        static void PrintPageResult(JObject result, int pageNum)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine($"                    第 {pageNum} 页");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            var mergedWords = result["merged_words"] as JObject;
            if (mergedWords != null)
            {
                Console.WriteLine("--- 合并后的关键信息 ---");
                foreach (var prop in mergedWords.Properties())
                {
                    string value = prop.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"{prop.Name}: {value}");
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine($"本页共 {result["words_result_num"] ?? 0} 行（合并后）");
        }

        static void PrintMergedResult(JObject result)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("                    识别结果（已合并折行）");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            var mergedWords = result["merged_words"] as JObject;
            if (mergedWords != null)
            {
                foreach (var prop in mergedWords.Properties())
                {
                    string value = prop.Value?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine($"{prop.Name}: {value}");
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine($"共 {result["words_result_num"] ?? 0} 行（合并后）");
        }

        static void SaveJson(JObject result, string filePath)
        {
            try
            {
                string jsonPath = Path.Combine(
                    Path.GetDirectoryName(filePath) ?? ".",
                    Path.GetFileNameWithoutExtension(filePath) + "_ocr.json"
                );

                string json = JsonConvert.SerializeObject(result, Formatting.Indented);
                File.WriteAllText(jsonPath, json, Encoding.UTF8);

                Console.WriteLine();
                Console.WriteLine($"JSON结果已保存: {Path.GetFullPath(jsonPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存JSON失败: {ex.Message}");
            }
        }

        static void SaveFormattedJson(JObject result, string filePath)
        {
            try
            {
                var wordsResult = result["vat_result"] as JObject;
                var words = wordsResult?["words_result"] as JObject;

                if (words == null)
                {
                    Console.WriteLine("无法格式化输出：未找到VAT识别结果");
                    return;
                }

                var formatted = new JObject();

                string invoiceType = words["InvoiceType"]?.ToString() ?? "";
                string invoiceTypeOrg = words["InvoiceTypeOrg"]?.ToString() ?? "";
                formatted["发票类型"] = string.IsNullOrEmpty(invoiceType) ? invoiceTypeOrg : invoiceType;

                formatted["发票号码"] = words["InvoiceNum"]?.ToString() ?? "";
                formatted["开票日期"] = words["InvoiceDate"]?.ToString() ?? "";

                var buyerInfo = new JObject();
                buyerInfo["名称"] = words["PurchaserName"]?.ToString() ?? "";
                buyerInfo["统一社会信用代码/纳税人识别号"] = words["PurchaserRegisterNum"]?.ToString() ?? "";
                formatted["购买方信息"] = buyerInfo;

                var sellerInfo = new JObject();
                sellerInfo["名称"] = words["SellerName"]?.ToString() ?? "";
                sellerInfo["统一社会信用代码/纳税人识别号"] = words["SellerRegisterNum"]?.ToString() ?? "";
                formatted["销售方信息"] = sellerInfo;

                var itemList = new JArray();
                var commodityName = words["CommodityName"] as JArray;
                var commodityType = words["CommodityType"] as JArray;
                var commodityUnit = words["CommodityUnit"] as JArray;
                var commodityNum = words["CommodityNum"] as JArray;
                var commodityPrice = words["CommodityPrice"] as JArray;
                var commodityAmount = words["CommodityAmount"] as JArray;
                var commodityTaxRate = words["CommodityTaxRate"] as JArray;
                var commodityTax = words["CommodityTax"] as JArray;

                int maxItems = 0;
                if (commodityName != null) maxItems = Math.Max(maxItems, commodityName.Count);
                if (commodityType != null) maxItems = Math.Max(maxItems, commodityType.Count);
                if (commodityUnit != null) maxItems = Math.Max(maxItems, commodityUnit.Count);
                if (commodityNum != null) maxItems = Math.Max(maxItems, commodityNum.Count);

                double totalAmount = 0;
                double totalTax = 0;

                for (int i = 0; i < maxItems; i++)
                {
                    var item = new JObject();

                    string name = commodityName != null && commodityName.Count > i ?
                        commodityName[i]?["word"]?.ToString() ?? "" : "";
                    string spec = commodityType != null && commodityType.Count > i ?
                        commodityType[i]?["word"]?.ToString() ?? "" : "";
                    string unit = commodityUnit != null && commodityUnit.Count > i ?
                        commodityUnit[i]?["word"]?.ToString() ?? "" : "";
                    string qty = commodityNum != null && commodityNum.Count > i ?
                        commodityNum[i]?["word"]?.ToString() ?? "" : "";
                    string price = commodityPrice != null && commodityPrice.Count > i ?
                        commodityPrice[i]?["word"]?.ToString() ?? "" : "";
                    string amount = commodityAmount != null && commodityAmount.Count > i ?
                        commodityAmount[i]?["word"]?.ToString() ?? "" : "";
                    string taxRate = commodityTaxRate != null && commodityTaxRate.Count > i ?
                        commodityTaxRate[i]?["word"]?.ToString() ?? "" : "";
                    string tax = commodityTax != null && commodityTax.Count > i ?
                        commodityTax[i]?["word"]?.ToString() ?? "" : "";

                    item["项目名称"] = name;
                    item["规格型号"] = spec;
                    item["单位"] = unit;

                    double qtyVal = 0;
                    if (double.TryParse(qty, out qtyVal)) item["数量"] = qtyVal;
                    else item["数量"] = qty;

                    double priceVal = 0;
                    if (double.TryParse(price, out priceVal)) item["单价"] = priceVal;

                    double amountVal = 0;
                    if (double.TryParse(amount, out amountVal))
                    {
                        item["金额"] = amountVal;
                        totalAmount += amountVal;
                    }

                    item["税率/征收率"] = taxRate;

                    double taxVal = 0;
                    if (double.TryParse(tax, out taxVal))
                    {
                        item["税额"] = taxVal;
                        totalTax += taxVal;
                    }

                    itemList.Add(item);
                }

                formatted["项目列表"] = itemList;

                var subtotal = new JObject();
                subtotal["金额"] = Math.Round(totalAmount, 2);
                subtotal["税额"] = Math.Round(totalTax, 2);
                formatted["小计"] = subtotal;

                var total = new JObject();
                total["小写"] = words["AmountInFiguers"]?.ToString() ?? "";
                total["大写"] = words["AmountInWords"]?.ToString() ?? "";
                formatted["合计"] = total;

                var totalAmountObj = new JObject();
                totalAmountObj["小写"] = words["AmountInFiguers"]?.ToString() ?? "";
                totalAmountObj["大写"] = words["AmountInWords"]?.ToString() ?? "";
                formatted["价税合计"] = totalAmountObj;

                formatted["备注"] = words["Remarks"]?.ToString() ?? "";

                string jsonPath = Path.Combine(
                    Path.GetDirectoryName(filePath) ?? ".",
                    Path.GetFileNameWithoutExtension(filePath) + "_fp.json"
                );

                string json = JsonConvert.SerializeObject(formatted, Formatting.Indented);
                File.WriteAllText(jsonPath, json, Encoding.UTF8);
                //File.WriteAllText(result, json, Encoding.UTF8);

                Console.WriteLine();
                Console.WriteLine($"格式化JSON已保存: {Path.GetFullPath(jsonPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存格式化JSON失败: {ex.Message}");
            }
        }

    }
}

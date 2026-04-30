namespace Demo.Main
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class ResumableDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _progressFileName = "process.json";
        private readonly string _tempExtension = ".tmp//";
        private readonly string _progressExtension = ".progress//";
        private readonly string _finalExtension = "uploades";
        /// <summary>
        /// 分片下载 每次下载1M
        /// </summary>
        private const long capacity = 1024 * 1024;
        public ResumableDownloader()
        {
            var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
        }

        /// <summary>
        /// 下载文件（支持断点续传）
        /// </summary>
        /// <param name="url">文件URL</param>
        /// <param name="progressCallback">进度回调（可选）</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        public async Task DownloadFileAsync(string url,IProgress<DownloadProgress> progressCallback = null,CancellationToken cancellationToken = default)
        {
            var fileName = url.Split('/').LastOrDefault();
            var finalPath = Directory.GetCurrentDirectory() + "//" + _finalExtension;
            if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);
            var zipFilePath = Path.Combine(finalPath, "1.zip");
            File.Create(zipFilePath);
            
            long total = 0;
            long download = 0;

            //当前文件是否支持断点续传
            bool isResume = false;
            
            try
            {
                var uniqueCode = 0l;

                // 发送HEAD请求获取文件信息
                using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    using (var response = await _httpClient.SendAsync(request, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        // 检查服务器是否支持断点续传
                        if (!response.Headers.AcceptRanges.Contains("bytes"))
                        {
                            Console.WriteLine("服务器不支持断点续传，将重新下载");
                            download = 0;
                            isResume = false;
                        }
                        else
                        {
                            total = response.Content.Headers.ContentLength ?? 0;
                            uniqueCode = response.Content.Headers.LastModified.Value.Ticks;
                        }
                    }
                }

                //临时文件夹路径:当前项目路径 + .temp + 文件修改时间
                var tempPath = Directory.GetCurrentDirectory() + "//" + _tempExtension + uniqueCode + "//";
                DateTime lastUpdateTime = DateTime.Now;
                // 检查是否已经存在部分下载的文件
                if (isResume)
                {
                    if (Directory.Exists(tempPath))
                    {
                        var files = Directory.GetFiles(tempPath);
                        download = files.Sum(x =>
                        {
                            var fs = new FileInfo(x).OpenRead();
                            var length = fs.Length;
                            fs.Close();
                            return length;
                        });
                        if (download > 0)
                        {
                            isResume = true;
                            Console.WriteLine($"检测到未完成的下载，已下载: {download} 字节");
                        }
                    }
                    else
                    {
                        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
                    }

                    //文件总大小除以每次请求文件大小 确定请求次数
                    var count = total / capacity + 1;
                    
                    for (int i = 0; i < count; i++)
                    {
                        //分片文件名称
                        var tempFileName = tempPath + i;
                        // 设置Range头进行断点续传
                        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                        {
                            using (var fileStream = File.Open(tempFileName, FileMode.OpenOrCreate))
                            {
                                //读取起点
                                long from = capacity * i;
                                //假如分片文件已存在 根据当前分片文件现有的字节数确定读取起点
                                if (fileStream.Length > 0)
                                {
                                    from = from + fileStream.Length;
                                }
                                //读取终点 
                                var to = capacity * (i + 1) - 1;

                                if (to - from > 0)
                                {
                                    //设置Range
                                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);
                                    try
                                    {
                                        //获取响应
                                        using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                                        {
                                            response.EnsureSuccessStatusCode();

                                            using (var stream = await response.Content.ReadAsStreamAsync())
                                            {
                                                byte[] buffer = new byte[to - from];

                                                //本次读取字节数
                                                int bytesRead;

                                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                                {
                                                    //从响应流读取数据
                                                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                                    download += bytesRead;

                                                    // 报告进度（每秒最多报告一次）
                                                    if (progressCallback != null && (DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
                                                    {
                                                        var progress = new DownloadProgress
                                                        {
                                                            BytesDownloaded = download,
                                                            TotalBytes = total,
                                                            ProgressPercentage = total > 0 ? (double)download / total * 100 : 0,

                                                        };
                                                        progressCallback.Report(progress);
                                                        lastUpdateTime = DateTime.Now;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {


                                    }
                                }
                            }
                        }
                    }

                    await FileMerge(tempPath, finalPath, fileName);
                }
                else
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        using (var fileStream = File.Open(Path.Combine(finalPath,fileName), FileMode.OpenOrCreate))
                        {
                            try
                            {
                                //获取响应
                                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                                {
                                    response.EnsureSuccessStatusCode();

                                    using (var stream = await response.Content.ReadAsStreamAsync())
                                    {
                                        byte[] buffer = new byte[capacity];

                                        //本次读取字节数
                                        int bytesRead;

                                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                        {
                                            //从响应流读取数据
                                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                            download += bytesRead;

                                            // 报告进度（每秒最多报告一次）
                                            if (progressCallback != null && (DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
                                            {
                                                var progress = new DownloadProgress
                                                {
                                                    BytesDownloaded = download,
                                                    TotalBytes = total,
                                                    ProgressPercentage = total > 0 ? (double)download / total * 100 : 0,

                                                };
                                                progressCallback.Report(progress);
                                                lastUpdateTime = DateTime.Now;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {


                            }
                        }
                    }
                }
                Console.WriteLine($"总字节数:{total} 已下载字节数:{download}");
                
                Console.WriteLine("下载完成！");

                // 发送最终进度报告
                if (progressCallback != null)
                {
                    var finalProgress = new DownloadProgress
                    {
                        BytesDownloaded = download,
                        TotalBytes = total,
                        ProgressPercentage = 100,
                        DownloadSpeed = 0
                    };
                    progressCallback.Report(finalProgress);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 文件下载
        /// </summary>
        /// <param name="url">文件下载链接</param>
        /// <param name="saveFileName">保存文件名</param>
        /// <param name="saveDirectoryPath">保存路径</param>
        /// <returns></returns>
        public async Task DownloadAsync(string url, string saveFileName,string saveDirectoryPath)
        {
            //如果目标目录不存在 先创建目录
            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            var fileName = url.Split('/').LastOrDefault();
            var finalPath = Directory.GetCurrentDirectory() + "//" + _finalExtension;
            //临时文件夹目录名称
            var tempDirectoryName = string.Empty;
            //当前文件是否支持断点续传
            bool isResume = false;
            //文件总字节数
            long total = 0;
            //已下载字节数
            long download = 0;

            var process = new DownloadProgress();
            try
            {
                // 发送HEAD请求获取文件信息 确定文件是否支持断点续传 以及文件大小
                using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    using (var response = await _httpClient.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();

                        // 检查服务器是否支持断点续传
                        if (!response.Headers.AcceptRanges.Contains("bytes"))
                        {
                            isResume = false;
                            total = 0;
                        }
                        else
                        {
                            isResume = true;
                            total = response.Content.Headers.ContentLength ?? 0;
                            tempDirectoryName = response.Content.Headers.LastModified.Value.Ticks.ToString();
                        }
                    }
                }

                //临时文件夹路径:当前项目路径 + .temp + 文件修改时间
                var tempDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), _tempExtension,tempDirectoryName);

                DateTime lastUpdateTime = DateTime.Now;
                // 检查是否已经存在部分下载的文件
                if (isResume)
                {
                    await DownChunksAsync(url, total, tempDirectoryPath, saveDirectoryPath, saveFileName);
                }
                else
                {
                    await DownAsync(url, total, tempDirectoryPath, saveDirectoryPath, saveFileName);
                }
                Console.WriteLine($"总字节数:{total} 已下载字节数:{download}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载出错: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DownChunksAsync(string url,long total,string tempDirectoryPath, string saveDirectoryPath, string saveFileName)
        {
            var download = 0l;
            //检查临时文件夹中是否存在未下载完成的任务
            if (Directory.Exists(tempDirectoryPath))
            {
                download = Directory.GetFiles(tempDirectoryPath).Sum(x =>
                {
                    var fs = new FileInfo(x).OpenRead();
                    var length = fs.Length;
                    fs.Close();
                    return length;
                });
                if (download > 0)
                {
                    Console.WriteLine($"检测到未完成的下载，已下载: {download} 字节");
                }
            }

            //文件总大小除以每次请求文件大小 确定请求次数
            var count = total / capacity + 1;
            for (int i = 0; i < count; i++)
            {
                //分片文件名称
                var tempFileName = Path.Combine(tempDirectoryPath ,i.ToString());
                // 设置Range头进行断点续传
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (var fileStream = File.Open(tempFileName, FileMode.OpenOrCreate))
                    {
                        //读取起点
                        long from = capacity * i;
                        //假如分片文件已存在 根据当前分片文件现有的字节数确定读取起点
                        if (fileStream.Length > 0)
                        {
                            from = from + fileStream.Length;
                        }
                        //读取终点 
                        var to = capacity * (i + 1) - 1;

                        if (to - from > 0)
                        {
                            //设置Range
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);
                            try
                            {
                                //获取响应
                                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                                {
                                    response.EnsureSuccessStatusCode();

                                    using (var stream = await response.Content.ReadAsStreamAsync())
                                    {
                                        byte[] buffer = new byte[to - from];

                                        //本次读取字节数
                                        int bytesRead;

                                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            //从响应流读取数据
                                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                                            download += bytesRead;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //记录日志
                            }
                        }
                    }
                }
            }

            return await FileMerge(tempDirectoryPath, saveDirectoryPath, saveFileName);
        }

        public async Task<bool> DownAsync(string url, long total, string tempDirectoryPath, string saveDirectoryPath, string saveFileName)
        {
            var download = 0l;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (var fileStream = File.Open(Path.Combine(tempDirectoryPath, saveFileName), FileMode.OpenOrCreate))
                    {
                        //获取响应
                        using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                byte[] buffer = new byte[capacity];

                                //本次读取字节数
                                int bytesRead;

                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    //从响应流读取数据
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    download += bytesRead;
                                }
                            }
                        }
                    }
                }

                //从临时文件目录复制到目标文件目录
                File.Copy(Path.Combine(tempDirectoryPath, saveFileName), Path.Combine(saveDirectoryPath, saveFileName));
                //删除临时文件目录中的文件
                File.Delete(Path.Combine(tempDirectoryPath, saveFileName));
            }
            catch (Exception ex) 
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 合并文件
        /// </summary>
        /// <param name="tempDirectoryPath"></param>
        /// <param name="savePathName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private async Task<bool> FileMerge(string tempDirectoryPath, string savePathName, string fileName)
        {
            var result = false;
            try
            {
                var tempFiles = Directory.GetFiles(tempDirectoryPath);//获得下面的所有文件
                var finalFilePath = Path.Combine(savePathName, fileName);//最终的文件名
                using (var stream = new FileStream(finalFilePath, FileMode.Create))
                {
                    tempFiles = tempFiles.OrderBy(x => int.Parse(x.Replace(tempDirectoryPath,string.Empty))).ToArray();
                    foreach (var item in tempFiles)
                    {
                        var bytes = System.IO.File.ReadAllBytes(item);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                        File.Delete(item);//删除分块
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //删除临时文件夹
                if (Directory.Exists(tempDirectoryPath))
                {
                    Directory.Delete(tempDirectoryPath);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 下载进度信息
    /// </summary>
    public class DownloadProgress
    {
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public long DownloadSpeed { get; set; } // 字节/秒
    }

}

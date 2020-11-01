using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using CheckWebsiteStatus.Configuration;
using CheckWebsiteStatus.SimpleLogger;
using HtmlAgilityPack;
using Quartz;

namespace CheckWebsiteStatus.Scheduler
{
    public class SchedulerJob : IJob
    {
        private static readonly ICLogger Logger = CLogger<SchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            //retreive the configuration and cast them directly
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];

            await Task.Run(async () =>
            {
                Logger.Log("Fire the scheduled event!");
                //Read the XML-Sitemap
                var retList = await RetrieveSitemapItems(configuration.SitemapUrl);

                Logger.Log($"Retrieve the sitemap with {retList.Count} items.");
                //Call each url in the list and print some header infos.
                Parallel.ForEach(retList, async url => await GetResultFromUrl(url));
            });
        }

        private async Task GetResultFromUrl(string url)
        {
            await Task.Run(async () =>
            {
                var response = await GetResponseFromUri(url,
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                var headers = response.Headers;
                var status = response.StatusCode;
                if (status != HttpStatusCode.OK) return true;


                var doc = new HtmlDocument();
                doc.Load(response.GetResponseStream());
                doc.OptionEmptyCollection = true;

                Logger.Log(
                    $"GET {url} :: {status.ToString()} : C {headers["cf-cache-status"]} : S {doc.Text.Length}");


                response.Close();
                response.Dispose();

                var documentDescendants = doc.DocumentNode.Descendants().ToList();

                var elementsImg = documentDescendants
                    .Where(node => node.Name.ToLower().TrimEnd().TrimStart() == "img")
                    .Where(node => node.Attributes.Contains("src"))
                    .Where(node => node.Attributes["src"].Value.StartsWith("https://www.dimatec.de"))
                    .ToList();

                var elementsJs = documentDescendants
                    .Where(node => node.Name.ToLower().TrimEnd().TrimStart() == "script")
                    .Where(node => node.Attributes.Contains("src"))
                    .Where(node => node.Attributes.Contains("type"))
                    .Where(node => node.Attributes["type"].Value.Contains("javascript"))
                    .Where(node => node.Attributes["src"].Value.StartsWith("https://www.dimatec.de"))
                    .ToList();


                var elementsCss = documentDescendants
                    .Where(node => node.Name.ToLower().TrimEnd().TrimStart() == "link")
                    .Where(node => node.Attributes.Contains("href"))
                    .Where(node => node.Attributes.Contains("rel"))
                    .Where(node => node.Attributes["rel"].Value == "stylesheet")
                    .Where(node => node.Attributes["href"].Value.StartsWith("https://www.dimatec.de"))
                    .ToList();


                var htmlNodes = (elementsImg.Concat(elementsJs).Concat(elementsCss)).ToList();

                Logger.Log(
                    $"[url]:{url} [i]:{elementsImg.Count} [c]:{elementsCss.Count} [j]:{elementsJs.Count}");

                foreach (var element in htmlNodes)
                {
                    switch (element.Name.ToLower().TrimEnd().TrimStart())
                    {
                        case "img":
                            var imgUrl = element.Attributes["src"].Value;
                            const string headerAcceptImages = "image/avif,image/webp,image/apng,image/*,*/*;q=0.8";
                            var responseImage = await GetResponseFromUri(imgUrl, headerAcceptImages);
                            if (responseImage.StatusCode == HttpStatusCode.OK)
                            {
                                var imgLength = getImageSize(responseImage.GetResponseStream());
                                Logger.Log(
                                    $"GET {imgUrl} : S {responseImage.StatusCode.ToString()} : C {responseImage.Headers["cf-cache-status"]} : L {imgLength}");
                            }

                            responseImage.Close();
                            responseImage.Dispose();
                            break;
                        case "link":
                            var cssUrl = element.Attributes["href"].Value;
                            const string headerAcceptCss = "text/css,*/*;q=0.1";
                            var responseCss = await GetResponseFromUri(cssUrl, headerAcceptCss);
                            if (responseCss.StatusCode == HttpStatusCode.OK)
                            {
                                var cssLength = getTextSize(responseCss.GetResponseStream());
                                Logger.Log(
                                    $"GET {cssUrl} : S {responseCss.StatusCode.ToString()} : C {responseCss.Headers["cf-cache-status"]} : L {cssLength}");
                            }

                            responseCss.Close();
                            responseCss.Dispose();
                            break;
                        case "script":
                            var jsUrl = element.Attributes["src"].Value;
                            const string headerAcceptJs = "*/*";
                            var responseJs = await GetResponseFromUri(jsUrl, headerAcceptJs);
                            if (responseJs.StatusCode == HttpStatusCode.OK)
                            {
                                var jsLength = getTextSize(responseJs.GetResponseStream());
                                Logger.Log(
                                    $"GET {jsUrl} : S {responseJs.StatusCode.ToString()} : C {responseJs.Headers["cf-cache-status"]} : L {jsLength}");
                            }

                            responseJs.Close();
                            responseJs.Dispose();
                            break;
                        default:
                            Logger.Log($"Unidentified element type <{element.Name}> found");
                            break;
                    }
                }


                return true;
            });
        }
        
        private long getImageSize(Stream responseStream)
        {
            var readStream = new BinaryReader(responseStream);
            using MemoryStream ms = new MemoryStream();
            var lnBuffer = readStream.ReadBytes(1024);
            while (lnBuffer.Length > 0)
            {
                ms.Write(lnBuffer,0,lnBuffer.Length);
                lnBuffer = readStream.ReadBytes(1024);
            }

            ms.Position = 0;
            return ms.Length;
        }

        private long getTextSize(Stream responseStream)
        {
            var readStream = new StreamReader(responseStream);
            return readStream.ReadToEnd().Length;
        }

        

        private async Task<HttpWebResponse> GetResponseFromUri(string uri, string acceptHeader)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.Accept = acceptHeader;
            return (HttpWebResponse) await request.GetResponseAsync();
        }

        private async Task<List<string>> RetrieveSitemapItems(string url)
        {
            return await Task.Run(() =>
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(new XmlTextReader(url));
                XmlNodeList xnList = xmlDocument.GetElementsByTagName("url");
                return (from XmlNode node in xnList select node["loc"].InnerText).ToList();
            });
        }
    }
}
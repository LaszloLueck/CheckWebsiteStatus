using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Xml;
using CheckWebsiteStatus.Configuration;
using CheckWebsiteStatus.SimpleLogger;
using HtmlAgilityPack;
using MoreLinq;
using Quartz;

namespace CheckWebsiteStatus.Scheduler
{
    public class SchedulerJob : IJob
    {
        private static readonly ICLogger Logger = CLogger<SchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            //retrieve the configuration and cast them directly
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];


            Logger.Log("Fire the scheduled event!");
            //Read the XML-Sitemap
            var retList = RetrieveSitemapItems(configuration.SitemapUrl);

            Logger.Log($"Retrieve the sitemap with {retList.Count} items.");
            //Call each url in the list and print some header infos.

            var parallelTasks = retList.AsParallel().Select(async url => await GetResultFromUrl(url));

            var htmlNodes = (await Task.WhenAll(parallelTasks))
                .SelectMany(x => x)
                .ToList();

            Logger.Log(
                $"Read entirely {htmlNodes.Count} sub-elements, now filter them!");


            var cssNodes = htmlNodes
                .Where(node => node.Attributes.Contains("href"))
                .DistinctBy(node => node.Attributes["href"].Value);

            var srcNodes = htmlNodes
                .Where(node => node.Attributes.Contains("src"))
                .DistinctBy(node => node.Attributes["src"].Value);

            var elementTasks = ReadAndProcessSubElements(cssNodes.Concat(srcNodes));

            await Task.WhenAll(elementTasks).ContinueWith(_ =>
            {
                Logger.Log("Finished Task");
            });
        }
        
        private IEnumerable<Task> ReadAndProcessSubElements(IEnumerable<HtmlNode> htmlNodes)
        {
            var nodes = htmlNodes.ToList();

            Logger.Log($"Process {nodes.Count()} sub-elements");
            const string headerAcceptImages = "image/avif,image/webp,image/apng,image/*,*/*;q=0.8";
            const string headerAcceptJs = "*/*";
            const string headerAcceptCss = "text/css,*/*;q=0.1";


            var tl = nodes.Select(htmlNode =>
            {
                return htmlNode.Name.ToLower() switch
                {
                    "img" => GetResponseForSubElement(htmlNode.Attributes["src"].Value, headerAcceptImages,
                        ElementType.Binary),
                    "link" => GetResponseForSubElement(htmlNode.Attributes["href"].Value, headerAcceptCss,
                        ElementType.Text),
                    "script" => GetResponseForSubElement(htmlNode.Attributes["src"].Value, headerAcceptJs,
                        ElementType.Text),
                    _ => Task.Run(() => false)
                };
            });
            return tl;
        }

        private enum ElementType
        {
            Text,
            Binary
        }

        private static async Task GetResponseForSubElement(string url, string acceptHeader, ElementType elementType)
        {

            var response = await GetResponseFromUri(url, acceptHeader);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var elementSize = elementType switch
                {
                    ElementType.Text => GetTextSize(response.GetResponseStream()),
                    ElementType.Binary => GetImageSize(response.GetResponseStream()),
                    _ => 0L
                };

                Logger.Log(
                    $"GET {url} : S {response.StatusCode.ToString()} : C {response.Headers["cf-cache-status"]} : L {elementSize}");
            }
            else
            {
                Logger.Log(
                    $"Could not get a valid ReturnCode from Element <{url}>. ReturnCode is {response.StatusCode}");
            }

            response.Close();
            response.Dispose();
        }

        private static async Task<IEnumerable<HtmlNode>> GetResultFromUrl(string url)
        {
            var response = await GetResponseFromUri(url,
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            var headers = response.Headers;
            var status = response.StatusCode;
            if (status != HttpStatusCode.OK) return new List<HtmlNode>();


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
                .Where(node => node.Attributes["src"].Value.StartsWith("https://www.dimatec.de"));

            var elementsJs = documentDescendants
                .Where(node => node.Name.ToLower().TrimEnd().TrimStart() == "script")
                .Where(node => node.Attributes.Contains("src"))
                .Where(node => node.Attributes.Contains("type"))
                .Where(node => node.Attributes["type"].Value.Contains("javascript"))
                .Where(node => node.Attributes["src"].Value.StartsWith("https://www.dimatec.de"));


            var elementsCss = documentDescendants
                .Where(node => node.Name.ToLower().TrimEnd().TrimStart() == "link")
                .Where(node => node.Attributes.Contains("href"))
                .Where(node => node.Attributes.Contains("rel"))
                .Where(node => node.Attributes["rel"].Value == "stylesheet")
                .Where(node => node.Attributes["href"].Value.StartsWith("https://www.dimatec.de"));

            return new List<IEnumerable<HtmlNode>> {elementsImg, elementsCss, elementsJs}.SelectMany(x => x);

        }

        private static long GetImageSize(Stream responseStream)
        {
            var readStream = new BinaryReader(responseStream);
            using MemoryStream ms = new MemoryStream();
            var lnBuffer = readStream.ReadBytes(1024);
            while (lnBuffer.Length > 0)
            {
                ms.Write(lnBuffer, 0, lnBuffer.Length);
                lnBuffer = readStream.ReadBytes(1024);
            }

            ms.Position = 0;
            return ms.Length;
        }

        private static long GetTextSize(Stream responseStream)
        {
            var readStream = new StreamReader(responseStream);
            return readStream.ReadToEnd().Length;
        }


        private static async Task<HttpWebResponse> GetResponseFromUri(string uri, string acceptHeader)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.Accept = acceptHeader;
            return (HttpWebResponse) await request.GetResponseAsync();
        }

        private static List<string> RetrieveSitemapItems(string url)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(new XmlTextReader(url));
            XmlNodeList xnList = xmlDocument.GetElementsByTagName("url");
            return (from XmlNode node in xnList select node["loc"].InnerText).ToList();

        }
    }
}
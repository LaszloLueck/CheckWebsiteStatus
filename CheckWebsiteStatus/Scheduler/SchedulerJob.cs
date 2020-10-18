using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using CheckWebsiteStatus.Configuration;
using CheckWebsiteStatus.SimpleLogger;
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
                //await Task.Run(() => {});
                Logger.Log("Fire the scheduled event!");
                //Read the XML-Sitemap
                var retList = await RetrieveSitemapItems(configuration.SitemapUrl);

                Logger.Log($"Retrieve the sitemap with {retList.Count} items.");
                //Call each url in the list and print some header infos.
                Parallel.ForEach(retList, async url => await GetResultFromUrl(url));
            });
        }

        private async Task<bool> GetResultFromUrl(string url)
        {
            return await Task.Run(async () =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Headers.Add("Accept", "text/html");
                HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
                var headers = response.Headers;
                var status = response.StatusCode;
                if (status == HttpStatusCode.OK)
                {
                    Logger.Log(
                        $"GET {url} returned: Status {status.ToString()} : Cache {headers["cf-cache-status"]} : via {headers["x-via"]} : server {headers["server"]} : edge {headers["cf-edge-cache"]}");
                }

                return true;
            });
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
using Abot.Crawler;
using Abot.Poco;
using AngleSharp.Dom.Html;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerTest
{
    class Program
    {
        // private List<IHtmlDocument> _angleSharpParsedHTML = new List<IHtmlDocument>();
        private List<HtmlDocument> _agiliyParsedHTML = new List<HtmlDocument>();
        private string uri = "https://www.totalrent.dk/shop/";
        private string domain = "https://www.totalrent.dk";
        static void Main(string[] args)
        {
            new Program().Run();

        }

        private void Run()
        {
            log4net.Config.XmlConfigurator.Configure();
            IWebCrawler crawler;
            Console.WriteLine("Start crawling");
            Console.ReadLine();
            Uri uriToCrawl = new Uri(uri);
            crawler = GetManuallyConfiguredWebCrawler();
            //This is where you process data about specific events of the crawl
            crawler.PageCrawlStartingAsync += Crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += Crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += Crawler_PageLinksCrawlDisallowed;

            CrawlResult result = crawler.Crawl(uriToCrawl);
            
            Console.WriteLine("Time Elapsed: " + result.Elapsed);
            Console.Write(Environment.NewLine + Environment.NewLine);
            
            foreach (var page in _agiliyParsedHTML)
            {
                foreach (HtmlNode node in page.DocumentNode.SelectNodes("//div[@class='col-sm-9 content']")){ 
                    string name = node
                    .SelectNodes("//span[@itemprop='name']")
                    .First()
                    .InnerText;

                    string productid = node
                    .SelectNodes("//span[@itemprop='productid']")
                    .First().InnerText;

                    string description = node
                    .SelectNodes("//div[@itemprop='description']")
                    .First().InnerText;

                    string imgurl = node
                    .Descendants("a").First(x => x.Attributes["class"] != null
                           && x.Attributes["class"].Value == "Thumbnail_Productinfo_FancyBox").Attributes["href"].Value;
                    
                    string price = node
                    .SelectNodes("//span[@itemprop='price']")
                    .First().InnerText;
                    Console.Clear();
                    Console.WriteLine($"Product ID: {productid}");
                    Console.WriteLine($"Name: {name}");
                    Console.WriteLine($"Description: {description}");
                    Console.WriteLine($"Price: {price}");
                    Console.WriteLine($"Img URL: {domain +imgurl}");
                }
            }
            Console.ReadLine();
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = 1;
            config.MaxPagesToCrawlPerDomain = 1;
            config.MinCrawlDelayPerDomainMilliSeconds = 1000;


            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("p.html") || pageToCrawl.Uri.AbsoluteUri.Contains("p.html"))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "Incorrect subdomain" };
            });


            crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            });

            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }

        void Crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }
        void Crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

            _agiliyParsedHTML.Add(crawledPage.HtmlDocument); //Html Agility Pack parser

            //_angleSharpParsedHTML.Add(crawledPage.AngleSharpHtmlDocument); //AngleSharp parser
        }

        void Crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
        }

        void Crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }

    }
}

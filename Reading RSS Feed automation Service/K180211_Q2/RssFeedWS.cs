using System;
using System.Xml;
using System.ServiceProcess;
using System.IO;
using System.Timers;
using System.ServiceModel.Syndication;

namespace K180211_Q2
{
    public partial class RssFeedWS : ServiceBase
    {
        Timer timeDelay;
        static public string rssfeedurl1 = System.Configuration.ConfigurationManager.AppSettings["RssFeedUrl1"];
        static public string rssfeedurl2 = System.Configuration.ConfigurationManager.AppSettings["RssFeedUrl2"];
        static public string FilePath = System.Configuration.ConfigurationManager.AppSettings["filepath"];
        static public string LogFilesPath = System.Configuration.ConfigurationManager.AppSettings["LogsFilePath"];  
        public RssFeedWS()
        {
            InitializeComponent();
            timeDelay = new Timer();
            timeDelay.Interval = 300000; //5 minutes = 300000 milliseconds
            timeDelay.Elapsed += new ElapsedEventHandler(RssFeedProcess);
        }

        protected override void OnStart(string[] args)
        {
            LogService("Service Starting");
            timeDelay.Enabled = true;
        }

        protected override void OnStop()
        {
            LogService("Service Stoping");
            timeDelay.Enabled = false;
        }

        private static void LogService(string content)
        {
            FileStream fs = new FileStream(LogFilesPath, FileMode.OpenOrCreate, FileAccess.Write);

            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }

        static public string ExtractNewsnameFromUri(Uri myuri) //extracts the news name present within uri
        {
            String uristring = myuri.ToString();
            var host = new System.Uri(uristring).Host.ToLower();
            string[] col = { ".com", ".pk", ".tv" };
            foreach (string name in col)
            {
                if (host.Contains(name))
                {
                    int index = host.IndexOf(name); 
                    int sec = host.Substring(0, index - 1).LastIndexOf('.');
                    var rootDomain = host.Substring(sec + 1);
                    string[] s = rootDomain.Split('.');
                    return s[0].ToString();
                }
            }
            return "";
        }
        public static void ReadAndUpdateRecords() // read the uri using syndication class and adds the newsitem into the newsitemcollection
        {
            Rss20FeedFormatter rssFormatter, rssFormatter2;

            //Gets xml from 1st news link
            using (var xmlReader = XmlReader.Create
                  (rssfeedurl1))
            {
                rssFormatter = new Rss20FeedFormatter();
                rssFormatter.ReadFrom(xmlReader);
            }

            //Gets xml from 2nd news link
            using (var xmlReader = XmlReader.Create
             (rssfeedurl2))
            {
                rssFormatter2 = new Rss20FeedFormatter();
                rssFormatter2.ReadFrom(xmlReader);

            }

            NewsItemCollection newscollection = new NewsItemCollection();

            //add feeds from first news link into collection
            foreach (var syndicationItem in rssFormatter.Feed.Items)
            {
                newscollection.NewsItems.Add(new NewsItem(syndicationItem.Title.Text,
                ExtractNewsnameFromUri(syndicationItem.Links[0].Uri),
                syndicationItem.PublishDate.DateTime,
                 syndicationItem.Summary.Text.Trim()
                ));

            }
            //add feeds from second news link into collection
            foreach (var syndicationItem in rssFormatter2.Feed.Items)
            {
                newscollection.NewsItems.Add(new NewsItem(syndicationItem.Title.Text,
                    ExtractNewsnameFromUri(syndicationItem.Links[0].Uri),
                    syndicationItem.PublishDate.DateTime,
                     syndicationItem.Summary.Text.Trim()
                    ));

            }

            //Sort the list of news item in descending order (by publication date)
            newscollection.NewsItems.Sort(delegate (NewsItem z, NewsItem y)
            {
                return y.PublicationDate.CompareTo(z.PublicationDate);
            });

            FileStream file = System.IO.File.Create(FilePath); //to be added in Appconfig
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(newscollection.GetType());
            x.Serialize(file, newscollection); //serialize and add objects to file

            file.Close();

        }
        public static void RssFeedProcess(object sender, ElapsedEventArgs e)
        {
            try
            {
                ReadAndUpdateRecords();             
            }
            catch (Exception ex)
            {
                LogService(ex.InnerException.Message);
                throw;
            }
        }
    }
}

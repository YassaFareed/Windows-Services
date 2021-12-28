using System.Collections.Generic;

namespace K180211_Q2
{
    public class NewsItemCollection
    {
        public List<NewsItem> NewsItems { get; set; }
        public NewsItemCollection()
        {
            NewsItems = new List<NewsItem>();
        }

    }
}

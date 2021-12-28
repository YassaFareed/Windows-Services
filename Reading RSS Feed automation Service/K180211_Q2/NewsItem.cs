using System;

namespace K180211_Q2
{

    [Serializable]
    public class NewsItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime PublicationDate { get; set; }
        public string NewsChannel { get; set; }


        public NewsItem()
        {

        }
        public NewsItem(string title, string newschannel, DateTime publicationdate, string description)
        {
            Title = title;
            Description = description;
            PublicationDate = publicationdate;
            NewsChannel = newschannel;
        }
    }
}

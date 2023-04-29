namespace RssBot.Models
{
    public class Feed
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public Feed(string url){
            // set name guid
            Name = Guid.NewGuid().ToString();
            Url = url;
        } 
    }
}
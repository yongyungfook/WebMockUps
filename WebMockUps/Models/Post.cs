namespace WebMockUps.Models
{
    public class Post
    {
        public string PostId { get; set; }
        public string PostTitle { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public List<string> PostCategoryIds { get; set; }
        public List<string> PostTagIds { get; set; }
        public string MediaPath { get; set; }
        public string SeoTitle { get; set; }
        public string MetaDescription { get; set; } 
        public DateTime PublishDate { get; set; }
        public List<string>? Categories { get; set; }
    }
}

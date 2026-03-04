namespace VandaliaCentral.Models
{
    public class ItNewsItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PostedOn { get; set; } = DateTime.UtcNow;
    }
}

namespace VandaliaCentral.Models
{
    public class FlappyScore
    {
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime AchievedOnUtc { get; set; }
    }
}

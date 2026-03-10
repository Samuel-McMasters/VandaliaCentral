namespace VandaliaCentral.Models
{
    public class FlappyLeaderboardSnapshot
    {
        public List<FlappyScore> Daily { get; set; } = new();
        public List<FlappyScore> Weekly { get; set; } = new();
        public List<FlappyScore> Monthly { get; set; } = new();
        public List<FlappyScore> AllTime { get; set; } = new();
    }
}

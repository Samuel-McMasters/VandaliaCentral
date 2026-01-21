using System.Security.Cryptography;

namespace VandaliaCentral.Services
{
    public class PasswordGeneratorService : IPasswordGeneratorService
    {
        
        // Can add/remove words anytime — just keep words <= 5 chars.
        private static readonly string[] Words =
        {
            "small","tree","cabin","brick","cloud","river","stone","brave","crisp","frost",
            "shade","spark","grape","panda","otter","eagle","tiger","zebra","maple","cider",
            "chair","table","light","night","quiet","swift","clear","north","south","pearl",
            "flint","steel","amber","coral","ember","honey","crown","daisy","lucky","witty",
            "mango","lemon","peach","berry","kinda","smile","proud","glory","grind","boost",
            "sharp","plain","fresh","slick","focus","align","pivot","stack","build","track"
        };

        private static readonly char[] Specials = "!@#$%&*?.".ToCharArray();

        public string GeneratePassword()
        {
            // pick 2 different words (optional; easy to allow duplicates if you want)
            var w1 = Pick(Words);
            string w2;
            do { w2 = Pick(Words); } while (w2 == w1);

            var digit = RandomNumberGenerator.GetInt32(0, 10); // 0-9
            var special = Pick(Specials);

            return $"{Title(w1)}{Title(w2)}{digit}{special}";
        }

        private static T Pick<T>(T[] items)
        {
            var i = RandomNumberGenerator.GetInt32(0, items.Length);
            return items[i];
        }

        private static string Title(string w)
        {
            if (string.IsNullOrWhiteSpace(w)) return w;
            if (w.Length == 1) return w.ToUpperInvariant();
            return char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
        }
    }
}

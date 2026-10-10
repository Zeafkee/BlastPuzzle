namespace BlastPuzzle.UI
{
    public static class NumberStrings
    {
        private const int CacheSize = 1000;
        private static readonly string[] Cache = Build();

        public static string Get(int value) =>
            value >= 0 && value < CacheSize ? Cache[value] : value.ToString();

        private static string[] Build()
        {
            var cache = new string[CacheSize];
            for (int i = 0; i < CacheSize; i++) cache[i] = i.ToString();
            return cache;
        }
    }
}

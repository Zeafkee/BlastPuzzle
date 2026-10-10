namespace BlastPuzzle.Core
{
    public interface IRandomSource
    {
        int Next(int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random _random;

        public SystemRandomSource(int seed) => _random = new System.Random(seed);
        public SystemRandomSource() => _random = new System.Random();

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}

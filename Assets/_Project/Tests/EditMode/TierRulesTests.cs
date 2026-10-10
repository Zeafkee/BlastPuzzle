using NUnit.Framework;

namespace BlastPuzzle.Core.Tests
{
    public class TierRulesTests
    {
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(7, 1)]
        [TestCase(8, 2)]
        [TestCase(9, 2)]
        [TestCase(10, 3)]
        [TestCase(42, 3)]
        public void GetTier_UsesStrictlyGreaterThanThresholds(int groupSize, int expectedTier)
        {
            var rules = new TierRules(4, 7, 9);
            Assert.AreEqual(expectedTier, rules.GetTier(groupSize));
        }

        [TestCase(0, 1, 2)]
        [TestCase(4, 4, 9)]
        [TestCase(4, 7, 7)]
        [TestCase(5, 3, 9)]
        public void Constructor_RejectsInvalidThresholds(int a, int b, int c)
        {
            Assert.Throws<System.ArgumentException>(() => new TierRules(a, b, c));
        }
    }
}

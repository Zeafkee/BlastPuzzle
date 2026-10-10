using System;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
#endif

namespace BlastPuzzle.Core.Tests
{
    public class AllocationTests
    {
        [Test]
        public void Turns_DoNotAllocate_AfterWarmUp()
        {
            var config = new LevelConfig
            {
                Width = 10,
                Height = 10,
                ColorCount = 6,
                Moves = 100000,
                Tiers = new TierRules(4, 6, 8),
                Goals = new[] { GoalConfig.CollectColor(0, int.MaxValue) },
            };
            var game = new BlastGame(config, new SystemRandomSource(42));
            var result = new TurnResult();

            for (int i = 0; i < 2000; i++)
                PlayOneTurn(game, result);

#if UNITY_5_3_OR_NEWER
            Assert.That(() =>
            {
                for (int i = 0; i < 500; i++)
                    PlayOneTurn(game, result);
            }, Is.Not.AllocatingGCMemory());
#else
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 500; i++)
                PlayOneTurn(game, result);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, allocated, "bytes allocated over 500 turns");
#endif
        }

        private static void PlayOneTurn(BlastGame game, TurnResult result)
        {
            if (!game.TryGetHint(out GridPos pos) || !game.Tap(pos, result))
                Assert.Fail("board has no valid move");
        }
    }
}

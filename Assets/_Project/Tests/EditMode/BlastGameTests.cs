using System.Linq;
using NUnit.Framework;

namespace BlastPuzzle.Core.Tests
{
    public class BlastGameTests
    {
        private static BlastGame Game(string[] layout, int colors = 6, int moves = 10, int seed = 1, params GoalConfig[] goals)
        {
            var config = new LevelConfig
            {
                Width = layout[0].Length,
                Height = layout.Length,
                ColorCount = colors,
                Moves = moves,
                Tiers = new TierRules(4, 6, 8),
                Goals = goals,
                Layout = layout,
            };
            return new BlastGame(config, new SystemRandomSource(seed));
        }

        private static string[] Rows(params string[] rows) => rows;

        [Test]
        public void Tap_OnGroup_ClearsItAndSpendsOneMove()
        {
            var game = Game(Rows(
                "0012",
                "3012",
                "3451"));
            var result = new TurnResult();

            Assert.IsTrue(game.Tap(new GridPos(0, 2), result));

            Assert.AreEqual(9, game.MovesLeft);
            Assert.AreEqual(1, result.Steps.Count);
            Assert.AreEqual(StepKind.Group, result.Steps[0].Kind);
            Assert.AreEqual(3, result.Cleared.Count);
            Assert.IsFalse(result.BoosterCreated);
            Assert.AreEqual(3, result.Spawns.Count);
            for (int i = 0; i < game.Board.Count; i++)
                Assert.IsFalse(game.Board[i].IsEmpty);
        }

        [Test]
        public void Tap_OnSingleBlock_IsRejectedAndChangesNothing()
        {
            var game = Game(Rows("0012", "3012", "3451"));
            string before = game.Board.ToString();

            Assert.IsFalse(game.Tap(new GridPos(3, 0), new TurnResult()));

            Assert.AreEqual(10, game.MovesLeft);
            Assert.AreEqual(before, game.Board.ToString());
        }

        [Test]
        public void Tap_OnBox_IsRejected()
        {
            var game = Game(Rows("#012", "0012", "3451"));
            Assert.IsFalse(game.Tap(new GridPos(0, 2), new TurnResult()));
        }

        [TestCase(4, TileKind.Color)]
        [TestCase(5, TileKind.Rocket)]
        [TestCase(6, TileKind.Rocket)]
        [TestCase(7, TileKind.Bomb)]
        [TestCase(8, TileKind.Bomb)]
        [TestCase(9, TileKind.Disco)]
        public void Tap_OnBigGroup_CreatesBoosterAtTappedCell(int groupSize, TileKind expected)
        {
            string bottom = new string('0', groupSize) + new string('1', 10 - groupSize).Replace("11", "12");
            bottom = bottom.Substring(0, 10);
            var game = Game(Rows("2323232323", bottom));
            var result = new TurnResult();

            Assert.IsTrue(game.Tap(new GridPos(0, 0), result));

            Assert.AreEqual(expected != TileKind.Color, result.BoosterCreated);
            if (expected != TileKind.Color)
            {
                Assert.AreEqual(expected, result.BoosterTile.Kind);
                Assert.AreEqual(new GridPos(0, 0), result.BoosterPos);
                Assert.AreEqual(expected, game.Board[0, 0].Kind, "booster stays at the bottom after gravity");
                if (expected == TileKind.Disco) Assert.AreEqual(0, game.Board[0, 0].Color);
            }
        }

        [Test]
        public void Rocket_ClearsItsWholeRow()
        {
            var game = Game(Rows(
                "01230",
                "12-01",
                "23012"));
            var result = new TurnResult();

            Assert.IsTrue(game.Tap(new GridPos(2, 1), result));

            Assert.AreEqual(StepKind.Rocket, result.Steps[0].Kind);
            CollectionAssert.AreEquivalent(
                new[] { new GridPos(0, 1), new GridPos(1, 1), new GridPos(3, 1), new GridPos(4, 1) },
                result.Cleared.Select(c => c.Pos));
            Assert.AreEqual(9, game.MovesLeft);
        }

        [Test]
        public void VerticalRocket_ClearsItsWholeColumn()
        {
            var game = Game(Rows(
                "012",
                "1|0",
                "201"));
            var result = new TurnResult();

            game.Tap(new GridPos(1, 1), result);

            CollectionAssert.AreEquivalent(
                new[] { new GridPos(1, 0), new GridPos(1, 2) },
                result.Cleared.Select(c => c.Pos));
        }

        [Test]
        public void Bomb_ClearsDiamondOfRadiusTwo()
        {
            var game = Game(Rows(
                "0101010",
                "1010101",
                "0101010",
                "101*101",
                "0101010",
                "1010101",
                "0101010"));
            var result = new TurnResult();

            game.Tap(new GridPos(3, 3), result);

            Assert.AreEqual(12, result.Cleared.Count, "13-cell diamond minus the bomb itself");
            foreach (var cleared in result.Cleared)
            {
                int distance = System.Math.Abs(cleared.Pos.X - 3) + System.Math.Abs(cleared.Pos.Y - 3);
                Assert.LessOrEqual(distance, 2);
            }
        }

        [Test]
        public void Disco_ClearsEveryBlockOfItsColor()
        {
            var game = Game(Rows(
                "2120",
                "1c12",
                "2021"));
            var result = new TurnResult();

            game.Tap(new GridPos(1, 1), result);

            Assert.AreEqual(5, result.Cleared.Count);
            Assert.IsTrue(result.Cleared.All(c => c.Tile.IsColor && c.Tile.Color == 2));
        }

        [Test]
        public void Booster_HitByAnotherBooster_GoesOffInTheSameTurn()
        {
            var game = Game(Rows(
                "012|0",
                "12021",
                "0-2*1",
                "12021",
                "01210"));
            var result = new TurnResult();

            game.Tap(new GridPos(1, 2), result);

            Assert.AreEqual(9, game.MovesLeft, "a chain is still one move");
            CollectionAssert.AreEqual(
                new[] { StepKind.Rocket, StepKind.Bomb, StepKind.Rocket },
                result.Steps.Select(s => s.Kind).ToArray(),
                "rocket hits the bomb, the bomb's blast reaches the vertical rocket above");
        }

        [Test]
        public void Box_NextToBlast_TakesOneHit_AndReinforcedBoxSurvives()
        {
            var game = Game(Rows(
                "1212",
                "#00@",
                "1212"));
            var result = new TurnResult();

            game.Tap(new GridPos(1, 1), result);

            Assert.IsTrue(result.Cleared.Any(c => c.Tile.IsBox && c.Pos == new GridPos(0, 1)), "1-hit box destroyed");
            Assert.AreEqual(1, result.Damaged.Count);
            Assert.AreEqual(new GridPos(3, 1), result.Damaged[0].Pos);
            Assert.AreEqual(Tile.Box(1), result.Damaged[0].After);
        }

        [Test]
        public void Goals_CountDown_AndWinWhenAllReachZero()
        {
            var game = Game(Rows("0012", "3012", "3451"), goals: new[]
            {
                GoalConfig.CollectColor(0, 3),
            });
            var result = new TurnResult();

            game.Tap(new GridPos(0, 2), result);

            Assert.AreEqual(0, game.GoalRemaining[0]);
            Assert.AreEqual(GameOutcome.Won, game.Outcome);
            Assert.AreEqual(GameOutcome.Won, result.Outcome);
            Assert.IsFalse(game.Tap(new GridPos(1, 0), result), "no moves after the game ended");
        }

        [Test]
        public void BoxGoal_CountsDestroyedBoxes()
        {
            var game = Game(Rows("1212", "#00@", "1212"), goals: new[] { GoalConfig.DestroyBoxes(2) });

            game.Tap(new GridPos(1, 1), new TurnResult());

            Assert.AreEqual(1, game.GoalRemaining[0]);
            Assert.AreEqual(GameOutcome.Playing, game.Outcome);
        }

        [Test]
        public void RunningOutOfMoves_LosesTheLevel()
        {
            var game = Game(Rows("0012", "3012", "3451"), moves: 1, goals: new[] { GoalConfig.CollectColor(5, 50) });

            game.Tap(new GridPos(0, 2), new TurnResult());

            Assert.AreEqual(GameOutcome.Lost, game.Outcome);
        }

        [Test]
        public void Game_NeverLeavesADeadBoard()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var config = new LevelConfig { Width = 4, Height = 4, ColorCount = 6, Moves = 30 };
                var random = new SystemRandomSource(seed);
                var game = new BlastGame(config, random);
                var result = new TurnResult();

                Assert.IsTrue(GroupFinder.HasAnyMove(game.Board), $"seed {seed}: dead start board");
                while (game.Outcome == GameOutcome.Playing)
                {
                    Assert.IsTrue(game.TryGetHint(out GridPos hint), $"seed {seed}: no hint\n{game.Board}");
                    Assert.IsTrue(game.Tap(hint, result));
                    Assert.IsTrue(GroupFinder.HasAnyMove(game.Board), $"seed {seed}: dead board\n{game.Board}");
                }
            }
        }

        [Test]
        public void SameSeed_PlaysOutIdentically()
        {
            string Play(int seed)
            {
                var config = new LevelConfig { Width = 7, Height = 8, ColorCount = 4, Moves = 15 };
                var random = new SystemRandomSource(seed);
                var game = new BlastGame(config, random);
                new AutoPlayer(random, 1f).Play(game);
                return game.Board + "|" + game.Score;
            }

            Assert.AreEqual(Play(5), Play(5));
            Assert.AreNotEqual(Play(5), Play(6));
        }

        [Test]
        public void GetTier_FollowsGroupSize()
        {
            var game = Game(Rows("2323232323", "0000012121"));

            Assert.AreEqual(1, game.GetTier(game.Board.ToIndex(0, 0)), "group of 5 > A=4");
            Assert.AreEqual(0, game.GetTier(game.Board.ToIndex(0, 1)));
        }

        [TestCase(10, 20, 3)]
        [TestCase(6, 20, 3)]
        [TestCase(3, 20, 2)]
        [TestCase(2, 20, 1)]
        [TestCase(0, 20, 1)]
        public void Stars_DependOnMovesLeft(int movesLeft, int total, int expected)
        {
            Assert.AreEqual(expected, StarRules.Calculate(movesLeft, total));
        }
    }
}

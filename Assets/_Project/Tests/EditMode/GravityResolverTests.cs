using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BlastPuzzle.Core.Tests
{
    public class GravityResolverTests
    {
        [Test]
        public void Collapse_MovesTilesDownAndKeepsOrder()
        {
            var board = BoardText.Parse(
                "0.1",
                "..2",
                "3..");
            var moves = new List<TileMove>();

            new GravityResolver().Collapse(board, moves);

            Assert.AreEqual(BoardText.Format(BoardText.Parse(
                "...",
                "0.1",
                "3.2")), board.ToString());

            CollectionAssert.AreEquivalent(new[]
            {
                new TileMove(new GridPos(0, 2), new GridPos(0, 1)),
                new TileMove(new GridPos(2, 1), new GridPos(2, 0)),
                new TileMove(new GridPos(2, 2), new GridPos(2, 1)),
            }, moves);
        }

        [Test]
        public void Collapse_OnFullBoard_DoesNothing()
        {
            var board = BoardText.Parse("01", "23");
            var moves = new List<TileMove>();

            new GravityResolver().Collapse(board, moves);

            Assert.IsEmpty(moves);
            Assert.AreEqual("01\n23", board.ToString());
        }

        [Test]
        public void Refill_FillsEveryEmptyCellWithValidColors()
        {
            var board = BoardText.Parse(
                "...",
                "0.1",
                "3.2");
            var spawns = new List<TileSpawn>();

            new GravityResolver().Refill(board, new SystemRandomSource(1), 4, spawns);

            Assert.AreEqual(5, spawns.Count);
            for (int i = 0; i < board.Count; i++)
            {
                Assert.IsFalse(board[i].IsEmpty, $"cell {board.ToPos(i)} still empty");
                Assert.Less(board[i].Color, 4);
            }
            foreach (var spawn in spawns)
                Assert.AreEqual(spawn.Tile, board[spawn.To]);
        }

        [Test]
        public void Refill_StacksNewTilesAboveBoard_SoEachColumnFallsTogether()
        {
            var board = BoardText.Parse(
                "...",
                "0.1",
                "3.2");
            var spawns = new List<TileSpawn>();

            new GravityResolver().Refill(board, new SystemRandomSource(1), 4, spawns);

            foreach (var column in spawns.GroupBy(s => s.To.X))
            {
                var ordered = column.OrderBy(s => s.To.Y).ToList();
                int dropDistance = ordered[0].StartY - ordered[0].To.Y;

                for (int i = 0; i < ordered.Count; i++)
                {
                    Assert.AreEqual(board.Height + i, ordered[i].StartY, $"column {column.Key}");
                    Assert.AreEqual(dropDistance, ordered[i].StartY - ordered[i].To.Y, $"column {column.Key}");
                }
            }
        }

        [Test]
        public void Refill_IsDeterministicForSameSeed()
        {
            var a = BoardText.Parse("...", "...", "...");
            var b = BoardText.Parse("...", "...", "...");
            var resolver = new GravityResolver();

            resolver.Refill(a, new SystemRandomSource(123), 6, new List<TileSpawn>());
            resolver.Refill(b, new SystemRandomSource(123), 6, new List<TileSpawn>());

            Assert.AreEqual(a.ToString(), b.ToString());
        }
    }
}

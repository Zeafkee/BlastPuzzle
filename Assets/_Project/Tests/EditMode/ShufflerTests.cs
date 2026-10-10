using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BlastPuzzle.Core.Tests
{
    public class ShufflerTests
    {
        private static int[] ColorHistogram(Board board)
        {
            var histogram = new int[10];
            for (int i = 0; i < board.Count; i++)
                histogram[board[i].Color]++;
            return histogram;
        }

        [Test]
        public void Shuffle_ResolvesDeadlock_ForManySeeds()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var board = BoardText.Parse(
                    "0101",
                    "1010",
                    "0101",
                    "1010");
                Assume.That(new GroupFinder(board).HasAnyGroup(), Is.False);

                new Shuffler(new SystemRandomSource(seed)).Shuffle(board, new List<TileMove>());

                Assert.IsTrue(new GroupFinder(board).HasAnyGroup(), $"seed {seed}\n{board}");
            }
        }

        [Test]
        public void Shuffle_PreservesColorCounts()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var board = BoardText.Parse(
                    "01234",
                    "12340",
                    "23401");
                var before = ColorHistogram(board);

                new Shuffler(new SystemRandomSource(seed)).Shuffle(board, new List<TileMove>());

                CollectionAssert.AreEqual(before, ColorHistogram(board), $"seed {seed}");
            }
        }

        [Test]
        public void Shuffle_ReportsMovesAsPermutation()
        {
            var original = BoardText.Parse("012", "120", "201");
            var board = BoardText.Parse("012", "120", "201");
            var moves = new List<TileMove>();

            new Shuffler(new SystemRandomSource(7)).Shuffle(board, moves);

            Assert.AreEqual(moves.Count, moves.Select(m => m.From).Distinct().Count(), "a cell moved twice");
            Assert.AreEqual(moves.Count, moves.Select(m => m.To).Distinct().Count(), "two tiles landed on one cell");
            foreach (var move in moves)
            {
                Assert.AreNotEqual(move.From, move.To, "non-moves should not be reported");
                Assert.AreEqual(original[move.From], board[move.To], $"{move}");
            }
        }

        [Test]
        public void Shuffle_WhenNoColorRepeats_StillCreatesAGroup()
        {
            var board = BoardText.Parse("01", "23");

            new Shuffler(new SystemRandomSource(3)).Shuffle(board, new List<TileMove>());

            Assert.IsTrue(new GroupFinder(board).HasAnyGroup(), board.ToString());
        }

        [Test]
        public void Shuffle_IsDeterministicForSameSeed()
        {
            var a = BoardText.Parse("0101", "1010", "0101");
            var b = BoardText.Parse("0101", "1010", "0101");

            new Shuffler(new SystemRandomSource(99)).Shuffle(a, new List<TileMove>());
            new Shuffler(new SystemRandomSource(99)).Shuffle(b, new List<TileMove>());

            Assert.AreEqual(a.ToString(), b.ToString());
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BlastPuzzle.Core.Tests
{
    public class GroupFinderTests
    {
        private static HashSet<GridPos> Set(params (int x, int y)[] cells) =>
            new HashSet<GridPos>(cells.Select(c => new GridPos(c.x, c.y)));

        [Test]
        public void FindGroup_ReturnsAllOrthogonallyConnectedSameColor()
        {
            var board = BoardText.Parse("001", "021", "311");
            var finder = new GroupFinder(board);
            var result = new List<GridPos>();

            int size = finder.FindGroup(new GridPos(0, 2), result);

            Assert.AreEqual(3, size);
            CollectionAssert.AreEquivalent(Set((0, 2), (1, 2), (0, 1)), result);
        }

        [Test]
        public void FindGroup_DoesNotConnectDiagonally()
        {
            var board = BoardText.Parse("01", "10");
            var finder = new GroupFinder(board);
            var result = new List<GridPos>();

            Assert.AreEqual(1, finder.FindGroup(new GridPos(0, 0), result));
        }

        [Test]
        public void FindGroup_DoesNotWrapAroundRowEdges()
        {
            var board = BoardText.Parse(
                "122",
                "001");
            var finder = new GroupFinder(board);
            var result = new List<GridPos>();

            Assert.AreEqual(1, finder.FindGroup(new GridPos(2, 0), result));
            Assert.AreEqual(1, finder.FindGroup(new GridPos(0, 1), result));
        }

        [Test]
        public void FindGroup_OnEmptyCell_ReturnsZero()
        {
            var board = BoardText.Parse("0.", "00");
            var finder = new GroupFinder(board);
            var result = new List<GridPos> { new GridPos(9, 9) };

            Assert.AreEqual(0, finder.FindGroup(new GridPos(1, 1), result));
            Assert.IsEmpty(result);
        }

        [Test]
        public void FindGroup_CanBeCalledRepeatedly()
        {
            var board = BoardText.Parse("001", "021", "311");
            var finder = new GroupFinder(board);
            var result = new List<GridPos>();

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(4, finder.FindGroup(new GridPos(2, 0), result));
                Assert.AreEqual(3, finder.FindGroup(new GridPos(0, 2), result));
            }
        }

        [Test]
        public void ComputeGroupSizes_WritesGroupSizeForEveryCell()
        {
            var board = BoardText.Parse(
                "001",
                "021",
                "311");
            var finder = new GroupFinder(board);
            var sizes = new int[board.Count];

            finder.ComputeGroupSizes(sizes);

            Assert.AreEqual(3, sizes[board.ToIndex(0, 2)]);
            Assert.AreEqual(3, sizes[board.ToIndex(0, 1)]);
            Assert.AreEqual(1, sizes[board.ToIndex(1, 1)]);
            Assert.AreEqual(1, sizes[board.ToIndex(0, 0)]);
            Assert.AreEqual(4, sizes[board.ToIndex(1, 0)]);
            Assert.AreEqual(4, sizes[board.ToIndex(2, 2)]);
        }

        [Test]
        public void ComputeGroupSizes_EmptyCellIsZero()
        {
            var board = BoardText.Parse("0.", "00");
            var finder = new GroupFinder(board);
            var sizes = new int[board.Count];

            finder.ComputeGroupSizes(sizes);

            Assert.AreEqual(0, sizes[board.ToIndex(1, 1)]);
            Assert.AreEqual(3, sizes[board.ToIndex(0, 0)]);
        }

        [Test]
        public void HasAnyGroup_FalseOnCheckerboard()
        {
            var board = BoardText.Parse("010", "101", "010");
            Assert.IsFalse(new GroupFinder(board).HasAnyGroup());
        }

        [Test]
        public void HasAnyGroup_TrueForSingleVerticalPair()
        {
            var board = BoardText.Parse("012", "310", "234");
            Assert.IsTrue(new GroupFinder(board).HasAnyGroup());
        }

        [Test]
        public void HasAnyGroup_TrueForSingleHorizontalPairInTopRightCorner()
        {
            var board = BoardText.Parse("011", "234", "402");
            Assert.IsTrue(new GroupFinder(board).HasAnyGroup());
        }

        [Test]
        public void HasAnyGroup_IgnoresAdjacentEmptyCells()
        {
            var board = BoardText.Parse("..", "01");
            Assert.IsFalse(new GroupFinder(board).HasAnyGroup());
        }
    }
}

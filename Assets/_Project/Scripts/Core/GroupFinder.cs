using System;
using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public sealed class GroupFinder
    {
        public const int MinGroupSize = 2;

        private readonly Board _board;

        private readonly int[] _visitStamp;
        private int _currentStamp;

        private readonly int[] _stack;

        private readonly int[] _component;

        public GroupFinder(Board board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _visitStamp = new int[board.Count];
            _stack = new int[board.Count];
            _component = new int[board.Count];
        }

        public int FindGroup(GridPos start, List<GridPos> result)
        {
            result.Clear();
            if (!_board.InBounds(start)) return 0;

            _currentStamp++;
            int size = FloodFill(_board.ToIndex(start));
            for (int i = 0; i < size; i++)
                result.Add(_board.ToPos(_component[i]));
            return size;
        }

        public void ComputeGroupSizes(int[] groupSizes)
        {
            _currentStamp++;

            for (int i = 0; i < _board.Count; i++)
            {
                if (!_board[i].IsColor)
                {
                    groupSizes[i] = 0;
                    continue;
                }

                if (_visitStamp[i] == _currentStamp) continue;

                int size = FloodFill(i);
                for (int k = 0; k < size; k++)
                    groupSizes[_component[k]] = size;
            }
        }

        public bool HasAnyGroup() => HasAnyGroup(_board);

        public static bool HasAnyGroup(Board board)
        {
            int width = board.Width;
            int height = board.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Tile tile = board[x, y];
                    if (!tile.IsColor) continue;
                    if (x + 1 < width && tile.Matches(board[x + 1, y])) return true;
                    if (y + 1 < height && tile.Matches(board[x, y + 1])) return true;
                }
            }

            return false;
        }

        public static bool HasAnyMove(Board board)
        {
            for (int i = 0; i < board.Count; i++)
                if (board[i].IsBooster) return true;
            return HasAnyGroup(board);
        }

        private int FloodFill(int startIndex)
        {
            Tile target = _board[startIndex];
            if (!target.IsColor) return 0;

            int width = _board.Width;
            int height = _board.Height;
            int stackSize = 0;
            int count = 0;

            _visitStamp[startIndex] = _currentStamp;
            _stack[stackSize++] = startIndex;

            while (stackSize > 0)
            {
                int index = _stack[--stackSize];
                _component[count++] = index;

                int x = index % width;
                int y = index / width;

                if (x > 0) Visit(index - 1);
                if (x < width - 1) Visit(index + 1);
                if (y > 0) Visit(index - width);
                if (y < height - 1) Visit(index + width);
            }

            return count;

            void Visit(int neighbour)
            {
                if (_visitStamp[neighbour] == _currentStamp) return;
                if (!_board[neighbour].Matches(target)) return;
                _visitStamp[neighbour] = _currentStamp;
                _stack[stackSize++] = neighbour;
            }
        }
    }
}

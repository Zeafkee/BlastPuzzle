using System;
using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public sealed class Shuffler
    {
        private readonly IRandomSource _random;

        private int[] _cells = Array.Empty<int>();
        private int[] _from = Array.Empty<int>();
        private Tile[] _original = Array.Empty<Tile>();
        private int[] _slotOf = Array.Empty<int>();
        private readonly int[] _histogram = new int[256];

        public Shuffler(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool LastShuffleRecolored { get; private set; }

        public bool Shuffle(Board board, List<TileMove> moves)
        {
            LastShuffleRecolored = false;
            EnsureCapacity(board.Count);

            int count = 0;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].IsColor)
                {
                    _slotOf[i] = count;
                    _cells[count] = i;
                    _original[count] = board[i];
                    _from[count] = count;
                    count++;
                }
                else
                {
                    _slotOf[i] = -1;
                }
            }

            if (count < 2) return false;

            for (int i = count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_from[i], _from[j]) = (_from[j], _from[i]);
            }

            for (int slot = 0; slot < count; slot++)
                board[_cells[slot]] = _original[_from[slot]];

            bool solved = GroupFinder.HasAnyGroup(board) || ForceGroup(board, count);

            for (int slot = 0; slot < count; slot++)
            {
                if (_from[slot] != slot)
                    moves.Add(new TileMove(board.ToPos(_cells[_from[slot]]), board.ToPos(_cells[slot])));
            }

            return solved;
        }

        private bool ForceGroup(Board board, int count)
        {
            if (!TryPickAdjacentPair(board, out int slotA, out int slotB))
                return false;

            Array.Clear(_histogram, 0, _histogram.Length);
            for (int slot = 0; slot < count; slot++)
                _histogram[board[_cells[slot]].Color]++;

            int color = board[_cells[slotA]].Color;
            if (_histogram[color] < 2)
            {
                color = -1;
                for (int c = 0; c < _histogram.Length; c++)
                {
                    if (_histogram[c] >= 2) { color = c; break; }
                }
            }

            if (color < 0)
            {
                board[_cells[slotB]] = Tile.OfColor(board[_cells[slotA]].Color);
                LastShuffleRecolored = true;
                return true;
            }

            PlaceColor(board, count, slotA, color, keep: slotB);
            PlaceColor(board, count, slotB, color, keep: slotA);
            return true;
        }

        private void PlaceColor(Board board, int count, int slot, int color, int keep)
        {
            if (board[_cells[slot]].Color == color) return;

            for (int other = 0; other < count; other++)
            {
                if (other == slot || other == keep) continue;
                if (board[_cells[other]].Color != color) continue;

                (_from[slot], _from[other]) = (_from[other], _from[slot]);
                Tile tmp = board[_cells[slot]];
                board[_cells[slot]] = board[_cells[other]];
                board[_cells[other]] = tmp;
                return;
            }
        }

        private bool TryPickAdjacentPair(Board board, out int slotA, out int slotB)
        {
            slotA = slotB = -1;
            int width = board.Width;
            int height = board.Height;

            int pairs = 0;
            for (int i = 0; i < board.Count; i++)
            {
                if (_slotOf[i] < 0) continue;
                if (i % width < width - 1 && _slotOf[i + 1] >= 0) pairs++;
                if (i / width < height - 1 && _slotOf[i + width] >= 0) pairs++;
            }

            if (pairs == 0) return false;

            int pick = _random.Next(pairs);
            for (int i = 0; i < board.Count; i++)
            {
                if (_slotOf[i] < 0) continue;

                if (i % width < width - 1 && _slotOf[i + 1] >= 0 && pick-- == 0)
                {
                    slotA = _slotOf[i];
                    slotB = _slotOf[i + 1];
                    return true;
                }

                if (i / width < height - 1 && _slotOf[i + width] >= 0 && pick-- == 0)
                {
                    slotA = _slotOf[i];
                    slotB = _slotOf[i + width];
                    return true;
                }
            }

            return false;
        }

        private void EnsureCapacity(int cellCount)
        {
            if (_cells.Length >= cellCount) return;
            _cells = new int[cellCount];
            _from = new int[cellCount];
            _original = new Tile[cellCount];
            _slotOf = new int[cellCount];
        }
    }
}

using System;

namespace BlastPuzzle.Core
{
    public sealed class Board
    {
        public const int MinSize = 2;
        public const int MaxSize = 10;

        private readonly Tile[] _tiles;

        public int Width { get; }
        public int Height { get; }
        public int Count => _tiles.Length;

        public Board(int width, int height)
        {
            if (width < MinSize || width > MaxSize) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < MinSize || height > MaxSize) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _tiles = new Tile[width * height];
        }

        public Tile this[int x, int y]
        {
            get => _tiles[ToIndex(x, y)];
            set => _tiles[ToIndex(x, y)] = value;
        }

        public Tile this[GridPos p]
        {
            get => _tiles[ToIndex(p.X, p.Y)];
            set => _tiles[ToIndex(p.X, p.Y)] = value;
        }

        public Tile this[int index]
        {
            get => _tiles[index];
            set => _tiles[index] = value;
        }

        public int ToIndex(int x, int y) => y * Width + x;
        public int ToIndex(GridPos p) => p.Y * Width + p.X;
        public GridPos ToPos(int index) => new GridPos(index % Width, index / Width);

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(GridPos p) => InBounds(p.X, p.Y);

        public override string ToString() => BoardText.Format(this);
    }
}

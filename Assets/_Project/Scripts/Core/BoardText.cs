using System;
using System.Text;

namespace BlastPuzzle.Core
{
    public static class BoardText
    {
        public static Board Parse(params string[] rowsTopToBottom)
        {
            if (rowsTopToBottom == null || rowsTopToBottom.Length == 0)
                throw new ArgumentException("At least one row is required.", nameof(rowsTopToBottom));

            int height = rowsTopToBottom.Length;
            int width = rowsTopToBottom[0].Length;
            var board = new Board(width, height);

            for (int row = 0; row < height; row++)
            {
                string line = rowsTopToBottom[row];
                if (line.Length != width)
                    throw new ArgumentException($"Row {row} has length {line.Length}, expected {width}.");

                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                    board[x, y] = ParseChar(line[x]);
            }

            return board;
        }

        public static string Format(Board board)
        {
            var sb = new StringBuilder((board.Width + 1) * board.Height);
            for (int y = board.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < board.Width; x++)
                    sb.Append(ToChar(board[x, y]));
                if (y > 0) sb.Append('\n');
            }
            return sb.ToString();
        }

        public static bool TryParseChar(char c, out Tile tile)
        {
            tile = Tile.Empty;
            if (c == '.') return true;
            if (c >= '0' && c <= '9') { tile = Tile.OfColor(c - '0'); return true; }
            if (c >= 'a' && c <= 'f') { tile = Tile.Disco(c - 'a'); return true; }
            switch (c)
            {
                case '-': tile = Tile.Rocket(false); return true;
                case '|': tile = Tile.Rocket(true); return true;
                case '*': tile = Tile.Bomb; return true;
                case '#': tile = Tile.Box(1); return true;
                case '@': tile = Tile.Box(2); return true;
                default: return false;
            }
        }

        public static char ToChar(Tile tile)
        {
            switch (tile.Kind)
            {
                case TileKind.Color: return (char)('0' + tile.Value);
                case TileKind.Rocket: return tile.Value == 1 ? '|' : '-';
                case TileKind.Bomb: return '*';
                case TileKind.Disco: return (char)('a' + tile.Value);
                case TileKind.Box: return tile.Value >= 2 ? '@' : '#';
                default: return '.';
            }
        }

        private static Tile ParseChar(char c)
        {
            if (TryParseChar(c, out Tile tile)) return tile;
            throw new ArgumentException($"Unknown tile character '{c}'.");
        }
    }
}

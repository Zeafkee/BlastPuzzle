using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public sealed class GravityResolver
    {
        public void Collapse(Board board, List<TileMove> moves)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int write = 0;
                for (int read = 0; read < board.Height; read++)
                {
                    Tile tile = board[x, read];
                    if (tile.IsEmpty) continue;

                    if (read != write)
                    {
                        board[x, write] = tile;
                        board[x, read] = Tile.Empty;
                        moves.Add(new TileMove(new GridPos(x, read), new GridPos(x, write)));
                    }

                    write++;
                }
            }
        }

        public void Refill(Board board, IRandomSource random, int colorCount, List<TileSpawn> spawns)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int stackIndex = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    if (!board[x, y].IsEmpty) continue;

                    Tile tile = Tile.OfColor(random.Next(colorCount));
                    board[x, y] = tile;
                    spawns.Add(new TileSpawn(new GridPos(x, y), tile, board.Height + stackIndex));
                    stackIndex++;
                }
            }
        }
    }
}

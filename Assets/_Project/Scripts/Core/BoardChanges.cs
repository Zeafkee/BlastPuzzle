namespace BlastPuzzle.Core
{
    public readonly struct TileMove
    {
        public readonly GridPos From;
        public readonly GridPos To;

        public TileMove(GridPos from, GridPos to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"{From}->{To}";
    }

    public readonly struct TileSpawn
    {
        public readonly GridPos To;
        public readonly Tile Tile;
        public readonly int StartY;

        public TileSpawn(GridPos to, Tile tile, int startY)
        {
            To = to;
            Tile = tile;
            StartY = startY;
        }

        public override string ToString() => $"spawn {Tile} y{StartY}->{To}";
    }
}

using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public enum GameOutcome : byte
    {
        Playing = 0,
        Won = 1,
        Lost = 2,
    }

    public enum StepKind : byte
    {
        Group = 0,
        Rocket = 1,
        Bomb = 2,
        Disco = 3,
    }

    public struct StepInfo
    {
        public StepKind Kind;
        public GridPos Origin;
        public Tile OriginTile;
        public int ClearedStart;
        public int ClearedCount;
        public int DamagedStart;
        public int DamagedCount;
    }

    public readonly struct ClearedTile
    {
        public readonly GridPos Pos;
        public readonly Tile Tile;

        public ClearedTile(GridPos pos, Tile tile)
        {
            Pos = pos;
            Tile = tile;
        }
    }

    public readonly struct DamagedTile
    {
        public readonly GridPos Pos;
        public readonly Tile After;

        public DamagedTile(GridPos pos, Tile after)
        {
            Pos = pos;
            After = after;
        }
    }

    public sealed class TurnResult
    {
        private const int MaxCells = Board.MaxSize * Board.MaxSize;

        public readonly List<StepInfo> Steps = new List<StepInfo>(MaxCells);
        public readonly List<ClearedTile> Cleared = new List<ClearedTile>(MaxCells);
        public readonly List<DamagedTile> Damaged = new List<DamagedTile>(MaxCells);

        public bool BoosterCreated;
        public GridPos BoosterPos;
        public Tile BoosterTile;

        public readonly List<TileMove> Moves = new List<TileMove>(MaxCells);
        public readonly List<TileSpawn> Spawns = new List<TileSpawn>(MaxCells);

        public bool Shuffled;
        public readonly List<TileMove> ShuffleMoves = new List<TileMove>(MaxCells);

        public int ScoreGained;
        public GameOutcome Outcome;

        public void Clear()
        {
            Steps.Clear();
            Cleared.Clear();
            Damaged.Clear();
            BoosterCreated = false;
            BoosterPos = default;
            BoosterTile = default;
            Moves.Clear();
            Spawns.Clear();
            Shuffled = false;
            ShuffleMoves.Clear();
            ScoreGained = 0;
            Outcome = GameOutcome.Playing;
        }
    }
}

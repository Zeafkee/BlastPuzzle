using System;
using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public sealed class BlastGame
    {
        public const int ScorePerBlock = 10;
        public const int ScorePerBox = 30;
        public const int BombRadius = 2;

        private readonly IRandomSource _random;
        private readonly GroupFinder _groupFinder;
        private readonly GravityResolver _gravity = new GravityResolver();
        private readonly Shuffler _shuffler;

        private readonly int[] _goalRemaining;
        private readonly int[] _groupSizes;
        private readonly List<GridPos> _group = new List<GridPos>(100);

        private readonly int[] _boosterQueue;
        private readonly int[] _queuedStamp;
        private readonly int[] _hitStamp;
        private int _turnStamp;
        private int _stepStamp;

        public LevelConfig Config { get; }
        public Board Board { get; }
        public int MovesLeft { get; private set; }
        public int Score { get; private set; }
        public GameOutcome Outcome { get; private set; }

        public IReadOnlyList<int> GoalRemaining => _goalRemaining;

        public int[] GroupSizes => _groupSizes;

        public BlastGame(LevelConfig config, IRandomSource random)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            config.Validate();

            Board = new Board(config.Width, config.Height);
            _groupFinder = new GroupFinder(Board);
            _shuffler = new Shuffler(random);
            _groupSizes = new int[Board.Count];
            _boosterQueue = new int[Board.Count];
            _queuedStamp = new int[Board.Count];
            _hitStamp = new int[Board.Count];

            MovesLeft = config.Moves;
            _goalRemaining = new int[config.Goals.Length];
            for (int i = 0; i < _goalRemaining.Length; i++)
                _goalRemaining[i] = config.Goals[i].Count;

            FillStartBoard();
            EnsurePlayable(null);
            _groupFinder.ComputeGroupSizes(_groupSizes);
        }

        public int GetTier(int index) => Board[index].IsColor ? Config.Tiers.GetTier(_groupSizes[index]) : 0;

        public bool CanTap(GridPos pos)
        {
            if (Outcome != GameOutcome.Playing || !Board.InBounds(pos)) return false;
            Tile tile = Board[pos];
            if (tile.IsBooster) return true;
            return tile.IsColor && _groupSizes[Board.ToIndex(pos)] >= GroupFinder.MinGroupSize;
        }

        public bool Tap(GridPos pos, TurnResult result)
        {
            result.Clear();
            if (!CanTap(pos)) return false;

            _turnStamp++;
            int queueHead = 0;
            int queueTail = 0;
            int index = Board.ToIndex(pos);
            Tile tapped = Board[index];

            if (tapped.IsColor)
            {
                int size = _groupFinder.FindGroup(pos, _group);
                BlastGroup(pos, tapped, result);

                int tier = Config.Tiers.GetTier(size);
                if (tier > 0)
                {
                    Tile booster = tier == 1 ? Tile.Rocket(_random.Next(2) == 1)
                        : tier == 2 ? Tile.Bomb
                        : Tile.Disco(tapped.Color);

                    Board[index] = booster;
                    result.BoosterCreated = true;
                    result.BoosterPos = pos;
                    result.BoosterTile = booster;
                }
            }
            else
            {
                _queuedStamp[index] = _turnStamp;
                _boosterQueue[queueTail++] = index;
            }

            while (queueHead < queueTail)
                ActivateBooster(_boosterQueue[queueHead++], result, ref queueTail);

            MovesLeft--;

            _gravity.Collapse(Board, result.Moves);
            _gravity.Refill(Board, _random, Config.ColorCount, result.Spawns);
            EnsurePlayable(result);
            _groupFinder.ComputeGroupSizes(_groupSizes);

            Score += result.ScoreGained;
            Outcome = AllGoalsDone() ? GameOutcome.Won
                : MovesLeft <= 0 ? GameOutcome.Lost
                : GameOutcome.Playing;
            result.Outcome = Outcome;
            return true;
        }

        public bool TryGetHint(out GridPos pos)
        {
            int best = -1;
            int bestSize = GroupFinder.MinGroupSize - 1;
            for (int i = 0; i < Board.Count; i++)
            {
                if (_groupSizes[i] > bestSize)
                {
                    bestSize = _groupSizes[i];
                    best = i;
                }
            }

            if (best < 0)
            {
                for (int i = 0; i < Board.Count && best < 0; i++)
                    if (Board[i].IsBooster) best = i;
            }

            pos = best >= 0 ? Board.ToPos(best) : default;
            return best >= 0;
        }

        private void BlastGroup(GridPos origin, Tile tapped, TurnResult result)
        {
            var step = BeginStep(StepKind.Group, origin, tapped, result);

            for (int i = 0; i < _group.Count; i++)
                ClearCell(Board.ToIndex(_group[i]), result);

            for (int i = 0; i < _group.Count; i++)
            {
                GridPos p = _group[i];
                HitBoxAt(p.X - 1, p.Y, result);
                HitBoxAt(p.X + 1, p.Y, result);
                HitBoxAt(p.X, p.Y - 1, result);
                HitBoxAt(p.X, p.Y + 1, result);
            }

            result.ScoreGained += Math.Max(0, _group.Count - GroupFinder.MinGroupSize) * 5;
            EndStep(step, result);
        }

        private void ActivateBooster(int index, TurnResult result, ref int queueTail)
        {
            Tile booster = Board[index];
            if (!booster.IsBooster) return;

            GridPos origin = Board.ToPos(index);
            StepKind kind = booster.Kind == TileKind.Rocket ? StepKind.Rocket
                : booster.Kind == TileKind.Bomb ? StepKind.Bomb
                : StepKind.Disco;

            var step = BeginStep(kind, origin, booster, result);
            Board[index] = Tile.Empty;

            switch (booster.Kind)
            {
                case TileKind.Rocket:
                    if (booster.IsVerticalRocket)
                    {
                        for (int y = 0; y < Board.Height; y++)
                            HitCell(origin.X, y, result, ref queueTail);
                    }
                    else
                    {
                        for (int x = 0; x < Board.Width; x++)
                            HitCell(x, origin.Y, result, ref queueTail);
                    }
                    break;

                case TileKind.Bomb:
                    for (int dy = -BombRadius; dy <= BombRadius; dy++)
                    {
                        int span = BombRadius - Math.Abs(dy);
                        for (int dx = -span; dx <= span; dx++)
                            HitCell(origin.X + dx, origin.Y + dy, result, ref queueTail);
                    }
                    break;

                case TileKind.Disco:
                    for (int i = 0; i < Board.Count; i++)
                    {
                        Tile tile = Board[i];
                        if (tile.IsColor && tile.Color == booster.Color)
                            ClearCell(i, result);
                    }
                    break;
            }

            EndStep(step, result);
        }

        private void HitCell(int x, int y, TurnResult result, ref int queueTail)
        {
            if (!Board.InBounds(x, y)) return;

            int index = Board.ToIndex(x, y);
            Tile tile = Board[index];

            if (tile.IsColor)
            {
                ClearCell(index, result);
            }
            else if (tile.IsBox)
            {
                HitBoxAt(x, y, result);
            }
            else if (tile.IsBooster && _queuedStamp[index] != _turnStamp)
            {
                _queuedStamp[index] = _turnStamp;
                _boosterQueue[queueTail++] = index;
            }
        }

        private void ClearCell(int index, TurnResult result)
        {
            Tile tile = Board[index];
            Board[index] = Tile.Empty;
            result.Cleared.Add(new ClearedTile(Board.ToPos(index), tile));
            result.ScoreGained += ScorePerBlock;
            CountTowardsGoals(GoalKind.Color, tile.Color);
        }

        private void HitBoxAt(int x, int y, TurnResult result)
        {
            if (!Board.InBounds(x, y)) return;

            int index = Board.ToIndex(x, y);
            Tile tile = Board[index];
            if (!tile.IsBox || _hitStamp[index] == _stepStamp) return;
            _hitStamp[index] = _stepStamp;

            GridPos pos = new GridPos(x, y);
            if (tile.Value <= 1)
            {
                Board[index] = Tile.Empty;
                result.Cleared.Add(new ClearedTile(pos, tile));
                result.ScoreGained += ScorePerBox;
                CountTowardsGoals(GoalKind.Box, 0);
            }
            else
            {
                Tile damaged = Tile.Box(tile.Value - 1);
                Board[index] = damaged;
                result.Damaged.Add(new DamagedTile(pos, damaged));
            }
        }

        private StepInfo BeginStep(StepKind kind, GridPos origin, Tile originTile, TurnResult result)
        {
            _stepStamp++;
            return new StepInfo
            {
                Kind = kind,
                Origin = origin,
                OriginTile = originTile,
                ClearedStart = result.Cleared.Count,
                DamagedStart = result.Damaged.Count,
            };
        }

        private static void EndStep(StepInfo step, TurnResult result)
        {
            step.ClearedCount = result.Cleared.Count - step.ClearedStart;
            step.DamagedCount = result.Damaged.Count - step.DamagedStart;
            result.Steps.Add(step);
        }

        private void CountTowardsGoals(GoalKind kind, int color)
        {
            GoalConfig[] goals = Config.Goals;
            for (int i = 0; i < goals.Length; i++)
            {
                if (goals[i].Kind != kind || _goalRemaining[i] <= 0) continue;
                if (kind == GoalKind.Color && goals[i].Color != color) continue;
                _goalRemaining[i]--;
            }
        }

        private bool AllGoalsDone()
        {
            for (int i = 0; i < _goalRemaining.Length; i++)
                if (_goalRemaining[i] > 0) return false;
            return true;
        }

        private void FillStartBoard()
        {
            string[] layout = Config.Layout;
            bool hasLayout = layout != null && layout.Length > 0;

            for (int y = 0; y < Board.Height; y++)
            {
                for (int x = 0; x < Board.Width; x++)
                {
                    char c = hasLayout ? layout[Board.Height - 1 - y][x] : '?';
                    Tile tile;
                    if (c == '?' || c == '.')
                        tile = Tile.OfColor(_random.Next(Config.ColorCount));
                    else if (!BoardText.TryParseChar(c, out tile))
                        throw new ArgumentException($"Unknown layout character '{c}'.");

                    Board[x, y] = tile;
                }
            }
        }

        private void EnsurePlayable(TurnResult result)
        {
            if (GroupFinder.HasAnyMove(Board)) return;

            List<TileMove> moves = result != null ? result.ShuffleMoves : null;
            _scratchMoves.Clear();
            bool solved = _shuffler.Shuffle(Board, moves ?? _scratchMoves);

            if (!solved)
            {
                for (int i = 0; i < Board.Count; i++)
                {
                    if (!Board[i].IsColor) continue;
                    Board[i] = Tile.Rocket(_random.Next(2) == 1);
                    break;
                }
            }

            if (result != null) result.Shuffled = true;
        }

        private readonly List<TileMove> _scratchMoves = new List<TileMove>(100);
    }

    public static class StarRules
    {
        public static int Calculate(int movesLeft, int totalMoves)
        {
            if (totalMoves <= 0) return 1;
            float left = (float)movesLeft / totalMoves;
            if (left >= 0.30f) return 3;
            if (left >= 0.12f) return 2;
            return 1;
        }
    }
}

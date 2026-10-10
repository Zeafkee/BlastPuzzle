using System.Collections.Generic;

namespace BlastPuzzle.Core
{
    public sealed class AutoPlayer
    {
        private readonly IRandomSource _random;
        private readonly float _skill;
        private readonly List<int> _candidates = new List<int>(100);
        private readonly List<GridPos> _group = new List<GridPos>(100);
        private readonly TurnResult _result = new TurnResult();

        private GroupFinder _finder;
        private Board _finderBoard;
        private int[] _seen = new int[0];
        private int[] _boxSeen = new int[0];
        private int _stamp;
        private int _boxStamp;

        public AutoPlayer(IRandomSource random, float skill = 0.7f)
        {
            _random = random;
            _skill = skill;
        }

        public bool Play(BlastGame game)
        {
            while (game.Outcome == GameOutcome.Playing)
            {
                if (!TryPickMove(game, out GridPos move)) break;
                if (!game.Tap(move, _result)) break;
            }

            return game.Outcome == GameOutcome.Won;
        }

        public bool TryPickMove(BlastGame game, out GridPos move)
        {
            Board board = game.Board;
            if (!ReferenceEquals(_finderBoard, board))
            {
                _finderBoard = board;
                _finder = new GroupFinder(board);
                _seen = new int[board.Count];
                _boxSeen = new int[board.Count];
            }

            _stamp++;
            _candidates.Clear();
            int best = -1;
            float bestValue = float.MinValue;

            for (int i = 0; i < board.Count; i++)
            {
                Tile tile = board[i];
                float value;

                if (tile.IsBooster)
                {
                    value = tile.Kind == TileKind.Rocket ? 9f : tile.Kind == TileKind.Bomb ? 13f : 16f;
                }
                else if (tile.IsColor && _seen[i] != _stamp && game.GroupSizes[i] >= GroupFinder.MinGroupSize)
                {
                    _finder.FindGroup(board.ToPos(i), _group);
                    value = ScoreGroup(game, tile);
                }
                else
                {
                    continue;
                }

                _candidates.Add(i);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = i;
                }
            }

            if (_candidates.Count == 0)
            {
                move = default;
                return false;
            }

            bool playBest = _random.Next(1000) < (int)(_skill * 1000);
            int pick = playBest ? best : _candidates[_random.Next(_candidates.Count)];
            move = board.ToPos(pick);
            return true;
        }

        private float ScoreGroup(BlastGame game, Tile tile)
        {
            Board board = game.Board;
            GoalConfig[] goals = game.Config.Goals;
            int size = _group.Count;
            int boxes = 0;
            _boxStamp++;

            for (int k = 0; k < size; k++)
            {
                GridPos p = _group[k];
                _seen[board.ToIndex(p)] = _stamp;
                boxes += CountBox(board, p.X - 1, p.Y) + CountBox(board, p.X + 1, p.Y)
                       + CountBox(board, p.X, p.Y - 1) + CountBox(board, p.X, p.Y + 1);
            }

            float value = size;
            if (game.Config.Tiers.GetTier(size) > 0) value += 4f;

            for (int g = 0; g < goals.Length; g++)
            {
                int remaining = game.GoalRemaining[g];
                if (remaining <= 0) continue;

                if (goals[g].Kind == GoalKind.Color && goals[g].Color == tile.Color)
                    value += 3f * System.Math.Min(size, remaining);
                else if (goals[g].Kind == GoalKind.Box)
                    value += 6f * System.Math.Min(boxes, remaining);
            }

            return value;
        }

        private int CountBox(Board board, int x, int y)
        {
            if (!board.InBounds(x, y)) return 0;
            int index = board.ToIndex(x, y);
            if (!board[index].IsBox || _boxSeen[index] == _boxStamp) return 0;
            _boxSeen[index] = _boxStamp;
            return 1;
        }

        public static float EstimateWinRate(LevelConfig config, int runs, float skill, int seed = 1)
        {
            int wins = 0;
            for (int i = 0; i < runs; i++)
            {
                var random = new SystemRandomSource(seed + i);
                var game = new BlastGame(config, random);
                if (new AutoPlayer(random, skill).Play(game)) wins++;
            }

            return runs > 0 ? (float)wins / runs : 0f;
        }
    }
}

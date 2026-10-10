using System;
using System.Collections;
using System.Collections.Generic;
using BlastPuzzle.Audio;
using BlastPuzzle.Core;
using BlastPuzzle.Data;
using UnityEngine;

namespace BlastPuzzle.View
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private BoardLayout layout;
        [SerializeField] private TileViewPool pool;
        [SerializeField] private TileAnimator animator;
        [SerializeField] private FxPlayer fx;
        [SerializeField] private TileTheme theme;
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private float framePadding = 0.3f;

        [Header("Timing")]
        [SerializeField] private float groupStepTime = 0.13f;
        [SerializeField] private float rocketCellDelay = 0.035f;
        [SerializeField] private float bombCellDelay = 0.04f;
        [SerializeField] private float shuffleTime = 0.45f;

        private BlastGame _game;
        private Board _board;
        private TileView[] _views = Array.Empty<TileView>();
        private TileView[] _scratch = Array.Empty<TileView>();

        public TileTheme Theme => theme;
        public BoardLayout Layout => layout;
        public bool IsBusy => animator.IsBusy;

        public event Action<TurnResult, int> StepShown;

        public void Build(BlastGame game)
        {
            Clear();

            _game = game;
            _board = game.Board;
            layout.Configure(_board.Width, _board.Height);
            animator.SetFadeLine(_board.Height * layout.CellSize * 0.5f);

            if (_views.Length != _board.Count)
            {
                _views = new TileView[_board.Count];
                _scratch = new TileView[_board.Count];
            }

            if (frame != null)
            {
                frame.gameObject.SetActive(true);
                frame.size = layout.BoardSize + Vector2.one * (framePadding * 2f);
            }

            for (int i = 0; i < _board.Count; i++)
            {
                GridPos pos = _board.ToPos(i);
                TileView view = pool.Get();
                view.CachedTransform.localPosition = layout.GridToLocal(pos);
                ApplyVisual(view, _board[i], _game.GetTier(i));
                animator.Appear(view, (pos.X + (_board.Height - 1 - pos.Y)) * 0.022f);
                _views[i] = view;
            }
        }

        public void Clear()
        {
            animator.Clear();
            fx.StopAll();

            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] == null) continue;
                pool.Release(_views[i]);
                _views[i] = null;
            }

            if (frame != null) frame.gameObject.SetActive(false);
            _game = null;
            _board = null;
        }

        public void ShakeTile(GridPos pos)
        {
            TileView view = _views[_board.ToIndex(pos)];
            if (view != null) animator.Shake(view);
        }

        public void PulseTiles(List<GridPos> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                TileView view = _views[_board.ToIndex(cells[i])];
                if (view != null) animator.Pulse(view, i * 0.03f);
            }
        }

        public IEnumerator PlayTurn(TurnResult result)
        {
            animator.StopIdleAnimations();

            for (int s = 0; s < result.Steps.Count; s++)
            {
                StepInfo step = result.Steps[s];
                float wait = PlayStep(result, step, s == 0 && result.BoosterCreated);
                StepShown?.Invoke(result, s);
                for (float t = 0f; t < wait; t += Time.deltaTime) yield return null;
            }

            if (result.BoosterCreated)
                SpawnBooster(result);

            ApplyGravity(result);
            while (animator.IsBusy) yield return null;

            if (result.Shuffled)
            {
                for (float t = 0f; t < 0.2f; t += Time.deltaTime) yield return null;
                PlayShuffle(result);
                while (animator.IsBusy) yield return null;
            }

            RefreshAll();
        }

        private float PlayStep(TurnResult result, StepInfo step, bool mergeIntoBooster)
        {
            Vector3 origin = layout.GridToLocal(step.Origin);
            float longestDelay = 0f;

            switch (step.Kind)
            {
                case StepKind.Group:
                    AudioManager.Play(Sfx.Pop, 0.9f + Mathf.Min(step.ClearedCount, 12) * 0.035f);
                    break;

                case StepKind.Rocket:
                    PopOrigin(step, origin);
                    AudioManager.Play(Sfx.Rocket);
                    fx.RocketTrails(origin, step.OriginTile.IsVerticalRocket, Mathf.Max(_board.Width, _board.Height) * layout.CellSize);
                    fx.Shake(0.06f, 0.18f);
                    break;

                case StepKind.Bomb:
                    PopOrigin(step, origin);
                    AudioManager.Play(Sfx.Bomb);
                    fx.Ring(origin, 5.2f, new Color(1f, 0.85f, 0.5f, 0.9f));
                    fx.Sparkle(origin, 14);
                    fx.Shake(0.22f, 0.3f);
                    break;

                case StepKind.Disco:
                    PopOrigin(step, origin);
                    AudioManager.Play(Sfx.Disco);
                    fx.Ring(origin, 7f, theme.GetTint(step.OriginTile), 0.5f);
                    fx.Sparkle(origin, 18);
                    break;
            }

            float discoStagger = step.ClearedCount > 0 ? Mathf.Min(0.035f, 0.8f / step.ClearedCount) : 0f;

            for (int i = step.ClearedStart; i < step.ClearedStart + step.ClearedCount; i++)
            {
                ClearedTile cleared = result.Cleared[i];
                int index = _board.ToIndex(cleared.Pos);
                TileView view = _views[index];
                if (view == null) continue;
                _views[index] = null;

                float delay;
                switch (step.Kind)
                {
                    case StepKind.Rocket:
                        delay = (Mathf.Abs(cleared.Pos.X - step.Origin.X) + Mathf.Abs(cleared.Pos.Y - step.Origin.Y)) * rocketCellDelay;
                        break;
                    case StepKind.Bomb:
                        delay = (Mathf.Abs(cleared.Pos.X - step.Origin.X) + Mathf.Abs(cleared.Pos.Y - step.Origin.Y)) * bombCellDelay;
                        break;
                    case StepKind.Disco:
                        delay = (i - step.ClearedStart) * discoStagger;
                        break;
                    default:
                        delay = 0f;
                        break;
                }

                if (delay > longestDelay) longestDelay = delay;

                if (cleared.Tile.IsBox)
                {
                    AudioManager.Play(Sfx.BoxBreak);
                    animator.Pop(view, delay, true, theme.GetTint(cleared.Tile));
                }
                else if (mergeIntoBooster)
                {
                    animator.Merge(view, origin, 0f);
                }
                else
                {
                    animator.Pop(view, delay, true, theme.GetTint(cleared.Tile));
                }
            }

            for (int i = step.DamagedStart; i < step.DamagedStart + step.DamagedCount; i++)
            {
                DamagedTile damaged = result.Damaged[i];
                TileView view = _views[_board.ToIndex(damaged.Pos)];
                if (view == null) continue;

                ApplyVisual(view, damaged.After, 0);
                animator.Shake(view);
                fx.Burst(layout.GridToLocal(damaged.Pos), theme.GetTint(damaged.After), 4);
                AudioManager.Play(Sfx.BoxHit);
            }

            return groupStepTime + longestDelay;
        }

        private void PopOrigin(StepInfo step, Vector3 origin)
        {
            int index = _board.ToIndex(step.Origin);
            TileView view = _views[index];
            if (view == null) return;
            _views[index] = null;
            animator.Pop(view, 0f, false, default);
        }

        private void SpawnBooster(TurnResult result)
        {
            int index = _board.ToIndex(result.BoosterPos);
            Vector3 local = layout.GridToLocal(result.BoosterPos);

            TileView view = pool.Get();
            view.CachedTransform.localPosition = local;
            ApplyVisual(view, result.BoosterTile, 0);
            animator.Appear(view, 0f);
            _views[index] = view;

            fx.Ring(local, 2.4f, new Color(1f, 1f, 1f, 0.8f), 0.3f);
            fx.Sparkle(local, 10);
            AudioManager.Play(Sfx.BoosterCreate);
        }

        private void ApplyGravity(TurnResult result)
        {
            List<TileMove> moves = result.Moves;
            for (int i = 0; i < moves.Count; i++)
            {
                int from = _board.ToIndex(moves[i].From);
                int to = _board.ToIndex(moves[i].To);
                TileView view = _views[from];
                _views[from] = null;
                _views[to] = view;
                if (view == null) continue;

                animator.Fall(view, view.CachedTransform.localPosition, layout.GridToLocal(moves[i].To), 0f, false);
                view.CachedTransform.localScale = Vector3.one;
            }

            List<TileSpawn> spawns = result.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                TileSpawn spawn = spawns[i];
                TileView view = pool.Get();
                ApplyVisual(view, spawn.Tile, 0);
                _views[_board.ToIndex(spawn.To)] = view;
                animator.Fall(view, layout.GridToLocal(spawn.To.X, spawn.StartY), layout.GridToLocal(spawn.To), 0f, true);
            }
        }

        private void PlayShuffle(TurnResult result)
        {
            AudioManager.Play(Sfx.Shuffle);

            Array.Copy(_views, _scratch, _views.Length);
            List<TileMove> moves = result.ShuffleMoves;
            for (int i = 0; i < moves.Count; i++)
            {
                TileView view = _views[_board.ToIndex(moves[i].From)];
                _scratch[_board.ToIndex(moves[i].To)] = view;
                if (view != null)
                    animator.Move(view, layout.GridToLocal(moves[i].To), shuffleTime, 0f);
            }

            (_views, _scratch) = (_scratch, _views);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                TileView view = _views[i];
                Tile tile = _board[i];

                if (tile.IsEmpty)
                {
                    if (view != null) { pool.Release(view); _views[i] = null; }
                    continue;
                }

                if (view == null)
                {
                    view = pool.Get();
                    view.CachedTransform.localPosition = layout.GridToLocal(_board.ToPos(i));
                    _views[i] = view;
                }

                ApplyVisual(view, tile, _game.GetTier(i));
            }
        }

        private void ApplyVisual(TileView view, Tile tile, int tier)
        {
            view.SetSprite(theme.GetSprite(tile, tier));
            view.CachedTransform.localRotation = tile.IsVerticalRocket ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        }
    }
}

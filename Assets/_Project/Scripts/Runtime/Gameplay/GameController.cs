using System;
using System.Collections;
using System.Collections.Generic;
using BlastPuzzle.Audio;
using BlastPuzzle.Core;
using BlastPuzzle.Data;
using BlastPuzzle.InputHandling;
using BlastPuzzle.UI;
using BlastPuzzle.View;
using UnityEngine;

namespace BlastPuzzle.Gameplay
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private TapInput tapInput;
        [SerializeField] private HudView hud;
        [SerializeField] private Banner banner;
        [SerializeField] private FxPlayer fx;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float hintDelay = 5f;
        [SerializeField] private float hintRepeat = 2.5f;

        private readonly TurnResult _turn = new TurnResult();
        private readonly List<GridPos> _hintCells = new List<GridPos>(100);

        private BlastGame _game;
        private GroupFinder _hintFinder;
        private Coroutine _turnRoutine;
        private bool _playing;
        private bool _paused;
        private float _idleTime;
        private Vector2Int _fittedScreen;
        private Rect _fittedSafeArea;

        public int LevelIndex { get; private set; } = -1;
        public bool IsRunning => _game != null;
        public BlastGame Game => _game;

        public event Action<bool, int> LevelEnded;

        private void Awake()
        {
            tapInput.Tapped += HandleTap;
            boardView.StepShown += HandleStepShown;
            tapInput.Locked = true;
        }

        private void OnDestroy()
        {
            tapInput.Tapped -= HandleTap;
            boardView.StepShown -= HandleStepShown;
        }

        public void StartLevel(int index)
        {
            StopLevel();

            LevelIndex = Mathf.Clamp(index, 0, catalog.Count - 1);
            LevelData level = catalog.Get(LevelIndex);

            _game = new BlastGame(level.ToConfig(), new SystemRandomSource());
            _hintFinder = new GroupFinder(_game.Board);

            boardView.Build(_game);
            FitCamera();
            hud.Bind(LevelIndex + 1, _game, boardView.Theme, worldCamera);
            banner.Show("LEVEL " + NumberStrings.Get(LevelIndex + 1), 0.6f);

            _idleTime = 0f;
            _playing = true;
            _paused = false;
            tapInput.Locked = false;
        }

        public void StopLevel()
        {
            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            _playing = false;
            tapInput.Locked = true;
            banner.HideInstant();
            boardView.Clear();
            _game = null;
        }

        public void SetInputEnabled(bool enabled)
        {
            _paused = !enabled;
            tapInput.Locked = _paused || !_playing || _turnRoutine != null;
            _idleTime = 0f;
        }

        public bool AcceptsInput => _playing && !_paused && _turnRoutine == null;

        private void HandleTap(Vector3 world)
        {
            if (boardView.Layout.TryWorldToGrid(world, out GridPos pos))
                TapCell(pos);
        }

        public void TapCell(GridPos pos)
        {
            if (!AcceptsInput) return;

            _idleTime = 0f;

            if (!_game.Tap(pos, _turn))
            {
                boardView.ShakeTile(pos);
                AudioManager.Play(Sfx.Invalid);
                return;
            }

            _turnRoutine = StartCoroutine(PlayTurn());
        }

        private IEnumerator PlayTurn()
        {
            tapInput.Locked = true;
            hud.SetMoves(_game.MovesLeft);

            yield return boardView.PlayTurn(_turn);

            if (_turn.Shuffled)
                banner.Show("NO MOVES - SHUFFLED", 0.7f);

            hud.SyncGoals();
            _turnRoutine = null;
            _idleTime = 0f;

            if (_turn.Outcome == GameOutcome.Playing)
            {
                tapInput.Locked = _paused;
                yield break;
            }

            _playing = false;
            bool won = _turn.Outcome == GameOutcome.Won;
            int stars = won ? StarRules.Calculate(_game.MovesLeft, _game.Config.Moves) : 0;

            if (won)
            {
                fx.Confetti();
                AudioManager.Play(Sfx.Win);
            }
            else
            {
                AudioManager.Play(Sfx.Lose);
            }

            for (float t = 0f; t < 0.7f; t += Time.deltaTime) yield return null;
            LevelEnded?.Invoke(won, stars);
        }

        private void HandleStepShown(TurnResult result, int stepIndex)
        {
            StepInfo step = result.Steps[stepIndex];
            hud.BeginStep();
            for (int i = step.ClearedStart; i < step.ClearedStart + step.ClearedCount; i++)
            {
                ClearedTile cleared = result.Cleared[i];
                hud.OnTileCleared(cleared.Tile, boardView.Layout.GridToWorld(cleared.Pos));
            }
        }

        private void Update()
        {
            if (_game == null) return;

            if (Screen.width != _fittedScreen.x || Screen.height != _fittedScreen.y || Screen.safeArea != _fittedSafeArea)
                FitCamera();

            if (!_playing || tapInput.Locked) return;

            _idleTime += Time.deltaTime;
            if (_idleTime >= hintDelay)
            {
                _idleTime = hintDelay - hintRepeat;
                ShowHint();
            }
        }

        private void ShowHint()
        {
            if (!_game.TryGetHint(out GridPos pos)) return;

            if (_hintFinder.FindGroup(pos, _hintCells) < GroupFinder.MinGroupSize)
            {
                _hintCells.Clear();
                _hintCells.Add(pos);
            }

            boardView.PulseTiles(_hintCells);
        }

        private void FitCamera()
        {
            _fittedScreen = new Vector2Int(Screen.width, Screen.height);
            _fittedSafeArea = Screen.safeArea;
            boardView.Layout.FitCamera(worldCamera);
        }
    }
}

using BlastPuzzle.Audio;
using BlastPuzzle.Core;
using BlastPuzzle.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private GoalItemView[] goalItems;
        [SerializeField] private Button pauseButton;
        [SerializeField] private RectTransform flyerRoot;
        [SerializeField] private Image[] flyers;
        [SerializeField] private Color normalMovesColor = Color.white;
        [SerializeField] private Color lowMovesColor = new Color(1f, 0.45f, 0.4f);
        [SerializeField] private int lowMovesThreshold = 5;

        private const int MaxFlyersPerStep = 6;

        private BlastGame _game;
        private TileTheme _theme;
        private Camera _camera;
        private int _shownMoves = -1;
        private int _nextFlyer;
        private int _flyersThisStep;
        private int[] _pending = new int[0];
        private int[] _flyerGoal;
        private TweenCallback[] _flyerLanded;

        private void Awake()
        {
            _flyerGoal = new int[flyers.Length];
            _flyerLanded = new TweenCallback[flyers.Length];
            for (int i = 0; i < flyers.Length; i++)
            {
                int index = i;
                _flyerLanded[i] = () => Land(index);
            }
        }

        public Button PauseButton => pauseButton;

        public void Bind(int levelNumber, BlastGame game, TileTheme theme, Camera worldCamera)
        {
            _game = game;
            _theme = theme;
            _camera = worldCamera;

            levelLabel.text = "LEVEL " + NumberStrings.Get(levelNumber);
            _shownMoves = -1;
            SetMoves(game.MovesLeft);

            GoalConfig[] goals = game.Config.Goals;
            if (_pending.Length != goals.Length) _pending = new int[goals.Length];
            for (int i = 0; i < goalItems.Length; i++)
            {
                bool used = i < goals.Length;
                goalItems[i].gameObject.SetActive(used);
                if (!used) continue;
                _pending[i] = 0;
                goalItems[i].Setup(theme.GetGoalIcon(goals[i]), goals[i].Count);
            }

            for (int i = 0; i < flyers.Length; i++)
            {
                flyers[i].rectTransform.DOKill();
                flyers[i].gameObject.SetActive(false);
            }
        }

        public void SetMoves(int moves)
        {
            if (moves == _shownMoves) return;
            _shownMoves = moves;
            movesLabel.text = NumberStrings.Get(moves);
            movesLabel.color = moves <= lowMovesThreshold ? lowMovesColor : normalMovesColor;

            Transform t = movesLabel.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * 0.18f, 0.2f, 5, 0.5f);
        }

        public void BeginStep() => _flyersThisStep = 0;

        public void OnTileCleared(Tile tile, Vector3 worldPosition)
        {
            GoalConfig[] goals = _game.Config.Goals;
            for (int g = 0; g < goals.Length && g < goalItems.Length; g++)
            {
                bool matches = goals[g].Kind == GoalKind.Box
                    ? tile.IsBox
                    : tile.IsColor && tile.Color == goals[g].Color;
                if (!matches) continue;

                GoalItemView item = goalItems[g];
                if (item.Shown - _pending[g] <= 0) continue;

                if (_flyersThisStep < MaxFlyersPerStep && flyers.Length > 0)
                {
                    _flyersThisStep++;
                    _pending[g]++;
                    LaunchFlyer(g, _theme.GetSprite(tile.IsBox ? Tile.Box(1) : tile, 0), worldPosition);
                }
                else
                {
                    item.SetCount(item.Shown - 1);
                }
            }
        }

        public void SyncGoals()
        {
            for (int g = 0; g < _game.GoalRemaining.Count && g < goalItems.Length; g++)
                goalItems[g].SetCount(_game.GoalRemaining[g] + _pending[g]);
        }

        private void LaunchFlyer(int goalIndex, Sprite sprite, Vector3 worldPosition)
        {
            int index = _nextFlyer;
            Image flyer = flyers[index];
            _nextFlyer = (_nextFlyer + 1) % flyers.Length;

            RectTransform rect = flyer.rectTransform;
            rect.DOKill(true);

            Vector2 screen = _camera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flyerRoot, screen, null, out Vector2 start);
            Vector3 target = flyerRoot.InverseTransformPoint(goalItems[goalIndex].IconTransform.position);

            _flyerGoal[index] = goalIndex;
            flyer.sprite = sprite;
            flyer.gameObject.SetActive(true);
            rect.anchoredPosition = start;
            rect.localScale = Vector3.one;

            float delay = (_flyersThisStep - 1) * 0.04f;
            rect.DOScale(0.6f, 0.45f).SetDelay(delay).SetEase(Ease.InQuad);
            rect.DOLocalMove(target, 0.45f).SetDelay(delay).SetEase(Ease.InBack, 1.2f)
                .OnComplete(_flyerLanded[index]);
        }

        private void Land(int flyerIndex)
        {
            int goalIndex = _flyerGoal[flyerIndex];
            flyers[flyerIndex].gameObject.SetActive(false);
            if (_game == null || goalIndex >= _pending.Length) return;

            _pending[goalIndex] = Mathf.Max(0, _pending[goalIndex] - 1);
            GoalItemView item = goalItems[goalIndex];
            int before = item.Shown;
            item.SetCount(_game.GoalRemaining[goalIndex] + _pending[goalIndex]);
            item.Punch();
            if (before > 0 && item.Shown == 0) AudioManager.Play(Sfx.Goal);
        }
    }
}

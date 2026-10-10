using BlastPuzzle.Gameplay;
using BlastPuzzle.UI;
using UnityEditor;
using UnityEngine;

namespace BlastPuzzle.EditorTools
{
    public static class SoakTest
    {
        private static ResultPopup _popup;
        private static double _popupShownAt = -1;
        private static bool _advanceOnWin;

        [MenuItem("BlastPuzzle/Tools/Soak Test/Play All Levels (bot)")]
        public static void PlayAllLevels() => Start(0, advanceOnWin: true);

        [MenuItem("BlastPuzzle/Tools/Soak Test/Loop Last Level (bot)")]
        public static void LoopLastLevel() => Start(int.MaxValue, advanceOnWin: false);

        [MenuItem("BlastPuzzle/Tools/Soak Test/Log Report")]
        public static void LogReport() => Debug.Log(Report());

        public static void Start(int levelIndex, bool advanceOnWin)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode first.");
                return;
            }

            var game = Object.FindFirstObjectByType<GameController>();
            if (game.GetComponent<DebugAutoPlayer>() == null)
                game.gameObject.AddComponent<DebugAutoPlayer>().Bind(game);
            if (DebugPerfProbe.Instance == null)
                game.gameObject.AddComponent<DebugPerfProbe>();

            _popup = Object.FindFirstObjectByType<ResultPopup>(FindObjectsInactive.Include);
            _advanceOnWin = advanceOnWin;
            _popupShownAt = -1;

            Object.FindFirstObjectByType<AppFlow>().StartLevel(levelIndex);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static string Report()
        {
            if (DebugPerfProbe.Instance == null) return "Soak test is not running.";

            var game = Object.FindFirstObjectByType<GameController>();
            var pool = Object.FindFirstObjectByType<View.TileViewPool>();
            var bot = game.GetComponent<DebugAutoPlayer>();
            return $"{DebugPerfProbe.Instance.Report()} | level={game.LevelIndex + 1} botMoves={bot.MovesPlayed} " +
                   $"pooledTiles={pool.CountAll} activeTweens={DG.Tweening.DOTween.TotalActiveTweens()}";
        }

        public static void ResetStats()
        {
            if (DebugPerfProbe.Instance != null) DebugPerfProbe.Instance.ResetStats();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || _popup == null)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!_popup.IsVisible)
            {
                _popupShownAt = -1;
                return;
            }

            if (_popupShownAt < 0)
            {
                _popupShownAt = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - _popupShownAt < 1.5) return;
            _popupShownAt = -1;

            if (_popup.IsWin && !_advanceOnWin) _popup.RetryButton.onClick.Invoke();
            else _popup.PrimaryButton.onClick.Invoke();
        }
    }
}

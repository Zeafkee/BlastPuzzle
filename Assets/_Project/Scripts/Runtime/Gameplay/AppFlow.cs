using BlastPuzzle.Data;
using BlastPuzzle.Save;
using BlastPuzzle.UI;
using UnityEngine;

namespace BlastPuzzle.Gameplay
{
    public sealed class AppFlow : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private GameController game;
        [SerializeField] private MenuScreen menu;
        [SerializeField] private UIPanel hudPanel;
        [SerializeField] private HudView hud;
        [SerializeField] private SettingsPopup settings;
        [SerializeField] private ResultPopup result;

        private void Awake()
        {
            menu.LevelSelected += StartLevel;
            menu.SettingsButton.onClick.AddListener(() => settings.Open(false));

            hud.PauseButton.onClick.AddListener(OpenPause);

            settings.CloseButton.onClick.AddListener(CloseSettings);
            settings.ResumeButton.onClick.AddListener(CloseSettings);
            settings.RestartButton.onClick.AddListener(() => { settings.Hide(); StartLevel(game.LevelIndex); });
            settings.HomeButton.onClick.AddListener(() => { settings.Hide(); GoToMenu(); });
            settings.ProgressReset += menu.Refresh;

            result.PrimaryButton.onClick.AddListener(HandleResultPrimary);
            result.RetryButton.onClick.AddListener(() => { result.Hide(); StartLevel(game.LevelIndex); });
            result.HomeButton.onClick.AddListener(() => { result.Hide(); GoToMenu(); });

            game.LevelEnded += HandleLevelEnded;
        }

        private void Start()
        {
            hudPanel.HideInstant();
            settings.HideInstant();
            result.HideInstant();
            menu.HideInstant();
            menu.Show();
        }

        public void StartLevel(int index)
        {
            menu.Hide();
            hudPanel.Show();
            game.StartLevel(index);
        }

        private void GoToMenu()
        {
            game.StopLevel();
            hudPanel.Hide();
            menu.Show();
        }

        private void OpenPause()
        {
            if (!game.IsRunning || result.IsVisible) return;
            game.SetInputEnabled(false);
            settings.Open(true);
        }

        private void CloseSettings()
        {
            settings.Hide();
            if (game.IsRunning) game.SetInputEnabled(true);
        }

        private void HandleLevelEnded(bool won, int stars)
        {
            int levelNumber = game.LevelIndex + 1;
            if (won)
            {
                SaveSystem.RecordWin(game.LevelIndex, stars, catalog.Count);
                result.ShowWin(levelNumber, stars, game.Game.Score, game.LevelIndex + 1 < catalog.Count);
            }
            else
            {
                result.ShowLose(levelNumber);
            }
        }

        private void HandleResultPrimary()
        {
            bool wasWin = result.IsWin;
            result.Hide();

            if (!wasWin)
                StartLevel(game.LevelIndex);
            else if (game.LevelIndex + 1 < catalog.Count)
                StartLevel(game.LevelIndex + 1);
            else
                GoToMenu();
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (settings.IsVisible) CloseSettings();
            else if (result.IsVisible) return;
            else if (game.IsRunning) OpenPause();
        }
    }
}

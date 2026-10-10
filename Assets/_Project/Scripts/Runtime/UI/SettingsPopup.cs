using BlastPuzzle.Audio;
using BlastPuzzle.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class SettingsPopup : UIPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private ToggleIcon soundToggle;
        [SerializeField] private ToggleIcon musicToggle;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject pauseButtons;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private GameObject menuButtons;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text resetLabel;

        private bool _resetArmed;

        public Button CloseButton => closeButton;
        public Button ResumeButton => resumeButton;
        public Button RestartButton => restartButton;
        public Button HomeButton => homeButton;

        public event System.Action ProgressReset;

        private void Awake()
        {
            soundToggle.Button.onClick.AddListener(ToggleSound);
            musicToggle.Button.onClick.AddListener(ToggleMusic);
            resetButton.onClick.AddListener(HandleReset);
        }

        public void Open(bool inLevel)
        {
            titleLabel.text = inLevel ? "PAUSED" : "SETTINGS";
            pauseButtons.SetActive(inLevel);
            menuButtons.SetActive(!inLevel);
            _resetArmed = false;
            resetLabel.text = "RESET PROGRESS";

            soundToggle.Set(SaveSystem.Data.soundOn);
            musicToggle.Set(SaveSystem.Data.musicOn);
            Show();
        }

        private void ToggleSound()
        {
            SaveSystem.Data.soundOn = !SaveSystem.Data.soundOn;
            SaveSystem.Save();
            soundToggle.Set(SaveSystem.Data.soundOn);
        }

        private void ToggleMusic()
        {
            SaveSystem.Data.musicOn = !SaveSystem.Data.musicOn;
            SaveSystem.Save();
            musicToggle.Set(SaveSystem.Data.musicOn);
            if (AudioManager.Instance != null) AudioManager.Instance.ApplySettings();
        }

        private void HandleReset()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                resetLabel.text = "TAP AGAIN TO CONFIRM";
                return;
            }

            _resetArmed = false;
            resetLabel.text = "PROGRESS RESET";
            SaveSystem.DeleteProgress();
            ProgressReset?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using BlastPuzzle.Data;
using BlastPuzzle.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class MenuScreen : UIPanel
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private LevelButton levelButtonPrefab;
        [SerializeField] private RectTransform gridContent;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playLabel;
        [SerializeField] private TMP_Text starTotalLabel;
        [SerializeField] private Button settingsButton;

        private readonly List<LevelButton> _buttons = new List<LevelButton>();
        private Action<int> _levelClicked;

        public event Action<int> LevelSelected;
        public Button SettingsButton => settingsButton;

        private void Awake()
        {
            _levelClicked = index => LevelSelected?.Invoke(index);
            gridContent.GetComponentsInChildren(true, _buttons);
            playButton.onClick.AddListener(() => LevelSelected?.Invoke(NextLevelIndex));
        }

        private int NextLevelIndex => Mathf.Clamp(SaveSystem.Data.highestUnlockedLevel, 0, catalog.Count - 1);

        protected override void OnShown() => Refresh();

        public void Refresh()
        {
            while (_buttons.Count < catalog.Count)
                _buttons.Add(Instantiate(levelButtonPrefab, gridContent));

            SaveData save = SaveSystem.Data;
            int totalStars = 0;

            for (int i = 0; i < _buttons.Count; i++)
            {
                bool exists = i < catalog.Count;
                _buttons[i].gameObject.SetActive(exists);
                if (!exists) continue;

                int stars = save.GetStars(i);
                totalStars += stars;

                LevelButtonState state = i > save.highestUnlockedLevel ? LevelButtonState.Locked
                    : stars > 0 ? LevelButtonState.Completed
                    : LevelButtonState.Current;
                _buttons[i].Setup(i, state, stars, _levelClicked);
            }

            playLabel.text = "LEVEL " + NumberStrings.Get(NextLevelIndex + 1);
            starTotalLabel.text = NumberStrings.Get(totalStars) + " / " + NumberStrings.Get(catalog.Count * 3);
        }
    }
}

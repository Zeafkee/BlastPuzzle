using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public enum LevelButtonState
    {
        Locked,
        Current,
        Completed,
    }

    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text numberLabel;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject starRow;
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite starFull;
        [SerializeField] private Sprite starEmpty;

        private int _index;
        private Action<int> _onClick;

        private void Awake() => button.onClick.AddListener(HandleClick);

        public void Setup(int index, LevelButtonState state, int starCount, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            bool locked = state == LevelButtonState.Locked;
            button.interactable = !locked;
            background.sprite = locked ? lockedSprite : state == LevelButtonState.Current ? currentSprite : completedSprite;

            lockIcon.SetActive(locked);
            numberLabel.gameObject.SetActive(!locked);
            numberLabel.text = NumberStrings.Get(index + 1);

            starRow.SetActive(state == LevelButtonState.Completed);
            for (int i = 0; i < stars.Length; i++)
                stars[i].sprite = i < starCount ? starFull : starEmpty;
        }

        private void HandleClick() => _onClick?.Invoke(_index);
    }
}

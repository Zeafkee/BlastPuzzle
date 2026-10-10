using BlastPuzzle.Audio;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class ResultPopup : UIPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private GameObject starRow;
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite starFull;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        private Sequence _starSequence;

        public Button PrimaryButton => primaryButton;
        public Button RetryButton => retryButton;
        public Button HomeButton => homeButton;
        public bool IsWin { get; private set; }

        public void ShowWin(int levelNumber, int starCount, int score, bool hasNextLevel)
        {
            IsWin = true;
            titleLabel.text = "LEVEL " + NumberStrings.Get(levelNumber);
            messageLabel.text = starCount >= 3 ? "PERFECT!" : starCount == 2 ? "GREAT!" : "WELL DONE!";
            scoreLabel.gameObject.SetActive(true);
            scoreLabel.text = "SCORE  " + score;
            primaryLabel.text = hasNextLevel ? "NEXT" : "MENU";
            retryButton.gameObject.SetActive(true);
            starRow.SetActive(true);

            Show();
            AnimateStars(starCount);
        }

        public void ShowLose(int levelNumber)
        {
            IsWin = false;
            titleLabel.text = "LEVEL " + NumberStrings.Get(levelNumber);
            messageLabel.text = "OUT OF MOVES";
            scoreLabel.gameObject.SetActive(false);
            primaryLabel.text = "TRY AGAIN";
            retryButton.gameObject.SetActive(false);
            starRow.SetActive(false);
            _starSequence?.Kill();
            Show();
        }

        private void AnimateStars(int starCount)
        {
            _starSequence?.Kill();
            _starSequence = DOTween.Sequence().SetUpdate(true).OnKill(() => _starSequence = null).AppendInterval(0.3f);

            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                star.sprite = starEmpty;
                star.transform.localScale = Vector3.one;
                if (i >= starCount) continue;

                float pitch = 1f + i * 0.12f;
                _starSequence.AppendCallback(() =>
                {
                    star.sprite = starFull;
                    star.transform.localScale = Vector3.one * 1.8f;
                    AudioManager.Play(Sfx.Star, pitch);
                });
                _starSequence.Append(star.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
            }
        }

        private void OnDisable() => _starSequence?.Kill();
    }
}

using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlastPuzzle.UI
{
    public sealed class Banner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text label;

        private Sequence _sequence;

        public void Show(string text, float holdSeconds = 0.9f)
        {
            _sequence?.Kill();
            gameObject.SetActive(true);
            label.text = text;
            group.alpha = 0f;
            body.localScale = Vector3.one * 0.6f;

            _sequence = DOTween.Sequence()
                .Append(group.DOFade(1f, 0.15f))
                .Join(body.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
                .AppendInterval(holdSeconds)
                .Append(group.DOFade(0f, 0.2f))
                .Join(body.DOScale(0.85f, 0.2f))
                .OnComplete(() => gameObject.SetActive(false))
                .OnKill(() => _sequence = null);
        }

        public void HideInstant()
        {
            _sequence?.Kill();
            gameObject.SetActive(false);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}

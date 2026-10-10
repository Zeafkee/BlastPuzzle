using DG.Tweening;
using UnityEngine;

namespace BlastPuzzle.UI
{
    public class UIPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private float showDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.15f;

        public bool IsVisible { get; private set; }

        public void Show()
        {
            if (IsVisible) return;
            IsVisible = true;

            KillTweens();
            gameObject.SetActive(true);
            group.alpha = 0f;
            group.interactable = true;
            group.blocksRaycasts = true;
            group.DOFade(1f, showDuration).SetUpdate(true);

            if (window != null)
            {
                window.localScale = Vector3.one * 0.75f;
                window.DOScale(1f, showDuration * 1.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            OnShown();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;

            KillTweens();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.DOFade(0f, hideDuration).SetUpdate(true).OnComplete(Deactivate);
        }

        public void HideInstant()
        {
            IsVisible = false;
            KillTweens();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        protected virtual void OnShown() { }

        private void Deactivate() => gameObject.SetActive(false);

        private void KillTweens()
        {
            group.DOKill();
            if (window != null) window.DOKill();
        }

        private void OnDestroy() => KillTweens();

#if UNITY_EDITOR
        private void Reset() => group = GetComponent<CanvasGroup>();
#endif
    }
}

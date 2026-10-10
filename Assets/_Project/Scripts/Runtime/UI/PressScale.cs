using BlastPuzzle.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private Selectable selectable;

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            if (selectable == null) selectable = GetComponent<Selectable>();
        }

        private bool Interactable => selectable == null || selectable.IsInteractable();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable) return;
            _transform.DOKill();
            _transform.DOScale(pressedScale, 0.08f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _transform.DOKill();
            _transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Interactable) AudioManager.Play(Sfx.Click);
        }

        private void OnDisable()
        {
            _transform.DOKill();
            _transform.localScale = Vector3.one;
        }
    }
}

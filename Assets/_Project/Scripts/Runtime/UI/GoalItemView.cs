using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class GoalItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private GameObject check;

        private int _shown = -1;

        public RectTransform IconTransform => icon.rectTransform;
        public int Shown => _shown;

        public void Setup(Sprite sprite, int count)
        {
            icon.sprite = sprite;
            _shown = -1;
            SetCount(count);
        }

        public void SetCount(int count)
        {
            if (count < 0) count = 0;
            if (count == _shown) return;
            _shown = count;

            bool done = count == 0;
            check.SetActive(done);
            countLabel.gameObject.SetActive(!done);
            if (!done) countLabel.text = NumberStrings.Get(count);
        }

        public void Punch()
        {
            Transform t = icon.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * 0.25f, 0.2f, 6, 0.6f);
        }

        private void OnDisable()
        {
            icon.transform.DOKill();
            icon.transform.localScale = Vector3.one;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.UI
{
    public sealed class ToggleIcon : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Image background;
        [SerializeField] private Sprite onBackground;
        [SerializeField] private Sprite offBackground;

        public Button Button => button;
        public bool IsOn { get; private set; }

        public void Set(bool on)
        {
            IsOn = on;
            icon.sprite = on ? onSprite : offSprite;
            if (background != null && onBackground != null)
                background.sprite = on ? onBackground : offBackground;
        }
    }
}

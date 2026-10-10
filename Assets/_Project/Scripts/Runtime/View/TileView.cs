using UnityEngine;

namespace BlastPuzzle.View
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        public const int NormalOrder = 0;
        public const int RaisedOrder = 10;

        [SerializeField] private SpriteRenderer spriteRenderer;

        public Transform CachedTransform { get; private set; }

        internal int AnimationSlot = -1;

        private void Awake()
        {
            CachedTransform = transform;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (!ReferenceEquals(spriteRenderer.sprite, sprite))
                spriteRenderer.sprite = sprite;
        }

        public void SetRaised(bool raised) => spriteRenderer.sortingOrder = raised ? RaisedOrder : NormalOrder;

        public void SetAlpha(float alpha)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        public void ResetVisual()
        {
            CachedTransform.localScale = Vector3.one;
            CachedTransform.localRotation = Quaternion.identity;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = NormalOrder;
        }

#if UNITY_EDITOR
        private void Reset() => spriteRenderer = GetComponent<SpriteRenderer>();
#endif
    }
}

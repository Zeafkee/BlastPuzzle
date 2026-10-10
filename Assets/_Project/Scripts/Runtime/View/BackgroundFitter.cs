using UnityEngine;

namespace BlastPuzzle.View
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float _lastSize = -1f;
        private float _lastAspect = -1f;
        private Vector3 _lastCameraPosition;

        private void LateUpdate()
        {
            if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

            Vector3 cameraPosition = targetCamera.transform.position;
            if (Mathf.Approximately(targetCamera.orthographicSize, _lastSize) &&
                Mathf.Approximately(targetCamera.aspect, _lastAspect) &&
                cameraPosition == _lastCameraPosition)
                return;

            _lastSize = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;
            _lastCameraPosition = cameraPosition;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float viewHeight = _lastSize * 2f;
            float viewWidth = viewHeight * _lastAspect;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;

            Transform t = transform;
            t.localScale = new Vector3(scale, scale, 1f);
            t.position = new Vector3(cameraPosition.x, cameraPosition.y, t.position.z);
        }

#if UNITY_EDITOR
        private void Reset() => spriteRenderer = GetComponent<SpriteRenderer>();
#endif
    }
}

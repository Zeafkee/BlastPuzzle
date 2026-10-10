using BlastPuzzle.Core;
using UnityEngine;

namespace BlastPuzzle.View
{
    public sealed class BoardLayout : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float padding = 0.45f;
        [SerializeField, Range(0f, 0.5f)] private float topHudFraction = 0.2f;
        [SerializeField, Range(0f, 0.5f)] private float bottomHudFraction = 0.05f;

        private int _width;
        private int _height;

        public float CellSize => cellSize;
        public int Width => _width;
        public int Height => _height;
        public Vector2 BoardSize => new Vector2(_width * cellSize, _height * cellSize);

        public void Configure(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public Vector3 GridToLocal(float x, float y)
        {
            return new Vector3(
                (x - (_width - 1) * 0.5f) * cellSize,
                (y - (_height - 1) * 0.5f) * cellSize,
                0f);
        }

        public Vector3 GridToLocal(GridPos p) => GridToLocal(p.X, p.Y);

        public Vector3 GridToWorld(GridPos p) => transform.TransformPoint(GridToLocal(p.X, p.Y));

        public bool TryWorldToGrid(Vector3 world, out GridPos pos)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            int x = Mathf.FloorToInt(local.x / cellSize + _width * 0.5f);
            int y = Mathf.FloorToInt(local.y / cellSize + _height * 0.5f);
            pos = new GridPos(x, y);
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        public void FitCamera(Camera cam)
        {
            if (!cam.orthographic || Screen.height <= 0 || _width == 0) return;

            Rect safe = Screen.safeArea;
            float topFraction = topHudFraction + (Screen.height - safe.yMax) / Screen.height;
            float bottomFraction = bottomHudFraction + safe.yMin / Screen.height;
            float usableFraction = Mathf.Max(0.1f, 1f - topFraction - bottomFraction);

            float boardWorldWidth = (_width + padding * 2f) * cellSize;
            float boardWorldHeight = (_height + padding * 2f) * cellSize;

            float sizeForHeight = boardWorldHeight / usableFraction * 0.5f;
            float sizeForWidth = boardWorldWidth / cam.aspect * 0.5f;
            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

            float visibleHeight = cam.orthographicSize * 2f;
            float bandCenterOffset = (bottomFraction - topFraction) * 0.5f * visibleHeight;
            Vector3 boardCenter = transform.position;
            cam.transform.position = new Vector3(boardCenter.x, boardCenter.y - bandCenterOffset, cam.transform.position.z);
        }
    }
}

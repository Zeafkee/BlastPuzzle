using System.Collections.Generic;
using UnityEngine;

namespace BlastPuzzle.View
{
    public sealed class TileViewPool : MonoBehaviour
    {
        [SerializeField] private TileView prefab;
        [SerializeField] private Transform parent;
        [SerializeField] private int prewarmCount = 128;

        private readonly Stack<TileView> _free = new Stack<TileView>(256);

        public int CountInactive => _free.Count;
        public int CountAll { get; private set; }

        private void Awake()
        {
            if (parent == null) parent = transform;
            for (int i = 0; i < prewarmCount; i++)
                _free.Push(Create());
        }

        public TileView Get()
        {
            TileView view = _free.Count > 0 ? _free.Pop() : Create();
            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(TileView view)
        {
            view.ResetVisual();
            view.gameObject.SetActive(false);
            _free.Push(view);
        }

        private TileView Create()
        {
            TileView view = Instantiate(prefab, parent);
            view.gameObject.SetActive(false);
            CountAll++;
            return view;
        }
    }
}

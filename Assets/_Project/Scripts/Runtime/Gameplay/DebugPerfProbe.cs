#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace BlastPuzzle.Gameplay
{
    [DefaultExecutionOrder(-32000)]
    public sealed class DebugPerfProbe : MonoBehaviour
    {
        private long _frameStartBytes;
        private long _totalBytes;
        private long _maxBytes;
        private int _frames;
        private int _framesWithAllocation;
        private float _totalTime;
        private float _maxFrameTime;

        public static DebugPerfProbe Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            gameObject.AddComponent<DebugPerfProbeFrameEnd>().Owner = this;
        }

        private void Update()
        {
            _frameStartBytes = GC.GetTotalMemory(false);
        }

        internal void EndFrame()
        {
            long allocated = GC.GetTotalMemory(false) - _frameStartBytes;
            if (allocated < 0) allocated = 0;
            _totalBytes += allocated;
            if (allocated > _maxBytes) _maxBytes = allocated;
            if (allocated > 0) _framesWithAllocation++;

            float dt = Time.unscaledDeltaTime;
            _totalTime += dt;
            if (dt > _maxFrameTime) _maxFrameTime = dt;
            _frames++;
        }

        public void ResetStats()
        {
            _totalBytes = _maxBytes = 0;
            _frames = _framesWithAllocation = 0;
            _totalTime = _maxFrameTime = 0f;
        }

        public string Report()
        {
            if (_frames == 0) return "no frames measured";
            return $"frames={_frames} avgFrame={_totalTime / _frames * 1000f:0.00}ms maxFrame={_maxFrameTime * 1000f:0.0}ms " +
                   $"allocTotal={_totalBytes}B allocPerFrame={(float)_totalBytes / _frames:0.0}B allocMax={_maxBytes}B " +
                   $"framesWithAlloc={_framesWithAllocation}";
        }
    }

    [DefaultExecutionOrder(32000)]
    internal sealed class DebugPerfProbeFrameEnd : MonoBehaviour
    {
        public DebugPerfProbe Owner;
        private void LateUpdate() => Owner.EndFrame();
    }
}
#endif

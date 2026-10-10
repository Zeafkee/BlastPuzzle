using BlastPuzzle.Audio;
using UnityEngine;

namespace BlastPuzzle.View
{
    public sealed class TileAnimator : MonoBehaviour
    {
        private enum Kind : byte { Fall, Pop, Merge, Move, Appear, Pulse, Shake }

        private struct Anim
        {
            public TileView View;
            public Transform Transform;
            public Kind Kind;
            public bool Blocking;
            public bool Landed;
            public bool FadeIn;
            public bool Burst;
            public float Delay;
            public float Time;
            public float Duration;
            public float Velocity;
            public float Strength;
            public Vector3 From;
            public Vector3 To;
            public Color32 BurstColor;
        }

        [SerializeField] private TileViewPool pool;
        [SerializeField] private FxPlayer fx;

        [Header("Fall")]
        [SerializeField] private float gravity = 70f;
        [SerializeField] private float startSpeed = 5f;
        [SerializeField] private float maxSpeed = 26f;
        [SerializeField] private float squashDuration = 0.16f;
        [SerializeField] private float maxSquash = 0.16f;

        [Header("Durations")]
        [SerializeField] private float popDuration = 0.17f;
        [SerializeField] private float mergeDuration = 0.15f;
        [SerializeField] private float appearDuration = 0.28f;

        private Anim[] _anims = new Anim[256];
        private int _count;
        private int _blockingCount;
        private float _fadeTopY = float.MaxValue;

        public bool IsBusy => _blockingCount > 0;

        public void SetFadeLine(float localTopY) => _fadeTopY = localTopY;

        public void Fall(TileView view, Vector3 from, Vector3 to, float delay, bool fadeIn)
        {
            ref Anim a = ref Add(view, Kind.Fall, true, delay);
            a.From = from;
            a.To = to;
            a.Velocity = startSpeed;
            a.FadeIn = fadeIn;
            view.CachedTransform.localPosition = from;
            if (fadeIn) view.SetAlpha(AlphaAt(from.y));
        }

        public void Pop(TileView view, float delay, bool burst, Color32 burstColor)
        {
            ref Anim a = ref Add(view, Kind.Pop, true, delay);
            a.Duration = popDuration;
            a.Burst = burst;
            a.BurstColor = burstColor;
            a.From = view.CachedTransform.localPosition;
            view.SetRaised(true);
        }

        public void Merge(TileView view, Vector3 target, float delay)
        {
            ref Anim a = ref Add(view, Kind.Merge, true, delay);
            a.Duration = mergeDuration;
            a.From = view.CachedTransform.localPosition;
            a.To = target;
            view.SetRaised(true);
        }

        public void Move(TileView view, Vector3 to, float duration, float delay)
        {
            ref Anim a = ref Add(view, Kind.Move, true, delay);
            a.Duration = duration;
            a.From = view.CachedTransform.localPosition;
            a.To = to;
        }

        public void Appear(TileView view, float delay)
        {
            ref Anim a = ref Add(view, Kind.Appear, true, delay);
            a.Duration = appearDuration;
            view.CachedTransform.localScale = Vector3.zero;
        }

        public void Pulse(TileView view, float delay)
        {
            if (view.AnimationSlot >= 0) return;
            ref Anim a = ref Add(view, Kind.Pulse, false, delay);
            a.Duration = 0.55f;
        }

        public void Shake(TileView view)
        {
            if (view.AnimationSlot >= 0) return;
            ref Anim a = ref Add(view, Kind.Shake, false, 0f);
            a.Duration = 0.28f;
            a.From = view.CachedTransform.localPosition;
        }

        public void StopIdleAnimations()
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                if (_anims[i].Blocking) continue;
                Settle(ref _anims[i]);
                RemoveAt(i);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _anims[i].View.AnimationSlot = -1;
                _anims[i] = default;
            }

            _count = 0;
            _blockingCount = 0;
        }

        private void Update()
        {
            if (_count == 0) return;
            float dt = Time.deltaTime;

            for (int i = _count - 1; i >= 0; i--)
            {
                ref Anim a = ref _anims[i];

                if (a.Delay > 0f)
                {
                    a.Delay -= dt;
                    if (a.Delay > 0f) continue;
                }

                bool done;
                switch (a.Kind)
                {
                    case Kind.Fall: done = StepFall(ref a, dt); break;
                    case Kind.Pop: done = StepPop(ref a, dt); break;
                    case Kind.Merge: done = StepMerge(ref a, dt); break;
                    case Kind.Move: done = StepMove(ref a, dt); break;
                    case Kind.Appear: done = StepAppear(ref a, dt); break;
                    case Kind.Pulse: done = StepPulse(ref a, dt); break;
                    default: done = StepShake(ref a, dt); break;
                }

                if (!done) continue;

                TileView view = a.View;
                bool release = a.Kind == Kind.Pop || a.Kind == Kind.Merge;
                RemoveAt(i);
                if (release) pool.Release(view);
            }
        }

        private bool StepFall(ref Anim a, float dt)
        {
            if (!a.Landed)
            {
                a.Velocity = Mathf.Min(a.Velocity + gravity * dt, maxSpeed);
                Vector3 p = a.Transform.localPosition;
                p.y -= a.Velocity * dt;

                if (p.y > a.To.y)
                {
                    a.Transform.localPosition = p;
                    if (a.FadeIn) a.View.SetAlpha(AlphaAt(p.y));
                    return false;
                }

                a.Landed = true;
                a.Time = 0f;
                a.Strength = maxSquash * Mathf.Clamp01(a.Velocity / maxSpeed);
                a.Transform.localPosition = a.To;
                if (a.FadeIn) a.View.SetAlpha(1f);
                AudioManager.Play(Sfx.Land, 0.95f + a.Strength);
            }

            a.Time += dt;
            float k = a.Time / squashDuration;
            if (k >= 1f)
            {
                a.Transform.localScale = Vector3.one;
                a.Transform.localPosition = a.To;
                return true;
            }

            float s = Mathf.Sin(k * Mathf.PI) * a.Strength;
            a.Transform.localScale = new Vector3(1f + s * 0.6f, 1f - s, 1f);
            a.Transform.localPosition = new Vector3(a.To.x, a.To.y - s * 0.5f, a.To.z);
            return false;
        }

        private bool StepPop(ref Anim a, float dt)
        {
            if (a.Time == 0f && a.Burst && fx != null)
                fx.Burst(a.From, a.BurstColor, 5);

            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            float scale = k < 0.3f ? Mathf.Lerp(1f, 1.2f, k / 0.3f) : Mathf.Lerp(1.2f, 0f, (k - 0.3f) / 0.7f);
            a.Transform.localScale = new Vector3(scale, scale, 1f);
            return k >= 1f;
        }

        private bool StepMerge(ref Anim a, float dt)
        {
            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            float eased = k * k;
            a.Transform.localPosition = Vector3.LerpUnclamped(a.From, a.To, eased);
            float scale = Mathf.Lerp(1f, 0.45f, eased);
            a.Transform.localScale = new Vector3(scale, scale, 1f);
            return k >= 1f;
        }

        private bool StepMove(ref Anim a, float dt)
        {
            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            float eased = k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;
            a.Transform.localPosition = Vector3.LerpUnclamped(a.From, a.To, eased);
            float scale = 1f - 0.25f * Mathf.Sin(k * Mathf.PI);
            a.Transform.localScale = new Vector3(scale, scale, 1f);
            return k >= 1f;
        }

        private bool StepAppear(ref Anim a, float dt)
        {
            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t = k - 1f;
            float scale = 1f + c3 * t * t * t + c1 * t * t;
            a.Transform.localScale = new Vector3(scale, scale, 1f);
            return k >= 1f;
        }

        private bool StepPulse(ref Anim a, float dt)
        {
            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            float scale = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
            a.Transform.localScale = new Vector3(scale, scale, 1f);
            return k >= 1f;
        }

        private bool StepShake(ref Anim a, float dt)
        {
            a.Time += dt;
            float k = Mathf.Clamp01(a.Time / a.Duration);
            float offset = Mathf.Sin(k * Mathf.PI * 5f) * 0.09f * (1f - k);
            a.Transform.localPosition = new Vector3(a.From.x + offset, a.From.y, a.From.z);
            return k >= 1f;
        }

        private ref Anim Add(TileView view, Kind kind, bool blocking, float delay)
        {
            int slot = view.AnimationSlot;
            if (slot >= 0)
            {
                Settle(ref _anims[slot]);
                if (_anims[slot].Blocking) _blockingCount--;
            }
            else
            {
                if (_count == _anims.Length)
                    System.Array.Resize(ref _anims, _anims.Length * 2);
                slot = _count++;
                view.AnimationSlot = slot;
            }

            _anims[slot] = new Anim
            {
                View = view,
                Transform = view.CachedTransform,
                Kind = kind,
                Blocking = blocking,
                Delay = delay,
            };

            if (blocking) _blockingCount++;
            return ref _anims[slot];
        }

        private void RemoveAt(int index)
        {
            if (_anims[index].Blocking) _blockingCount--;
            _anims[index].View.AnimationSlot = -1;

            int last = --_count;
            if (index != last)
            {
                _anims[index] = _anims[last];
                _anims[index].View.AnimationSlot = index;
            }

            _anims[last] = default;
        }

        private static void Settle(ref Anim a)
        {
            if (a.Kind == Kind.Pulse) a.Transform.localScale = Vector3.one;
            else if (a.Kind == Kind.Shake) a.Transform.localPosition = a.From;
        }

        private float AlphaAt(float localY) => Mathf.Clamp01((_fadeTopY + 0.55f - localY) / 0.5f);
    }
}

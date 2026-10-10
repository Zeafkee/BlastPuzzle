using DG.Tweening;
using UnityEngine;

namespace BlastPuzzle.View
{
    public sealed class FxPlayer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem shards;
        [SerializeField] private ParticleSystem sparkles;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private SpriteRenderer[] rings;
        [SerializeField] private SpriteRenderer[] beams;
        [SerializeField] private Transform shakeTarget;

        private int _nextRing;
        private int _nextBeam;
        private Vector3 _shakeRestPosition;

        private TweenCallback[] _hideRing;
        private TweenCallback[] _hideBeam;
        private TweenCallback _resetShake;

        private void Awake()
        {
            if (shakeTarget != null) _shakeRestPosition = shakeTarget.localPosition;

            _hideRing = new TweenCallback[rings.Length];
            for (int i = 0; i < rings.Length; i++)
            {
                GameObject go = rings[i].gameObject;
                go.SetActive(false);
                _hideRing[i] = () => go.SetActive(false);
            }

            _hideBeam = new TweenCallback[beams.Length];
            for (int i = 0; i < beams.Length; i++)
            {
                GameObject go = beams[i].gameObject;
                go.SetActive(false);
                _hideBeam[i] = () => go.SetActive(false);
            }

            _resetShake = () => shakeTarget.localPosition = _shakeRestPosition;
        }

        public void Burst(Vector3 localPosition, Color32 color, int count)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = localPosition,
                startColor = color,
                applyShapeToPosition = true,
            };
            shards.Emit(emit, count);
        }

        public void Sparkle(Vector3 localPosition, int count)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = localPosition,
                applyShapeToPosition = true,
            };
            sparkles.Emit(emit, count);
        }

        public void Ring(Vector3 localPosition, float endScale, Color color, float duration = 0.35f)
        {
            if (rings.Length == 0) return;
            int index = _nextRing;
            SpriteRenderer ring = rings[index];
            _nextRing = (_nextRing + 1) % rings.Length;

            Transform t = ring.transform;
            t.DOKill();
            ring.DOKill();

            ring.gameObject.SetActive(true);
            t.localPosition = localPosition;
            t.localScale = Vector3.one * 0.2f;
            ring.color = color;

            t.DOScale(endScale, duration).SetEase(Ease.OutCubic);
            ring.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(_hideRing[index]);
        }

        public void RocketTrails(Vector3 localPosition, bool vertical, float length, float duration = 0.3f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                if (beams.Length == 0) return;
                int index = _nextBeam;
                SpriteRenderer beam = beams[index];
                _nextBeam = (_nextBeam + 1) % beams.Length;

                Transform t = beam.transform;
                t.DOKill();
                beam.DOKill();

                beam.gameObject.SetActive(true);
                beam.color = Color.white;
                t.localPosition = localPosition;
                float angle = vertical ? (side > 0 ? 90f : -90f) : (side > 0 ? 0f : 180f);
                t.localRotation = Quaternion.Euler(0f, 0f, angle);
                t.localScale = new Vector3(1.2f, 1f, 1f);

                Vector3 direction = vertical ? new Vector3(0f, side, 0f) : new Vector3(side, 0f, 0f);
                t.DOLocalMove(localPosition + direction * length, duration).SetEase(Ease.OutQuad);
                t.DOScaleX(3.2f, duration);
                beam.DOFade(0f, duration * 0.5f).SetDelay(duration * 0.6f).OnComplete(_hideBeam[index]);
            }
        }

        public void Shake(float strength, float duration = 0.25f)
        {
            if (shakeTarget == null) return;
            shakeTarget.DOKill();
            shakeTarget.localPosition = _shakeRestPosition;
            shakeTarget.DOShakePosition(duration, strength, 22, 90f, false, true).OnComplete(_resetShake);
        }

        public void Confetti()
        {
            if (confetti != null) confetti.Play();
        }

        public void StopAll()
        {
            shards.Clear();
            sparkles.Clear();
            if (confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

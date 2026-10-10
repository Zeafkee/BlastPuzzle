using System;
using BlastPuzzle.Save;
using UnityEngine;

namespace BlastPuzzle.Audio
{
    public enum Sfx
    {
        Pop = 0,
        Land,
        Invalid,
        Click,
        Rocket,
        Bomb,
        Disco,
        BoosterCreate,
        BoxHit,
        BoxBreak,
        Shuffle,
        Star,
        Goal,
        Win,
        Lose,
    }

    [Serializable]
    public struct SfxEntry
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SfxEntry[] effects;
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField] private int voiceCount = 6;

        private AudioSource[] _voices;
        private AudioSource _musicSource;
        private AudioClip[] _clips;
        private float[] _volumes;
        private int[] _lastPlayedFrame;
        private int _nextVoice;

        private void Awake()
        {
            Instance = this;

            int count = Enum.GetValues(typeof(Sfx)).Length;
            _clips = new AudioClip[count];
            _volumes = new float[count];
            _lastPlayedFrame = new int[count];
            for (int i = 0; i < effects.Length; i++)
            {
                int id = (int)effects[i].id;
                _clips[id] = effects[i].clip;
                _volumes[id] = effects[i].volume;
                _lastPlayedFrame[id] = -1;
            }

            _voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = music;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = musicVolume;
        }

        private void Start() => ApplySettings();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(Sfx id, float pitch = 1f, float volumeScale = 1f)
        {
            if (Instance != null) Instance.PlayInternal(id, pitch, volumeScale);
        }

        public void ApplySettings()
        {
            if (music == null) return;

            bool musicOn = SaveSystem.Data.musicOn;
            if (musicOn && !_musicSource.isPlaying) _musicSource.Play();
            else if (!musicOn && _musicSource.isPlaying) _musicSource.Stop();
        }

        private void PlayInternal(Sfx id, float pitch, float volumeScale)
        {
            if (!SaveSystem.Data.soundOn) return;

            int index = (int)id;
            AudioClip clip = _clips[index];
            if (clip == null || _lastPlayedFrame[index] == Time.frameCount) return;
            _lastPlayedFrame[index] = Time.frameCount;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            voice.pitch = pitch;
            voice.volume = _volumes[index] * volumeScale;
            voice.clip = clip;
            voice.Play();
        }
    }
}

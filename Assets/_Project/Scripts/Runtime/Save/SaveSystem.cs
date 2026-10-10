using System;
using UnityEngine;

namespace BlastPuzzle.Save
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int highestUnlockedLevel;
        public int[] stars = new int[0];
        public bool soundOn = true;
        public bool musicOn = true;

        public int GetStars(int levelIndex) =>
            levelIndex >= 0 && levelIndex < stars.Length ? stars[levelIndex] : 0;
    }

    public static class SaveSystem
    {
        private const string Key = "blastpuzzle.save";

        private static SaveData _data;

        public static SaveData Data => _data ??= Load();

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void RecordWin(int levelIndex, int starCount, int levelCount)
        {
            SaveData data = Data;
            if (data.stars.Length <= levelIndex)
                Array.Resize(ref data.stars, levelIndex + 1);

            data.stars[levelIndex] = Mathf.Max(data.stars[levelIndex], starCount);
            data.highestUnlockedLevel = Mathf.Clamp(Mathf.Max(data.highestUnlockedLevel, levelIndex + 1), 0, levelCount - 1);
            Save();
        }

        public static void DeleteProgress()
        {
            SaveData data = Data;
            data.highestUnlockedLevel = 0;
            data.stars = new int[0];
            Save();
        }

        private static SaveData Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new SaveData();

            try
            {
                SaveData loaded = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                loaded.stars ??= new int[0];
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save data corrupted, starting fresh. {e.Message}");
                return new SaveData();
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _data = null;
#endif
    }
}

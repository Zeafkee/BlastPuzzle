using UnityEngine;

namespace BlastPuzzle.Data
{
    [CreateAssetMenu(menuName = "BlastPuzzle/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelData[] levels = new LevelData[0];

        public int Count => levels.Length;
        public LevelData Get(int index) => levels[Mathf.Clamp(index, 0, levels.Length - 1)];
    }
}

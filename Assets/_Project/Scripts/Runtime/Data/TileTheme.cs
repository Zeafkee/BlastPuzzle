using System;
using BlastPuzzle.Core;
using UnityEngine;

namespace BlastPuzzle.Data
{
    [Serializable]
    public struct ColorSprites
    {
        public string name;
        public Color tint;
        public Sprite[] tiers;
        public Sprite disco;
    }

    [CreateAssetMenu(menuName = "BlastPuzzle/Tile Theme", fileName = "TileTheme")]
    public sealed class TileTheme : ScriptableObject
    {
        public ColorSprites[] colors = new ColorSprites[6];
        public Sprite rocket;
        public Sprite bomb;
        public Sprite box;
        public Sprite boxReinforced;
        public Color boxTint = new Color(0.8f, 0.55f, 0.3f);

        public Sprite GetSprite(Tile tile, int tier)
        {
            switch (tile.Kind)
            {
                case TileKind.Color:
                    Sprite[] tiers = colors[tile.Color].tiers;
                    return tiers[Mathf.Clamp(tier, 0, tiers.Length - 1)];
                case TileKind.Rocket: return rocket;
                case TileKind.Bomb: return bomb;
                case TileKind.Disco: return colors[tile.Color].disco;
                case TileKind.Box: return tile.Value >= 2 ? boxReinforced : box;
                default: return null;
            }
        }

        public Color GetTint(Tile tile)
        {
            switch (tile.Kind)
            {
                case TileKind.Color:
                case TileKind.Disco: return colors[tile.Color].tint;
                case TileKind.Box: return boxTint;
                default: return Color.white;
            }
        }

        public Sprite GetGoalIcon(GoalConfig goal) =>
            goal.Kind == GoalKind.Box ? box : colors[goal.Color].tiers[0];
    }
}

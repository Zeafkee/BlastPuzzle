using System;

namespace BlastPuzzle.Core
{
    public enum GoalKind : byte
    {
        Color = 0,
        Box = 1,
    }

    public readonly struct GoalConfig
    {
        public readonly GoalKind Kind;
        public readonly int Color;
        public readonly int Count;

        public GoalConfig(GoalKind kind, int color, int count)
        {
            Kind = kind;
            Color = color;
            Count = count;
        }

        public static GoalConfig CollectColor(int color, int count) => new GoalConfig(GoalKind.Color, color, count);
        public static GoalConfig DestroyBoxes(int count) => new GoalConfig(GoalKind.Box, 0, count);
    }

    public sealed class LevelConfig
    {
        public int Width = 8;
        public int Height = 8;
        public int ColorCount = 4;
        public int Moves = 20;
        public TierRules Tiers = new TierRules(4, 6, 8);
        public GoalConfig[] Goals = Array.Empty<GoalConfig>();

        public string[] Layout;

        public void Validate()
        {
            if (Width < Board.MinSize || Width > Board.MaxSize) throw new ArgumentException($"Width {Width} out of range.");
            if (Height < Board.MinSize || Height > Board.MaxSize) throw new ArgumentException($"Height {Height} out of range.");
            if (ColorCount < 1 || ColorCount > 6) throw new ArgumentException($"ColorCount {ColorCount} out of range.");
            if (Moves < 1) throw new ArgumentException("Moves must be positive.");

            if (Layout != null && Layout.Length > 0)
            {
                if (Layout.Length != Height) throw new ArgumentException($"Layout has {Layout.Length} rows, expected {Height}.");
                for (int row = 0; row < Layout.Length; row++)
                {
                    if (Layout[row].Length != Width)
                        throw new ArgumentException($"Layout row {row} has {Layout[row].Length} cells, expected {Width}.");
                }
            }
        }
    }
}

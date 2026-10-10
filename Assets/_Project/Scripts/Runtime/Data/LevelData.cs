using System;
using System.Collections.Generic;
using BlastPuzzle.Core;
using UnityEngine;

namespace BlastPuzzle.Data
{
    [Serializable]
    public struct GoalEntry
    {
        public GoalKind kind;
        [Range(0, 5)] public int color;
        [Min(1)] public int count;
    }

    [CreateAssetMenu(menuName = "BlastPuzzle/Level", fileName = "Level_00")]
    public sealed class LevelData : ScriptableObject
    {
        [Header("Board")]
        [Range(Board.MinSize, Board.MaxSize)] public int width = 8;
        [Range(Board.MinSize, Board.MaxSize)] public int height = 8;
        [Range(1, 6)] public int colorCount = 4;

        [TextArea(3, 10)] public string layout;

        [Header("Rules")]
        [Min(1)] public int moves = 20;
        public List<GoalEntry> goals = new List<GoalEntry>();

        [Header("Icon tiers / boosters (group size MORE THAN threshold)")]
        [Min(1)] public int tierA = 4;
        [Min(2)] public int tierB = 6;
        [Min(3)] public int tierC = 8;

        public LevelConfig ToConfig()
        {
            var config = new LevelConfig
            {
                Width = width,
                Height = height,
                ColorCount = colorCount,
                Moves = moves,
                Tiers = new TierRules(tierA, tierB, tierC),
                Goals = new GoalConfig[goals.Count],
                Layout = ParseLayout(),
            };

            for (int i = 0; i < goals.Count; i++)
                config.Goals[i] = new GoalConfig(goals[i].kind, goals[i].color, goals[i].count);

            return config;
        }

        private string[] ParseLayout()
        {
            if (string.IsNullOrWhiteSpace(layout)) return null;

            string[] lines = layout.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim();
            return lines;
        }

        private void OnValidate()
        {
            tierB = Mathf.Max(tierB, tierA + 1);
            tierC = Mathf.Max(tierC, tierB + 1);
            for (int i = 0; i < goals.Count; i++)
            {
                GoalEntry goal = goals[i];
                goal.color = Mathf.Clamp(goal.color, 0, colorCount - 1);
                goals[i] = goal;
            }
        }
    }
}

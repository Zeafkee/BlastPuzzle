using System;

namespace BlastPuzzle.Core
{
    public readonly struct TierRules
    {
        public readonly int A;
        public readonly int B;
        public readonly int C;

        public TierRules(int a, int b, int c)
        {
            if (a < 1 || b <= a || c <= b)
                throw new ArgumentException($"Thresholds must satisfy 1 <= A < B < C (got {a}, {b}, {c}).");
            A = a;
            B = b;
            C = c;
        }

        public int GetTier(int groupSize)
        {
            if (groupSize > C) return 3;
            if (groupSize > B) return 2;
            if (groupSize > A) return 1;
            return 0;
        }
    }
}

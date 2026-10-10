using System;

namespace BlastPuzzle.Core
{
    public enum TileKind : byte
    {
        Empty = 0,
        Color = 1,
        Rocket = 2,
        Bomb = 3,
        Disco = 4,
        Box = 5,
    }

    public readonly struct Tile : IEquatable<Tile>
    {
        public static readonly Tile Empty = default;
        public static readonly Tile Bomb = new Tile(TileKind.Bomb, 0);

        public readonly TileKind Kind;
        public readonly byte Value;

        private Tile(TileKind kind, byte value)
        {
            Kind = kind;
            Value = value;
        }

        public static Tile OfColor(int color)
        {
            if (color < 0 || color > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(color));
            return new Tile(TileKind.Color, (byte)color);
        }

        public static Tile Rocket(bool vertical) => new Tile(TileKind.Rocket, (byte)(vertical ? 1 : 0));
        public static Tile Disco(int color) => new Tile(TileKind.Disco, (byte)color);
        public static Tile Box(int hitPoints) => new Tile(TileKind.Box, (byte)hitPoints);

        public byte Color => Value;

        public bool IsEmpty => Kind == TileKind.Empty;
        public bool IsColor => Kind == TileKind.Color;
        public bool IsBox => Kind == TileKind.Box;
        public bool IsBooster => Kind == TileKind.Rocket || Kind == TileKind.Bomb || Kind == TileKind.Disco;
        public bool IsVerticalRocket => Kind == TileKind.Rocket && Value == 1;

        public bool Matches(Tile other) =>
            Kind == TileKind.Color && other.Kind == TileKind.Color && Value == other.Value;

        public bool Equals(Tile other) => Kind == other.Kind && Value == other.Value;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => ((int)Kind << 8) | Value;
        public static bool operator ==(Tile a, Tile b) => a.Equals(b);
        public static bool operator !=(Tile a, Tile b) => !a.Equals(b);

        public override string ToString() => BoardText.ToChar(this).ToString();
    }
}

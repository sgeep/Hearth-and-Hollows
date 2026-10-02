namespace Hearthdelve.Core.Random
{
    /// <summary>Injectable randomness so rules (harvest rolls, loot) are deterministic in tests.</summary>
    public interface IRandom
    {
        /// <summary>Uniform float in [0, 1).</summary>
        float Value();

        /// <summary>Uniform int in [minInclusive, maxInclusive].</summary>
        int Range(int minInclusive, int maxInclusive);
    }

    public sealed class SeededRandom : IRandom
    {
        readonly System.Random m_Random;

        public SeededRandom(int seed) => m_Random = new System.Random(seed);
        public SeededRandom() => m_Random = new System.Random();

        public float Value() => (float)m_Random.NextDouble();
        public int Range(int minInclusive, int maxInclusive) => m_Random.Next(minInclusive, maxInclusive + 1);
    }
}

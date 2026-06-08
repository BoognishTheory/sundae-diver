namespace SundaeDiver
{
    /// <summary>
    /// Deterministic 32-bit RNG (mulberry32). Same seed -> same level, every time.
    /// Ported from the HTML prototype so generated levels stay reproducible
    /// (useful for testing, daily challenges, and shareable seeds).
    /// All arithmetic relies on uint wrapping (mod 2^32), which C# does by default
    /// in an unchecked context.
    /// </summary>
    public class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(uint seed) { _state = seed; }

        /// <summary>Returns a float in [0, 1).</summary>
        public float Next()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint t = _state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t = (t + ((t ^ (t >> 7)) * (t | 61u))) ^ t;
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }

        /// <summary>Float in [min, max).</summary>
        public float Range(float min, float max) => min + Next() * (max - min);

        /// <summary>Int in [min, max).</summary>
        public int RangeInt(int min, int max) => min + (int)(Next() * (max - min));
    }
}

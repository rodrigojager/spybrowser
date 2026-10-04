using System.Security.Cryptography;

namespace SpyBrowser.Cursory.Internal;

/// <summary>NumPy PCG64 XSL-RR 128/64 bit generator; no mutable state is shared across calls.</summary>
internal sealed class Pcg64
{
    private const uint InitA = 0x43b0d7e5;
    private const uint MultA = 0x931e8875;
    private const uint InitB = 0x8b51f9dd;
    private const uint MultB = 0x58f38ded;
    private const uint MixL = 0xca01f9dd;
    private const uint MixR = 0x4973f715;
    private const double DoubleScale = 1.0 / 9007199254740992.0;
    private static readonly UInt128 Multiplier = ((UInt128)2549297995355413924UL << 64) | 4865540595714422341UL;

    private UInt128 state;
    private UInt128 increment;
    private bool hasSpareUInt32;
    private uint spareUInt32;

    internal Pcg64(UInt128? seed)
    {
        var entropy = ExpandSeed(seed ?? CreateRandomSeed());
        uint[] pool = InitializePool(entropy);
        uint[] words = ExpandPool(pool);
        UInt128 initialState = ((UInt128)ReadWordPair(words, 0) << 64) | ReadWordPair(words, 1);
        UInt128 sequence = ((UInt128)ReadWordPair(words, 2) << 64) | ReadWordPair(words, 3);

        increment = (sequence << 1) | 1;
        Step();
        state += initialState;
        Step();
    }

    internal ulong NextUInt64()
    {
        Step();
        ulong value = (ulong)(state >> 64) ^ (ulong)state;
        int rotation = (int)(state >> 122);
        return (value >> rotation) | (value << ((-rotation) & 63));
    }

    internal uint NextUInt32()
    {
        if (hasSpareUInt32)
        {
            hasSpareUInt32 = false;
            return spareUInt32;
        }

        ulong value = NextUInt64();
        hasSpareUInt32 = true;
        spareUInt32 = (uint)(value >> 32);
        return (uint)value;
    }

    internal double NextDouble() => (NextUInt64() >> 11) * DoubleScale;

    internal int Integers(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
        uint maximum = (uint)(exclusiveUpperBound - 1);
        if (maximum == 0) return 0;
        if (maximum == uint.MaxValue) return (int)NextUInt32();

        ulong range = (ulong)maximum + 1;
        ulong threshold = (uint.MaxValue - maximum) % range;
        ulong product = (ulong)NextUInt32() * range;
        ulong low = (uint)product;
        while (low < threshold)
        {
            product = (ulong)NextUInt32() * range;
            low = (uint)product;
        }
        return (int)(product >> 32);
    }

    private void Step() => state = unchecked(state * Multiplier + increment);

    private static UInt128 CreateRandomSeed()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return new UInt128(BitConverter.ToUInt64(bytes[8..]), BitConverter.ToUInt64(bytes[..8]));
    }

    private static List<uint> ExpandSeed(UInt128 seed)
    {
        var words = new List<uint>();
        do
        {
            words.Add((uint)seed);
            seed >>= 32;
        } while (seed != 0);
        return words;
    }

    private static uint[] InitializePool(List<uint> entropy)
    {
        var pool = new uint[4];
        uint counter = InitA;
        uint Hash(uint value)
        {
            uint mixed = value ^ counter;
            counter = unchecked(counter * MultA);
            mixed = unchecked(mixed * counter);
            return mixed ^ (mixed >> 16);
        }
        uint Mix(uint left, uint right)
        {
            uint mixed = unchecked(MixL * left - MixR * right);
            return mixed ^ (mixed >> 16);
        }

        for (int i = 0; i < pool.Length; i++) pool[i] = Hash(i < entropy.Count ? entropy[i] : 0);
        for (int source = 0; source < pool.Length; source++)
            for (int destination = 0; destination < pool.Length; destination++)
                if (source != destination) pool[destination] = Mix(pool[destination], Hash(pool[source]));
        for (int source = pool.Length; source < entropy.Count; source++)
            for (int destination = 0; destination < pool.Length; destination++)
                pool[destination] = Mix(pool[destination], Hash(entropy[source]));
        return pool;
    }

    private static uint[] ExpandPool(uint[] pool)
    {
        var words = new uint[8];
        uint counter = InitB;
        for (int i = 0; i < words.Length; i++)
        {
            uint value = pool[i % pool.Length] ^ counter;
            counter = unchecked(counter * MultB);
            value = unchecked(value * counter);
            words[i] = value ^ (value >> 16);
        }
        return words;
    }

    private static ulong ReadWordPair(uint[] words, int pairIndex) =>
        words[pairIndex * 2] | ((ulong)words[pairIndex * 2 + 1] << 32);
}

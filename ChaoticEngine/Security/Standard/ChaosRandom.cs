// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Security.Cryptography;
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;

namespace ChaoticEngine.Security.Standard;

/// <summary>
/// A cryptographically strong, high-performance replacement for <see cref="System.Random"/> powered by the Chaotic Engine.
/// <br/>Uses a buffered keystream approach to generate random numbers efficiently.
/// </summary>
/// <typeparam name="TPrimitive">The chaotic algorithm to use (e.g., IntegerTentMap, IntegerLorenz).</typeparam>
public class ChaosRandom<TPrimitive> : Random
    where TPrimitive : struct, IChaoticPrimitive
{
    private readonly byte[] _buffer;
    private int _bufferIdx;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance with a default buffer size (4KB).
    /// <br/>The seed is automatically generated using a CSPRNG.
    /// </summary>
    public ChaosRandom() : this(4096) { }

    /// <summary>
    /// Initializes a new instance with a specified buffer size.
    /// </summary>
    /// <param name="bufferSize">Size of the internal generation buffer in bytes.</param>
    public ChaosRandom(int bufferSize)
    {
        _buffer = new byte[bufferSize];
        _bufferIdx = bufferSize; // Force refill on first use

        // Auto-generate secure seed
        _key = new byte[32];
        _iv = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_key);
        rng.GetBytes(_iv);
    }

    /// <summary>
    /// Refills the internal buffer with fresh chaotic entropy.
    /// Increments the IV to ensure non-repeating blocks (Counter Mode).
    /// </summary>
    private void RefillBuffer()
    {
        // Increment IV (Counter Mode) to generate a new unique block
        unsafe
        {
            fixed (byte* p = _iv)
            {
                // Treat the first 8 bytes of IV as a counter
                (*(ulong*)p)++;
            }
        }

        // Generate Keystream (Encrypt zero-buffer)
        Array.Clear(_buffer, 0, _buffer.Length);
        ChaosCipher<TPrimitive>.Process(_buffer, _key, _iv);
        _bufferIdx = 0;
    }

    /// <summary>
    /// Returns a non-negative random integer.
    /// </summary>
    public override int Next()
    {
        return Next(int.MaxValue);
    }

    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    public override int Next(int maxValue)
    {
        if (maxValue <= 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
        return Next(0, maxValue);
    }

    /// <summary>
    /// Returns a random integer that is within a specified range.
    /// </summary>
    public override int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        long range = (long)maxValue - minValue;
        if (range == 0) return minValue;

        // Check buffer availability (Need 4 bytes for UInt32)
        if (_bufferIdx + 4 > _buffer.Length) RefillBuffer();

        uint randomUInt = BitConverter.ToUInt32(_buffer, _bufferIdx);
        _bufferIdx += 4;

        // Map to range (Simple modulo mapping)
        return minValue + (int)(randomUInt % range);
    }

    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    public override double NextDouble()
    {
        // Need 8 bytes for UInt64
        if (_bufferIdx + 8 > _buffer.Length) RefillBuffer();

        ulong randomULong = BitConverter.ToUInt64(_buffer, _bufferIdx);
        _bufferIdx += 8;

        // Normalize to [0.0, 1.0) using 53-bit precision
        return (randomULong >> 11) * (1.0 / (1UL << 53));
    }

    /// <summary>
    /// Fills the elements of a specified array of bytes with random numbers.
    /// </summary>
    public override void NextBytes(byte[] buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));

        int offset = 0;
        int count = buffer.Length;

        while (count > 0)
        {
            if (_bufferIdx >= _buffer.Length) RefillBuffer();

            int available = _buffer.Length - _bufferIdx;
            int toCopy = Math.Min(available, count);

            Array.Copy(_buffer, _bufferIdx, buffer, offset, toCopy);

            _bufferIdx += toCopy;
            offset += toCopy;
            count -= toCopy;
        }
    }
}
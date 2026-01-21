// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Security.Cryptography;
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;

namespace ChaoticEngine.Security.Standard;

/// <summary>
/// A high-complexity random number generator using 3D Chaotic Maps (Lorenz/Chen).
/// </summary>
public class ChaosRandom3D<TPrimitive> : Random where TPrimitive : struct, IChaoticPrimitive3D
{
    private readonly byte[] _buffer;
    private int _bufferIdx;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public ChaosRandom3D(int bufferSize = 4096)
    {
        _buffer = new byte[bufferSize];
        _bufferIdx = bufferSize;
        _key = new byte[32]; _iv = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_key); rng.GetBytes(_iv);
    }

    private void RefillBuffer()
    {
        unsafe { fixed (byte* p = _iv) { (*(ulong*)p)++; } }
        Array.Clear(_buffer, 0, _buffer.Length);
        // CRITICAL FIX: Calls 3D Cipher Engine
        ChaosCipher3D<TPrimitive>.Process(_buffer, _key, _iv);
        _bufferIdx = 0;
    }

    public override int Next() => Next(int.MaxValue);
    public override int Next(int maxValue) => Next(0, maxValue);
    public override int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
        long range = (long)maxValue - minValue;
        if (range == 0) return minValue;
        if (_bufferIdx + 4 > _buffer.Length) RefillBuffer();
        uint rnd = BitConverter.ToUInt32(_buffer, _bufferIdx);
        _bufferIdx += 4;
        return minValue + (int)(rnd % range);
    }

    public override double NextDouble()
    {
        if (_bufferIdx + 8 > _buffer.Length) RefillBuffer();
        ulong rnd = BitConverter.ToUInt64(_buffer, _bufferIdx);
        _bufferIdx += 8;
        return (rnd >> 11) * (1.0 / (1UL << 53));
    }

    public override void NextBytes(byte[] buffer)
    {
        int offset = 0, count = buffer.Length;
        while (count > 0)
        {
            if (_bufferIdx >= _buffer.Length) RefillBuffer();
            int toCopy = Math.Min(_buffer.Length - _bufferIdx, count);
            Array.Copy(_buffer, _bufferIdx, buffer, offset, toCopy);
            _bufferIdx += toCopy; offset += toCopy; count -= toCopy;
        }
    }
}
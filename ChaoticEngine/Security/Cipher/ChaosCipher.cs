// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Buffers.Binary;
using ChaoticEngine.Security.Hash;
using ChaoticEngine.Security.Primitives;

namespace ChaoticEngine.Security.Cipher;

/// <summary>
/// Provides a high-performance, SIMD-accelerated generic stream cipher based on 1D Chaotic Maps.
/// <br/>Supports AVX-512 and AVX2 hardware acceleration with automatic scalar fallback.
/// </summary>
/// <typeparam name="TPrimitive">The integer chaotic map implementation (e.g., IntegerTentMap).</typeparam>
public static class ChaosCipher<TPrimitive> where TPrimitive : struct, IChaoticPrimitive
{
    private const uint SCRAMBLER1 = 0x85EBCA6B;
    private const uint SCRAMBLER2 = 0xC2B2AE35;

    /// <summary>
    /// Encrypts or Decrypts the given data buffer in-place using the provided key and IV.
    /// </summary>
    /// <param name="data">The data buffer to encrypt/decrypt (in-place).</param>
    /// <param name="key">The 256-bit (32 byte) secret key.</param>
    /// <param name="iv">The 128-bit (16 byte) initialization vector.</param>
    public static void Process(Span<byte> data, byte[] key, ReadOnlySpan<byte> iv)
    {
        if (Avx512F.IsSupported)
        {
            Span<uint> seeds = stackalloc uint[16];
            DeriveSeeds(key, iv, seeds);
            ProcessAvx512(data, seeds);
        }
        else if (Avx2.IsSupported)
        {
            Span<uint> seeds = stackalloc uint[8];
            DeriveSeeds(key, iv, seeds);
            ProcessAvx2(data, seeds);
        }
        else
        {
            Span<uint> seeds = stackalloc uint[1];
            DeriveSeeds(key, iv, seeds);
            ProcessScalar(data, seeds);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx512(Span<byte> data, Span<uint> seeds)
    {
        Vector512<uint> vState = Vector512.Create(seeds);
        int vectorSize = 64;
        int i = 0;

        fixed (byte* ptr = data)
        {
            while (i <= data.Length - vectorSize)
            {
                vState = TPrimitive.NextStateAvx512(vState);

                Vector512<byte> vData = Vector512.Load(ptr + i);

                Vector512<uint> vStateMixed = Vector512.Multiply(vState, Vector512.Create(SCRAMBLER1));
                vStateMixed = Vector512.Xor(vStateMixed, Vector512.ShiftRightLogical(vStateMixed, 16));

                vStateMixed = Vector512.Multiply(vStateMixed, Vector512.Create(SCRAMBLER2));
                vStateMixed = Vector512.Xor(vStateMixed, Vector512.ShiftRightLogical(vStateMixed, 13));

                Vector512<byte> vKey = vStateMixed.AsByte();
                Vector512<byte> vEncrypted = Vector512.Xor(vData, vKey);
                vEncrypted.Store(ptr + i);

                i += vectorSize;
            }
        }

        if (i < data.Length)
        {
            ProcessScalar(data.Slice(i), stackalloc uint[] { vState[0] });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx2(Span<byte> data, Span<uint> seeds)
    {
        Vector256<uint> vState = Vector256.Create(seeds);
        int vectorSize = 32;
        int i = 0;

        fixed (byte* ptr = data)
        {
            while (i <= data.Length - vectorSize)
            {
                vState = TPrimitive.NextStateAvx2(vState);

                Vector256<byte> vData = Avx.LoadVector256(ptr + i);
                var vStateInt = vState.AsInt32();

                var vMixed = Avx2.MultiplyLow(vStateInt, Vector256.Create(unchecked((int)SCRAMBLER1))).AsUInt32();
                vMixed = Avx2.Xor(vMixed, Avx2.ShiftRightLogical(vMixed, 16));

                vMixed = Avx2.MultiplyLow(vMixed.AsInt32(), Vector256.Create(unchecked((int)SCRAMBLER2))).AsUInt32();
                vMixed = Avx2.Xor(vMixed, Avx2.ShiftRightLogical(vMixed, 13));

                Vector256<byte> vKey = vMixed.AsByte();
                Vector256<byte> vEncrypted = Avx2.Xor(vData, vKey);
                Avx.Store(ptr + i, vEncrypted);

                i += vectorSize;
            }
        }

        if (i < data.Length)
        {
            ProcessScalar(data.Slice(i), seeds);
        }
    }

    private static void ProcessScalar(Span<byte> data, Span<uint> seeds)
    {
        uint x = seeds[0];
        for (int i = 0; i < data.Length; i++)
        {
            x = TPrimitive.NextState(x);
            uint hash = MurmurHash3.Mix(x);
            data[i] ^= (byte)hash;
        }
        seeds[0] = x;
    }

    private static void DeriveSeeds(byte[] key, ReadOnlySpan<byte> iv, Span<uint> seeds)
    {
        for (int i = 0; i < seeds.Length; i++)
        {
            seeds[i] = BitConverter.ToUInt32(key, (i * 4) % key.Length);

            if (iv.Length >= 4)
            {
                int ivOffset = (i * 4) % iv.Length;
                // Safe read using BinaryPrimitives (Works on ARM/x64)
                if (ivOffset + 4 <= iv.Length)
                    seeds[i] ^= BinaryPrimitives.ReadUInt32LittleEndian(iv.Slice(ivOffset, 4));
            }

            if (seeds[i] == 0) seeds[i] = 0xDEADBEEF;
        }

        // Warm-up rounds
        for (int round = 0; round < 16; round++)
        {
            for (int i = 0; i < seeds.Length; i++)
            {
                seeds[i] = TPrimitive.NextState(seeds[i]);
                uint neighbor = seeds[(i + 1) % seeds.Length];
                seeds[i] ^= (neighbor >> 1);
            }
        }
    }
}
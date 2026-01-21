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
/// Specialized Engine for 2D Chaotic Systems (e.g., Henon Map).
/// Holds 2 parallel states (X, Y) per stream for increased complexity.
/// </summary>
public static class ChaosCipher2D<TPrimitive> where TPrimitive : struct, IChaoticPrimitive2D
{
    private const uint SCRAMBLER1 = 0x85EBCA6B;

    public static void Process(Span<byte> data, byte[] key, ReadOnlySpan<byte> iv)
    {
        if (Avx2.IsSupported)
        {
            Span<uint> seedsX = stackalloc uint[8];
            Span<uint> seedsY = stackalloc uint[8];
            DeriveSeeds(key, iv, seedsX, seedsY);
            ProcessAvx2(data, seedsX, seedsY);
        }
        else
        {
            uint x = BitConverter.ToUInt32(key, 0);
            uint y = BitConverter.ToUInt32(key, 4);
            ProcessScalar(data, x, y);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx2(Span<byte> data, Span<uint> sx, Span<uint> sy)
    {
        var vX = Vector256.Create(sx);
        var vY = Vector256.Create(sy);
        int vectorSize = 32;
        int i = 0;

        fixed (byte* ptr = data)
        {
            while (i <= data.Length - vectorSize)
            {
                (vX, vY) = TPrimitive.NextStateAvx2(vX, vY);

                var vMixed = Avx2.Xor(vX, vY);
                vMixed = Avx2.MultiplyLow(vMixed, Vector256.Create(SCRAMBLER1));
                vMixed = Avx2.Xor(vMixed, Avx2.ShiftRightLogical(vMixed, 16));

                Vector256<byte> vData = Avx.LoadVector256(ptr + i);
                Vector256<byte> vKey = vMixed.AsByte();
                Avx.Store(ptr + i, Avx2.Xor(vData, vKey));

                i += vectorSize;
            }
        }
    }

    private static void ProcessScalar(Span<byte> data, uint x, uint y)
    {
        for (int i = 0; i < data.Length; i++)
        {
            (x, y) = TPrimitive.NextState(x, y);

            uint k = x ^ y;
            k *= SCRAMBLER1;
            k ^= k >> 16;

            data[i] ^= (byte)k;
        }
    }

    private static void DeriveSeeds(byte[] key, ReadOnlySpan<byte> iv, Span<uint> sx, Span<uint> sy)
    {
        for (int i = 0; i < 8; i++)
        {
            sx[i] = BitConverter.ToUInt32(key, i % 4) ^ (uint)i;
            sy[i] = (iv.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(iv) : 0) + (uint)i;
        }
    }
}
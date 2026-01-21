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
/// Specialized Engine for 3D Chaotic Systems (e.g., Lorenz, Chen).
/// Holds 3 parallel states (X, Y, Z) per stream.
/// </summary>
public static class ChaosCipher3D<TPrimitive> where TPrimitive : struct, IChaoticPrimitive3D
{
    private const uint SCRAMBLER1 = 0x85EBCA6B;

    public static void Process(Span<byte> data, byte[] key, ReadOnlySpan<byte> iv)
    {
        if (Avx2.IsSupported)
        {
            Span<uint> seedsX = stackalloc uint[8];
            Span<uint> seedsY = stackalloc uint[8];
            Span<uint> seedsZ = stackalloc uint[8];
            DeriveSeeds(key, iv, seedsX, seedsY, seedsZ);
            ProcessAvx2(data, seedsX, seedsY, seedsZ);
        }
        else
        {
            uint x = BitConverter.ToUInt32(key, 0);
            uint y = BitConverter.ToUInt32(key, 4);
            uint z = (iv.Length >= 4) ? BinaryPrimitives.ReadUInt32LittleEndian(iv) : 0xDEADBEEF;
            ProcessScalar(data, x, y, z);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx2(Span<byte> data, Span<uint> sx, Span<uint> sy, Span<uint> sz)
    {
        var vX = Vector256.Create(sx);
        var vY = Vector256.Create(sy);
        var vZ = Vector256.Create(sz);
        int vectorSize = 32;
        int i = 0;

        fixed (byte* ptr = data)
        {
            while (i <= data.Length - vectorSize)
            {
                (vX, vY, vZ) = TPrimitive.NextStateAvx2(vX, vY, vZ);

                var vMixed = Avx2.Xor(vX, vY);
                vMixed = Avx2.Xor(vMixed, vZ);

                vMixed = Avx2.MultiplyLow(vMixed, Vector256.Create(SCRAMBLER1));
                vMixed = Avx2.Xor(vMixed, Avx2.ShiftRightLogical(vMixed, 16));

                Vector256<byte> vData = Avx.LoadVector256(ptr + i);
                Vector256<byte> vKey = vMixed.AsByte();
                Avx.Store(ptr + i, Avx2.Xor(vData, vKey));

                i += vectorSize;
            }
        }
    }

    private static void ProcessScalar(Span<byte> data, uint x, uint y, uint z)
    {
        for (int i = 0; i < data.Length; i++)
        {
            (x, y, z) = TPrimitive.NextState(x, y, z);

            uint k = x ^ y ^ z;
            k *= SCRAMBLER1;
            k ^= k >> 16;

            data[i] ^= (byte)k;
        }
    }

    private static void DeriveSeeds(byte[] key, ReadOnlySpan<byte> iv, Span<uint> sx, Span<uint> sy, Span<uint> sz)
    {
        for (int i = 0; i < 8; i++)
        {
            sx[i] = BitConverter.ToUInt32(key, i % 4) ^ (uint)i;
            sy[i] = BitConverter.ToUInt32(key, (i + 4) % key.Length) ^ 0xAAAA;
            sz[i] = (iv.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(iv) : 0) + (uint)i;
        }
    }
}
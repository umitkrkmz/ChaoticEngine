// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer implementation of the Tent Map.
/// <br/>This is the fastest algorithm in the suite, utilizing pure bitwise operations.
/// </summary>
public struct IntegerTentMap : IChaoticPrimitive
{
    private const uint THRESHOLD = 0x80000000;
    public static uint WeylConstant => 0x9E3779B9;

    /// <summary>
    /// Computes the next state using bitwise folding logic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint NextState(uint x)
    {
        // Formula: x < 0.5 ? 2x : 2(1-x)
        if (x < THRESHOLD)
            x = (x << 1) | (x >> 31);
        else
            x = ((~x) << 1) | ((~x) >> 31);

        return x + WeylConstant;
    }

    /// <summary>
    /// Computes the next state for 8 parallel streams using AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> NextStateAvx2(Vector256<uint> vState)
    {
        // Create mask for conditional logic
        var mask = Avx2.ShiftRightArithmetic(vState.AsInt32(), 31).AsUInt32();

        // Conditional Invert: if (x >= 0.5) x = ~x
        var processed = Avx2.Xor(vState, mask);

        // Scaling (x * 2) using bitwise rotation
        var rotLeft = Avx2.ShiftLeftLogical(processed, 1);
        var rotRight = Avx2.ShiftRightLogical(processed, 31);
        var result = Avx2.Or(rotLeft, rotRight);

        return Avx2.Add(result, Vector256.Create(WeylConstant));
    }

    /// <summary>
    /// Computes the next state for 16 parallel streams using AVX-512.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<uint> NextStateAvx512(Vector512<uint> vState)
    {
        var mask = Vector512.ShiftRightArithmetic(vState.AsInt32(), 31).AsUInt32();
        var processed = Vector512.Xor(vState, mask);

        var rotLeft = Vector512.ShiftLeft(processed, 1);
        var rotRight = Vector512.ShiftRightLogical(processed, 31);
        var result = Vector512.BitwiseOr(rotLeft, rotRight);

        return Vector512.Add(result, Vector512.Create(WeylConstant));
    }
}
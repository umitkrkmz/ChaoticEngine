// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer-based implementation of the Henon Map optimized for 2D stream encryption.
/// <br/>Adaptation: x_new = y + 1 - 1.4*x^2 (Mapped to modular arithmetic).
/// </summary>
public struct IntegerHenonMap : IChaoticPrimitive2D
{
    // Weyl Sequence constant to prevent zero-state collapse
    private const uint WEYL = 0x6D2B79F5;

    /// <summary>
    /// Computes the next 2D state using scalar integer arithmetic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint x, uint y) NextState(uint x, uint y)
    {
        // Classic Henon: x_new = 1 - a*x^2 + y
        // Integer Variation: x_new = y + Weyl - (x*x high bits)

        // x*x operation overflows in 32-bit. 
        // We mix high bits with low bits to preserve entropy.
        ulong xSq = (ulong)x * x;
        uint termX = (uint)(xSq ^ (xSq >> 32));

        uint x_new = y + WEYL - termX;

        // y_new = b * x 
        // For encryption, we use a bijective mapping (y_new = x) to preserve information density.
        uint y_new = x;

        return (x_new, y_new);
    }

    /// <summary>
    /// Computes the next state for 8 parallel streams using AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector256<uint> vx, Vector256<uint> vy)
        NextStateAvx2(Vector256<uint> vx, Vector256<uint> vy)
    {
        // 1. Calculate x^2 term
        // AVX2 32x32->64 multiply is expensive. We use MultiplyLow + Rotate for speed.
        var xSq = Avx2.MultiplyLow(vx, vx);
        var termX = Avx2.Xor(xSq, Avx2.ShiftRightLogical(xSq, 13)); // Mixing

        // 2. x_new = y + Weyl - termX
        var vWeyl = Vector256.Create(WEYL);
        var yPlusWeyl = Avx2.Add(vy, vWeyl);
        var new_vx = Avx2.Subtract(yPlusWeyl, termX);

        // 3. y_new = x
        var new_vy = vx;

        return (new_vx, new_vy);
    }

    /// <summary>
    /// Computes the next state for 16 parallel streams using AVX-512.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector512<uint> vx, Vector512<uint> vy)
        NextStateAvx512(Vector512<uint> vx, Vector512<uint> vy)
    {
        // 1. Calculate x^2 term
        var xSq = Vector512.Multiply(vx, vx);
        var termX = Vector512.Xor(xSq, Vector512.ShiftRightLogical(xSq, 13));

        // 2. x_new = y + Weyl - termX
        var vWeyl = Vector512.Create(WEYL);
        var yPlusWeyl = Vector512.Add(vy, vWeyl);
        var new_vx = Vector512.Subtract(yPlusWeyl, termX);

        // 3. y_new = x
        var new_vy = vx;

        return (new_vx, new_vy);
    }
}
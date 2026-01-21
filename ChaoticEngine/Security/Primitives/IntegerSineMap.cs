// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer implementation of the Sine Map using Bhaskara I approximation.
/// <br/>Strategy: Converts Integer -> Float -> Fast Approximation -> Integer to utilize AVX FMA units.
/// </summary>
public struct IntegerSineMap : IChaoticPrimitive
{
    // Weyl Sequence constant
    public static uint WeylConstant => 0xB504F333;

    /// <summary>
    /// Computes the next state using a scalar approximation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint NextState(uint x)
    {
        // Bhaskara I Formula Approximation: sin(pi*x) ~ 16x(1-x) / (5 - 4x(1-x))

        // 1. Normalize to [0, 1]
        double val = x / (double)uint.MaxValue;

        // 2. Apply Formula (Assuming r=4.0)
        double numerator = 16.0 * val * (1.0 - val);
        double denominator = 5.0 - 4.0 * val * (1.0 - val);
        double result = 4.0 * (numerator / denominator);

        // 3. Convert back to Uint
        return (uint)(result * uint.MaxValue) + WeylConstant;
    }

    /// <summary>
    /// Computes the next state for 8 parallel streams using AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> NextStateAvx2(Vector256<uint> vState)
    {
        // 1. Convert Uint -> Float
        var vStateInt = Avx2.ShiftRightLogical(vState, 1).AsInt32(); // Avoid negative int interpretation
        var vFloat = Avx.ConvertToVector256Single(vStateInt);

        // Normalize
        var vMax = Vector256.Create((float)int.MaxValue);
        var vNorm = Avx.Divide(vFloat, vMax);

        // Constants
        var vOne = Vector256.Create(1.0f);
        var v16 = Vector256.Create(16.0f);
        var v5 = Vector256.Create(5.0f);
        var v4 = Vector256.Create(4.0f);

        // Term = x * (1 - x)
        var vOneMinusX = Avx.Subtract(vOne, vNorm);
        var vTerm = Avx.Multiply(vNorm, vOneMinusX);

        // Formula Application
        var vNum = Avx.Multiply(v16, vTerm);
        var vDenomTerm = Avx.Multiply(v4, vTerm);
        var vDenom = Avx.Subtract(v5, vDenomTerm);

        var vDiv = Avx.Divide(vNum, vDenom);
        var vResFloat = Avx.Multiply(v4, vDiv);

        // 3. Convert Float -> Uint
        var vResInt = Avx.ConvertToVector256Int32(Avx.Multiply(vResFloat, vMax));
        var vResult = Avx2.ShiftLeftLogical(vResInt.AsUInt32(), 1);

        return Avx2.Add(vResult, Vector256.Create(WeylConstant));
    }

    /// <summary>
    /// Computes the next state for 16 parallel streams using AVX-512.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<uint> NextStateAvx512(Vector512<uint> vState)
    {
        // AVX-512 supports direct Uint -> Float conversion
        var vFloat = Vector512.ConvertToSingle(vState);
        var vMax = Vector512.Create((float)uint.MaxValue);
        var vNorm = Vector512.Divide(vFloat, vMax);

        var vOne = Vector512.Create(1.0f);
        var v16 = Vector512.Create(16.0f);
        var v5 = Vector512.Create(5.0f);
        var v4 = Vector512.Create(4.0f);

        var vTerm = Vector512.Multiply(vNorm, Vector512.Subtract(vOne, vNorm));
        var vNum = Vector512.Multiply(v16, vTerm);
        var vDenom = Vector512.Subtract(v5, Vector512.Multiply(v4, vTerm));

        var vResFloat = Vector512.Multiply(v4, Vector512.Divide(vNum, vDenom));

        var vResult = Vector512.ConvertToUInt32(Vector512.Multiply(vResFloat, vMax));
        return Vector512.Add(vResult, Vector512.Create(WeylConstant));
    }
}
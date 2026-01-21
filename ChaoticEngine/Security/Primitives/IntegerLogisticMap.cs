// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer implementation of the Logistic Map (r=4.0 case).
/// <br/>Formula: x_next = 4 * x * (1 - x) mapped to integer space.
/// </summary>
public struct IntegerLogisticMap : IChaoticPrimitive
{
    // Golden Ratio * PI derived constant
    public static uint WeylConstant => 0x61C88647;

    /// <summary>
    /// Computes the next state using scalar integer arithmetic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint NextState(uint x)
    {
        // 64-bit multiplication: x * (1-x) simulation
        // We use bitwise NOT (~x) to approximate (1-x) in integer space.
        ulong product = (ulong)x * (ulong)(~x);

        // Mapping r=4 (shift left 2) and normalizing to 32-bit (shift right 32).
        // Combined operation: product >> 30
        uint result = (uint)(product >> 30);

        return result + WeylConstant;
    }

    /// <summary>
    /// Computes the next state for 8 parallel streams using AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<uint> NextStateAvx2(Vector256<uint> vState)
    {
        // AVX2 Pseudo-Logistic Variation for Speed
        // Full 32x32->64 mul is expensive in AVX2.
        // We use MultiplyLow (Modular Mul) + Rotate to emulate chaotic mixing.

        var vOneMinusX = Avx2.Xor(vState, Vector256<uint>.AllBitsSet); // ~x
        var vProduct = Avx2.MultiplyLow(vState, vOneMinusX);

        // Mixing step to propagate entropy
        var result = Avx2.Xor(vProduct, Avx2.ShiftRightLogical(vProduct, 16));

        return Avx2.Add(result, Vector256.Create(WeylConstant));
    }

    /// <summary>
    /// Computes the next state for 16 parallel streams using AVX-512.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector512<uint> NextStateAvx512(Vector512<uint> vState)
    {
        // AVX-512 Variation
        var vOneMinusX = Vector512.Xor(vState, Vector512<uint>.AllBitsSet);
        var vProduct = Vector512.Multiply(vState, vOneMinusX);
        var result = Vector512.Xor(vProduct, Vector512.ShiftRightLogical(vProduct, 16));

        return Vector512.Add(result, Vector512.Create(WeylConstant));
    }
}
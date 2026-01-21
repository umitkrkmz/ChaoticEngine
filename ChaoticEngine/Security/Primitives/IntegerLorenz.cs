// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer-based implementation of the Lorenz Attractor (3D).
/// <br/>Optimized for speed using bitwise approximations of the differential equations.
/// </summary>
public struct IntegerLorenz : IChaoticPrimitive3D
{
    // Simulation of Time Step (dt) approx 1/32
    private const int SHIFT = 5;

    /// <summary>
    /// Computes the next 3D state using scalar integer arithmetic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint x, uint y, uint z) NextState(uint x, uint y, uint z)
    {
        // DX = 10 * (y - x) * dt
        // Approx: (y - x) >> 2
        uint dx = (y - x) >> 2;

        // DY = (28*x - y - x*z) * dt
        // Approx: (x ^ (y >> 3)) - z
        uint dy = (x ^ (y >> 3)) - z;

        // DZ = (xy - 8/3*z) * dt
        // Approx: (x + y) ^ (z << 1)
        uint dz = (x + y) ^ (z << 1);

        // State Update with Modular Arithmetic
        uint x_new = x + dx;
        uint y_new = y + dy;
        uint z_new = z + dz;

        return (x_new, y_new, z_new);
    }

    /// <summary>
    /// Computes the next state for 8 parallel streams using AVX2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz)
        NextStateAvx2(Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz)
    {
        // 1. DX Calculation
        var diff = Avx2.Subtract(vy, vx);
        var dx = Avx2.ShiftRightLogical(diff, 2);
        var new_vx = Avx2.Add(vx, dx);

        // 2. DY Calculation
        var yShift = Avx2.ShiftRightLogical(vy, 3);
        var xXorY = Avx2.Xor(vx, yShift);
        var dy = Avx2.Subtract(xXorY, vz);
        var new_vy = Avx2.Add(vy, dy);

        // 3. DZ Calculation
        var xPlusY = Avx2.Add(vx, vy);
        var zShift = Avx2.ShiftLeftLogical(vz, 1);
        var dz = Avx2.Xor(xPlusY, zShift);
        var new_vz = Avx2.Add(vz, dz);

        return (new_vx, new_vy, new_vz);
    }

    /// <summary>
    /// Computes the next state for 16 parallel streams using AVX-512.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz)
        NextStateAvx512(Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz)
    {
        // DX
        var diff = Vector512.Subtract(vy, vx);
        var dx = Vector512.ShiftRightLogical(diff, 2);
        var new_vx = Vector512.Add(vx, dx);

        // DY
        var yShift = Vector512.ShiftRightLogical(vy, 3);
        var xXorY = Vector512.Xor(vx, yShift);
        var dy = Vector512.Subtract(xXorY, vz);
        var new_vy = Vector512.Add(vy, dy);

        // DZ
        var xPlusY = Vector512.Add(vx, vy);
        var zShift = Vector512.ShiftLeft(vz, 1); // Fixed method name
        var dz = Vector512.Xor(xPlusY, zShift);
        var new_vz = Vector512.Add(vz, dz);

        return (new_vx, new_vy, new_vz);
    }
}
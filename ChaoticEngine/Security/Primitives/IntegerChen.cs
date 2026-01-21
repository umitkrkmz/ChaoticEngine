// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Integer implementation of the Chen System.
/// A more complex chaotic attractor than Lorenz, with stronger topological mixing.
/// </summary>
public struct IntegerChen : IChaoticPrimitive3D
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint x, uint y, uint z) NextState(uint x, uint y, uint z)
    {
        uint dx = (y - x) + ((y - x) << 1);
        uint dy = (x ^ (y << 2)) + (z >> 1);
        uint dz = (x + y) ^ (z + (z << 1));

        return (x + dx, y + dy, z + dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz)
        NextStateAvx2(Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz)
    {
        // DX
        var diff = Avx2.Subtract(vy, vx);
        var diff2 = Avx2.ShiftLeftLogical(diff, 1);
        var dx = Avx2.Add(diff, diff2);
        var new_vx = Avx2.Add(vx, dx);

        // DY
        var yShift = Avx2.ShiftLeftLogical(vy, 2);
        var xXorY = Avx2.Xor(vx, yShift);
        var zShift = Avx2.ShiftRightLogical(vz, 1);
        var dy = Avx2.Add(xXorY, zShift);
        var new_vy = Avx2.Add(vy, dy);

        // DZ
        var xPlusY = Avx2.Add(vx, vy);
        var zTimes2 = Avx2.ShiftLeftLogical(vz, 1);
        var zTimes3 = Avx2.Add(vz, zTimes2);
        var dz = Avx2.Xor(xPlusY, zTimes3);
        var new_vz = Avx2.Add(vz, dz);

        return (new_vx, new_vy, new_vz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz)
        NextStateAvx512(Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz)
    {
        // DX
        var diff = Vector512.Subtract(vy, vx);
        var diff2 = Vector512.ShiftLeft(diff, 1);
        var dx = Vector512.Add(diff, diff2);
        var new_vx = Vector512.Add(vx, dx);

        // DY
        var yShift = Vector512.ShiftLeft(vy, 2);
        var xXorY = Vector512.Xor(vx, yShift);
        var zShift = Vector512.ShiftRightLogical(vz, 1);
        var dy = Vector512.Add(xXorY, zShift);
        var new_vy = Vector512.Add(vy, dy);

        // DZ
        var xPlusY = Vector512.Add(vx, vy);
        var zTimes2 = Vector512.ShiftLeft(vz, 1);
        var zTimes3 = Vector512.Add(vz, zTimes2);
        var dz = Vector512.Xor(xPlusY, zTimes3);
        var new_vz = Vector512.Add(vz, dz);

        return (new_vx, new_vy, new_vz);
    }
}
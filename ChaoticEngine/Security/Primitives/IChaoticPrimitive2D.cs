using System.Runtime.Intrinsics;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Contract for 2-Dimensional Integer Chaotic Maps (e.g., Henon).
/// Operates on 2 coupled states (X, Y).
/// </summary>
public interface IChaoticPrimitive2D
{
    // Scalar: 2 girdi al, 2 çıktı ver
    static abstract (uint x, uint y) NextState(uint x, uint y);

    // AVX2: 2 Vektör al, 2 Vektör ver
    static abstract (Vector256<uint> vx, Vector256<uint> vy)
        NextStateAvx2(Vector256<uint> vx, Vector256<uint> vy);

    // AVX-512: 2 Vektör al, 2 Vektör ver
    static abstract (Vector512<uint> vx, Vector512<uint> vy)
        NextStateAvx512(Vector512<uint> vx, Vector512<uint> vy);
}
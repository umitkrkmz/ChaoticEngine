using System.Runtime.Intrinsics;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Contract for 3-Dimensional Integer Chaotic Maps (e.g., Lorenz, Chen).
/// Operates on 3 coupled states (X, Y, Z).
/// </summary>
public interface IChaoticPrimitive3D
{
    // Scalar: 3 girdi al, 3 çıktı ver (Tuple kullanımı)
    static abstract (uint x, uint y, uint z) NextState(uint x, uint y, uint z);

    // AVX2: 3 Vektör al, 3 Vektör ver
    static abstract (Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz)
        NextStateAvx2(Vector256<uint> vx, Vector256<uint> vy, Vector256<uint> vz);

    // AVX-512: 3 Vektör al, 3 Vektör ver
    static abstract (Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz)
        NextStateAvx512(Vector512<uint> vx, Vector512<uint> vy, Vector512<uint> vz);
}
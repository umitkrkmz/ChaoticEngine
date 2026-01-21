using System.Runtime.Intrinsics;

namespace ChaoticEngine.Security.Primitives;

/// <summary>
/// Defines the contract for Integer-based Chaotic Maps used in the encryption engine.
/// Using 'static abstract' allows generic usage without runtime overhead (Zero-Cost Abstraction).
/// </summary>
public interface IChaoticPrimitive
{
    // Algoritmanın enerji sabiti (Weyl Sequence için)
    static abstract uint WeylConstant { get; }

    // Scalar işlem (Tek bir sayı için)
    static abstract uint NextState(uint x);

    // AVX2 işlem (8 sayı aynı anda)
    static abstract Vector256<uint> NextStateAvx2(Vector256<uint> vState);

    // AVX-512 işlem (16 sayı aynı anda)
    static abstract Vector512<uint> NextStateAvx512(Vector512<uint> vState);
}
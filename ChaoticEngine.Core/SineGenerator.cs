using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Core;

public class SineGenerator : IChaoticGenerator1D
{
    // Parameter R (Between 0 and 4.0; 4.0 represents full chaos)
    public double R { get; init; } = 4.0;

    // Small epsilon to diverge parallel streams so each lane follows a distinct path
    private const double Epsilon = 1e-10;

    public void Generate(Span<double> buffer, double x0)
    {
        int i = 0;

        // Stage 1: AVX-512 (8 Parallel Streams)
        if (Avx512F.IsSupported)
            i = GenerateAvx512(buffer, x0);
        // Stage 2: AVX2 (4 Parallel Streams)
        else if (Avx2.IsSupported)
            i = GenerateAvx2(buffer, x0);

        // Stage 3: Scalar Fallback
        // Resume from where SIMD left off, or start from initial conditions.
        double x = (i == 0) ? x0 : buffer[i - 1];

        for (; i < buffer.Length; i++)
        {
            // Original Formula: x = r * sin(pi * x)
            // We use actual Math.Sin here for maximum precision in scalar mode.
            x = R * Math.Sin(Math.PI * x);
            buffer[i] = x;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateAvx512(Span<double> buffer, double x0)
    {
        int count = Vector512<double>.Count; // 8
        if (buffer.Length < count) return 0;

        // Step 1: Multi-State Initialization
        Span<double> seeds = stackalloc double[count];
        for (int k = 0; k < count; k++)
        {
            seeds[k] = (x0 + k * Epsilon) % 1.0;
        }

        Vector512<double> vX = Vector512.Create(seeds);
        Vector512<double> vR = Vector512.Create(R);

        // Bhaskara I constants
        Vector512<double> v16 = Vector512.Create(16.0);
        Vector512<double> v5 = Vector512.Create(5.0);
        Vector512<double> vOne = Vector512.Create(1.0);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            // Bhaskara I Approximation: sin(pi*x) ≈ (16*x*(1-x)) / (5 - x*(1-x))
            // Used because standard sin() is expensive in SIMD.

            // Term = x * (1 - x)
            var vTerm = Avx512F.Multiply(vX, Avx512F.Subtract(vOne, vX));

            // Numerator = 16 * Term
            var vNum = Avx512F.Multiply(v16, vTerm);

            // Denominator = 5 - Term
            var vDenom = Avx512F.Subtract(v5, vTerm);

            // SinApprox = Num / Denom
            var vSin = Avx512F.Divide(vNum, vDenom);

            // Result = R * SinApprox
            vX = Avx512F.Multiply(vR, vSin);

            vX.CopyTo(buffer.Slice(i));
        }
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateAvx2(Span<double> buffer, double x0)
    {
        int count = Vector256<double>.Count; // 4
        if (buffer.Length < count) return 0;

        // Step 1: Multi-State Initialization
        Span<double> seeds = stackalloc double[count];
        for (int k = 0; k < count; k++)
        {
            seeds[k] = (x0 + k * Epsilon) % 1.0;
        }

        Vector256<double> vX = Vector256.Create(seeds);
        Vector256<double> vR = Vector256.Create(R);

        // Bhaskara I constants
        Vector256<double> v16 = Vector256.Create(16.0);
        Vector256<double> v5 = Vector256.Create(5.0);
        Vector256<double> vOne = Vector256.Create(1.0);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            // Term = x * (1 - x)
            var vTerm = Avx2.Multiply(vX, Avx2.Subtract(vOne, vX));

            // Numerator = 16 * Term
            var vNum = Avx2.Multiply(v16, vTerm);

            // Denominator = 5 - Term
            var vDenom = Avx2.Subtract(v5, vTerm);

            // SinApprox = Num / Denom
            var vSin = Avx2.Divide(vNum, vDenom);

            // Result = R * SinApprox
            vX = Avx2.Multiply(vR, vSin);

            vX.CopyTo(buffer.Slice(i));
        }
        return i;
    }
}
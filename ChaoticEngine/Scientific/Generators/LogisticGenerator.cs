using ChaoticEngine.Scientific.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Scientific.Generators;

/// <summary>
/// Implements the Logistic Map, a polynomial mapping (degree 2) often cited as an archetypal example 
/// of how complex, chaotic behavior can arise from very simple non-linear dynamical equations.
/// <br/>Originally used for demographic models (population growth).
/// </summary>
public class LogisticGenerator : IChaoticGenerator1D
{
    /// <summary>
    /// The growth rate 'r' (Canonical value: 3.99).
    /// <list type="bullet">
    /// <item>r &lt; 3: Population stabilizes.</item>
    /// <item>3 &lt; r &lt; 3.5699: Period doubling bifurcations.</item>
    /// <item>r &gt; 3.5699: <b>Onset of Chaos.</b></item>
    /// </list>
    /// </summary>
    public double GrowthRate { get; init; } = 3.99;

    // Small epsilon to diverge parallel streams
    private const double Epsilon = 1e-10;

    /// <summary>
    /// Generates a chaotic sequence based on the Logistic Map equation: x = r * x * (1 - x).
    /// </summary>
    public void Generate(Span<double> buffer, double x0)
    {
        int i = 0;

        // Stage 1: AVX-512 (8 Parallel Streams)
        if (Avx512F.IsSupported)
        {
            i = GenerateAvx512(buffer, x0);
        }
        // Stage 2: AVX2 (4 Parallel Streams)
        else if (Avx2.IsSupported)
        {
            i = GenerateAvx2(buffer, x0);
        }

        // Stage 3: Scalar Fallback
        double x = (i == 0) ? x0 : buffer[i - 1];
        for (; i < buffer.Length; i++)
        {
            x = GrowthRate * x * (1.0 - x);
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
            seeds[k] = x0 + (k * Epsilon);
            if (seeds[k] >= 1.0) seeds[k] -= 1.0;
        }

        Vector512<double> vX = Vector512.Create(seeds);
        Vector512<double> vR = Vector512.Create(GrowthRate);
        Vector512<double> vOne = Vector512.Create(1.0);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            // Formula: x = r * x * (1 - x)
            var vOneMinusX = Avx512F.Subtract(vOne, vX);
            vX = Avx512F.Multiply(vR, Avx512F.Multiply(vX, vOneMinusX));

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
            seeds[k] = x0 + (k * Epsilon);
            if (seeds[k] >= 1.0) seeds[k] -= 1.0;
        }

        Vector256<double> vX = Vector256.Create(seeds);
        Vector256<double> vR = Vector256.Create(GrowthRate);
        Vector256<double> vOne = Vector256.Create(1.0);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            var vOneMinusX = Avx2.Subtract(vOne, vX);
            vX = Avx2.Multiply(vR, Avx2.Multiply(vX, vOneMinusX));
            vX.CopyTo(buffer.Slice(i));
        }
        return i;
    }
}
using ChaoticEngine.Scientific.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Scientific.Generators;

/// <summary>
/// Implements the Tent Map, a piecewise linear, one-dimensional map.
/// <br/>Named for the tent-like shape of its graph. It allows for extremely fast generation due to simple arithmetic operations.
/// </summary>
public class TentGenerator : IChaoticGenerator1D
{
    /// <summary>
    /// The control parameter 'Mu' (Canonical value: 1.9999).
    /// <br/>Controls the height of the tent.
    /// <list type="bullet">
    /// <item>Values close to 2.0 exhibit full chaotic behavior filling the interval [0,1].</item>
    /// <item>At exactly 2.0, the map is topologically conjugate to the Logistic Map (r=4.0).</item>
    /// </list>
    /// </summary>
    public double Mu { get; init; } = 1.9999;

    // Small epsilon to diverge parallel streams
    private const double Epsilon = 1e-10;

    /// <summary>
    /// Generates a chaotic sequence using the Tent Map equation: x = (x &lt; 0.5) ? mu*x : mu*(1-x).
    /// </summary>
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
        double x = (i == 0) ? x0 : buffer[i - 1];

        for (; i < buffer.Length; i++)
        {
            // Original Formula: x < 0.5 ? mu * x : mu * (1 - x)
            // Ternary operator (?:) is generally the fastest approach for scalar execution.
            x = x < 0.5 ? Mu * x : Mu * (1.0 - x);
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
        Vector512<double> vMu = Vector512.Create(Mu);
        Vector512<double> vOne = Vector512.Create(1.0);
        Vector512<double> vHalf = Vector512.Create(0.5);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            // SIMD Branchless Logic:
            // Uses Masking and Blending instead of if-else to avoid branch mispredictions.

            // 1. Create Mask: Set bits to 1 where x < 0.5, else 0.
            var mask = Avx512F.CompareLessThan(vX, vHalf);

            // 2. Calculate both paths speculatively (Pipeline friendly)
            // Left Path (True case): mu * x
            var left = Avx512F.Multiply(vMu, vX);

            // Right Path (False case): mu * (1 - x)
            var right = Avx512F.Multiply(vMu, Avx512F.Subtract(vOne, vX));

            // 3. Blend: Select 'left' where mask is 1, 'right' where mask is 0.
            vX = Avx512F.BlendVariable(right, left, mask);

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
        Vector256<double> vMu = Vector256.Create(Mu);
        Vector256<double> vOne = Vector256.Create(1.0);
        Vector256<double> vHalf = Vector256.Create(0.5);

        int i = 0;
        for (; i <= buffer.Length - count; i += count)
        {
            // 1. Mask: x < 0.5
            var mask = Avx2.CompareLessThan(vX, vHalf);

            // 2. Calculate Both
            var left = Avx2.Multiply(vMu, vX);
            var right = Avx2.Multiply(vMu, Avx2.Subtract(vOne, vX));

            // 3. Blend
            vX = Avx2.BlendVariable(right, left, mask);

            vX.CopyTo(buffer.Slice(i));
        }
        return i;
    }
}
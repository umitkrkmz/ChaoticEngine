using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Analysis;

/// <summary>
/// Provides high-performance statistical analysis tools for double-precision chaotic sequences.
/// Includes hardware-accelerated (AVX-512/AVX2) implementations of common error metrics.
/// </summary>
public static class QualityMetrics
{
    // Tolerance value for floating-point comparisons
    private const double Epsilon = 1e-9;

    // Bit Masks for Absolute Value (Sign bit 0, others 1)
    private static readonly Vector256<long> AbsMask256 = Vector256.Create(0x7FFFFFFFFFFFFFFF);
    private static readonly Vector512<long> AbsMask512 = Vector512.Create(0x7FFFFFFFFFFFFFFF);

    #region Error Metrics (MSE, RMSE, MAE)

    /// <summary>
    /// Calculates the Mean Squared Error (MSE) between two data sets.
    /// Used to measure the average squared difference between estimated values and the actual value.
    /// </summary>
    /// <param name="expected">The reference data set (original signal).</param>
    /// <param name="actual">The observed data set (generated/encrypted signal).</param>
    /// <returns>The Mean Squared Error value.</returns>
    /// <exception cref="ArgumentException">Thrown when data sets have different lengths.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateMse(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual)
    {
        if (expected.Length != actual.Length)
            throw new ArgumentException("Data sets must have equal length.");

        double sumSquaredError = 0;
        int i = 0;

        // Stage 1: AVX-512 (Ultra-High Performance)
        if (Avx512F.IsSupported)
        {
            var vSum = Vector512<double>.Zero;
            int count = Vector512<double>.Count;
            // Unsafe pointer arithmetic logic hidden behind Span optimization
            for (; i <= expected.Length - count; i += count)
            {
                var vExp = Vector512.Create(expected.Slice(i));
                var vAct = Vector512.Create(actual.Slice(i));
                var diff = Avx512F.Subtract(vExp, vAct);
                // Fused Multiply-Add could be used here if FMA3 is supported, but Mul+Add is fine.
                vSum = Avx512F.Add(vSum, Avx512F.Multiply(diff, diff));
            }
            sumSquaredError = Vector512.Sum(vSum);
        }
        // Stage 2: AVX2 (High Performance)
        else if (Avx2.IsSupported)
        {
            var vSum = Vector256<double>.Zero;
            int count = Vector256<double>.Count;
            for (; i <= expected.Length - count; i += count)
            {
                var vExp = Vector256.Create(expected.Slice(i));
                var vAct = Vector256.Create(actual.Slice(i));
                var diff = Avx2.Subtract(vExp, vAct);
                vSum = Avx2.Add(vSum, Avx2.Multiply(diff, diff));
            }
            sumSquaredError = Vector256.Sum(vSum);
        }

        // Stage 3: Scalar Fallback
        for (; i < expected.Length; i++)
        {
            double diff = expected[i] - actual[i];
            sumSquaredError += diff * diff;
        }

        return sumSquaredError / expected.Length;
    }

    /// <summary>
    /// Calculates the Root Mean Squared Error (RMSE).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateRmse(double mse) => Math.Sqrt(mse);

    /// <summary>
    /// Calculates the Mean Absolute Error (MAE).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateMae(ReadOnlySpan<double> expected, ReadOnlySpan<double> actual)
    {
        if (expected.Length != actual.Length) throw new ArgumentException("Dimensions do not match.");

        double sumAbsError = 0;
        int i = 0;

        // 1. AVX-512
        if (Avx512F.IsSupported)
        {
            var vSum = Vector512<double>.Zero;
            int count = Vector512<double>.Count;

            for (; i <= expected.Length - count; i += count)
            {
                var vExp = Vector512.Create(expected.Slice(i));
                var vAct = Vector512.Create(actual.Slice(i));
                var diff = Avx512F.Subtract(vExp, vAct);

                // AVX-512 Fix: Treat as Int64 to apply absolute mask, then cast back.
                var absDiff = Avx512F.And(diff.AsInt64(), AbsMask512).AsDouble();

                vSum = Avx512F.Add(vSum, absDiff);
            }
            sumAbsError = Vector512.Sum(vSum);
        }
        // 2. AVX2
        else if (Avx2.IsSupported)
        {
            var vSum = Vector256<double>.Zero;
            var vMask = AbsMask256.AsDouble();
            int count = Vector256<double>.Count;

            for (; i <= expected.Length - count; i += count)
            {
                var vExp = Vector256.Create(expected.Slice(i));
                var vAct = Vector256.Create(actual.Slice(i));
                var diff = Avx2.Subtract(vExp, vAct);

                var absDiff = Avx2.And(diff, vMask);
                vSum = Avx2.Add(vSum, absDiff);
            }
            sumAbsError = Vector256.Sum(vSum);
        }

        // 3. Fallback
        for (; i < expected.Length; i++)
        {
            sumAbsError += Math.Abs(expected[i] - actual[i]);
        }

        return sumAbsError / expected.Length;
    }

    #endregion

    #region Signal Quality (PSNR, SNR)

    /// <summary>
    /// Calculates Peak Signal-to-Noise Ratio (PSNR) in decibels (dB).
    /// Commonly used to measure the quality of reconstruction of lossy compression.
    /// </summary>
    /// <param name="mse">Mean Squared Error</param>
    /// <param name="maxValue">Maximum possible pixel value (default 1.0 for normalized data)</param>
    public static double CalculatePsnr(double mse, double maxValue = 1.0)
    {
        if (mse <= 1e-15) return 100.0; // Perfect match (Infinity dB cap)
        return 10 * Math.Log10((maxValue * maxValue) / mse);
    }

    /// <summary>
    /// Calculates Signal-to-Noise Ratio (SNR) in decibels (dB).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateSnr(ReadOnlySpan<double> signal, double mse)
    {
        if (mse <= 1e-15) return 100.0;

        double signalPower = 0;
        int i = 0;

        // Calculate Signal Power using SIMD
        if (Avx512F.IsSupported)
        {
            var vSum = Vector512<double>.Zero;
            int count = Vector512<double>.Count;
            for (; i <= signal.Length - count; i += count)
            {
                var vSig = Vector512.Create(signal.Slice(i));
                vSum = Avx512F.Add(vSum, Avx512F.Multiply(vSig, vSig));
            }
            signalPower = Vector512.Sum(vSum);
        }
        else if (Avx2.IsSupported)
        {
            var vSum = Vector256<double>.Zero;
            int count = Vector256<double>.Count;
            for (; i <= signal.Length - count; i += count)
            {
                var vSig = Vector256.Create(signal.Slice(i));
                vSum = Avx2.Add(vSum, Avx2.Multiply(vSig, vSig));
            }
            signalPower = Vector256.Sum(vSum);
        }

        // Scalar Fallback
        for (; i < signal.Length; i++)
        {
            signalPower += signal[i] * signal[i];
        }

        signalPower /= signal.Length;
        return 10 * Math.Log10(signalPower / mse);
    }

    #endregion

    #region Sensitivity Metrics (NPCR, Entropy)

    /// <summary>
    /// Calculates Number of Pixels Change Rate (NPCR) for double-precision sequences.
    /// Measures the percentage of different values between two sequences.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateNpcr(ReadOnlySpan<double> data1, ReadOnlySpan<double> data2)
    {
        if (data1.Length != data2.Length) throw new ArgumentException("Dimensions do not match.");

        long diffCount = 0;
        int i = 0;

        // 1. AVX-512
        if (Avx512F.IsSupported)
        {
            var vEpsilon = Vector512.Create(Epsilon);
            int count = Vector512<double>.Count;

            for (; i <= data1.Length - count; i += count)
            {
                var v1 = Vector512.Create(data1.Slice(i));
                var v2 = Vector512.Create(data2.Slice(i));

                var diff = Avx512F.Subtract(v1, v2);
                var absDiff = Avx512F.And(diff.AsInt64(), AbsMask512).AsDouble();
                var mask = Avx512F.CompareGreaterThan(absDiff, vEpsilon);

                diffCount += BitOperations.PopCount(mask.ExtractMostSignificantBits());
            }
        }
        // 2. AVX2
        else if (Avx2.IsSupported)
        {
            var vEpsilon = Vector256.Create(Epsilon);
            var vMaskAbs = AbsMask256.AsDouble();
            int count = Vector256<double>.Count;

            for (; i <= data1.Length - count; i += count)
            {
                var v1 = Vector256.Create(data1.Slice(i));
                var v2 = Vector256.Create(data2.Slice(i));

                var diff = Avx2.Subtract(v1, v2);
                var absDiff = Avx2.And(diff, vMaskAbs);
                var cmpRes = Avx2.CompareGreaterThan(absDiff, vEpsilon);

                int mask = Avx2.MoveMask(cmpRes);
                diffCount += BitOperations.PopCount((uint)mask);
            }
        }

        // 3. Fallback
        for (; i < data1.Length; i++)
        {
            if (Math.Abs(data1[i] - data2[i]) > Epsilon)
                diffCount++;
        }

        return (double)diffCount / data1.Length * 100.0;
    }

    /// <summary>
    /// Calculates Shannon Entropy for a sequence of double values.
    /// Note: This calculates entropy based on unique values found in the sequence.
    /// </summary>
    public static double CalculateEntropy(ReadOnlySpan<double> data)
    {
        // For floating point numbers, using a dictionary to count occurrences 
        // implies we are looking for exact matches.
        var counts = new Dictionary<double, int>(data.Length);
        foreach (var val in data)
        {
            CollectionsMarshal.GetValueRefOrAddDefault(counts, val, out bool exists)++;
        }

        double entropy = 0;
        double lenInv = 1.0 / data.Length;

        foreach (var count in counts.Values)
        {
            double p = count * lenInv;
            entropy -= p * Math.Log2(p);
        }
        return entropy;
    }

    #endregion
}
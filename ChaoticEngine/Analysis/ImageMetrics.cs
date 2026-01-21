using System.Runtime.CompilerServices;

namespace ChaoticEngine.Analysis;

/// <summary>
/// Provides statistical analysis tools specifically designed for byte-level data (images, encrypted streams).
/// Essential for validating cryptographic strength (Histogram, Correlation, Entropy).
/// </summary>
public static class ImageMetrics
{
    #region Histogram & Uniformity

    /// <summary>
    /// Calculates the frequency histogram of byte data.
    /// For a perfectly encrypted image, this should be flat (uniform distribution).
    /// </summary>
    /// <param name="data">The byte data to analyze.</param>
    /// <returns>A 256-element array where index i represents the count of byte value i.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long[] CalculateHistogram(ReadOnlySpan<byte> data)
    {
        long[] histogram = new long[256];

        unsafe
        {
            fixed (byte* ptr = data)
            {
                byte* p = ptr;
                int len = data.Length;
                // Pointer arithmetic loop for maximum throughput
                for (int i = 0; i < len; i++)
                {
                    histogram[p[i]]++;
                }
            }
        }
        return histogram;
    }

    /// <summary>
    /// Calculates the Chi-Square (χ²) statistic to test the uniformity of the data distribution.
    /// </summary>
    /// <remarks>
    /// <para>Interpretation:</para>
    /// <list type="bullet">
    /// <item>Values &lt; 290 indicate a Uniform Distribution (Pass).</item>
    /// <item>Values &gt; 290 indicate a Biased Distribution (Fail).</item>
    /// </list>
    /// Based on 256 degrees of freedom at 0.05 significance level.
    /// </remarks>
    /// <param name="data">The encrypted data buffer.</param>
    /// <returns>The Chi-Square value.</returns>
    public static double CalculateChiSquare(ReadOnlySpan<byte> data)
    {
        long[] histogram = CalculateHistogram(data);
        double expected = data.Length / 256.0;
        double chiSquare = 0.0;

        for (int i = 0; i < 256; i++)
        {
            double diff = histogram[i] - expected;
            chiSquare += (diff * diff) / expected;
        }

        return chiSquare;
    }

    #endregion

    #region Correlation Analysis

    /// <summary>
    /// Calculates the Correlation Coefficient between adjacent pixels/bytes.
    /// Measures how much a pixel depends on its neighbors.
    /// </summary>
    /// <param name="data">The image byte data.</param>
    /// <param name="stride">
    /// The step size to the next neighbor. 
    /// <br/>Use <b>1</b> for Horizontal, <b>Width</b> for Vertical, <b>Width+1</b> for Diagonal.
    /// </param>
    /// <returns>A value between -1.0 and 1.0. Ideally close to <b>0.0</b> for encrypted data.</returns>
    public static double CalculateCorrelation(ReadOnlySpan<byte> data, int stride)
    {
        // Note: Replaced specific 'mode' enum with generic 'stride' for flexibility.
        // stride = 1 (Horizontal)
        // stride = width (Vertical)
        // stride = width + 1 (Diagonal)

        if (data.Length == 0) return 0;

        // Accumulators
        double sumX = 0, sumY = 0;
        double sumXY = 0;
        double sumX2 = 0, sumY2 = 0;
        int N = 0;

        unsafe
        {
            fixed (byte* ptr = data)
            {
                int limit = data.Length - stride;
                if (limit <= 0) return 0;

                for (int i = 0; i < limit; i++)
                {
                    // x is the current pixel, y is the neighbor at 'stride' distance
                    double x = ptr[i];
                    double y = ptr[i + stride];

                    sumX += x;
                    sumY += y;
                    sumXY += (x * y);
                    sumX2 += (x * x);
                    sumY2 += (y * y);
                    N++;
                }
            }
        }

        if (N == 0) return 0;

        double numerator = (N * sumXY) - (sumX * sumY);
        double denomX = (N * sumX2) - (sumX * sumX);
        double denomY = (N * sumY2) - (sumY * sumY);

        // Avoid NaN if variance is zero (e.g., solid color image)
        if (denomX <= 0 || denomY <= 0) return 0;

        return numerator / Math.Sqrt(denomX * denomY);
    }

    #endregion

    #region Entropy

    /// <summary>
    /// Calculates the Shannon Entropy of the data in bits per byte.
    /// Measures the uncertainty or randomness of the information.
    /// </summary>
    /// <param name="data">The data buffer.</param>
    /// <returns>A value between 0.0 and 8.0. Ideally close to <b>8.0</b> (e.g., 7.999...) for encryption.</returns>
    public static double CalculateEntropy(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return 0;

        long[] histogram = CalculateHistogram(data);
        double entropy = 0.0;
        double lenInv = 1.0 / data.Length; // Inverse length for speed

        for (int i = 0; i < 256; i++)
        {
            if (histogram[i] > 0)
            {
                double p = histogram[i] * lenInv; // Probability
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }

    #endregion
}
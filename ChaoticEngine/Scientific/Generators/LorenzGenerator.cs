using ChaoticEngine.Scientific.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Scientific.Generators;

/// <summary>
/// Implements the Lorenz System, a system of ordinary differential equations first studied by Edward Lorenz.
/// <br/>Notable for having chaotic solutions for certain parameter values and the "Butterfly Effect".
/// </summary>
public class LorenzGenerator : IChaoticGenerator3D
{
    /// <summary>
    /// Parameter Sigma (Canonical value: 10.0).
    /// Represents the <b>Prandtl number</b> (ratio of momentum diffusivity to thermal diffusivity).
    /// </summary>
    public double Sigma { get; init; } = 10.0;

    /// <summary>
    /// Parameter Rho (Canonical value: 28.0).
    /// Represents the <b>Rayleigh number</b>. The system exhibits chaos when Rho > 24.74.
    /// </summary>
    public double Rho { get; init; } = 28.0;

    /// <summary>
    /// Parameter Beta (Canonical value: 8.0 / 3.0).
    /// Related to the geometric physical dimensions of the layer.
    /// </summary>
    public double Beta { get; init; } = 8.0 / 3.0;

    /// <summary>
    /// Time step (Delta T) for the Euler integration method.
    /// <br/>Default: 0.01. Smaller steps increase simulation accuracy but require more CPU cycles.
    /// </summary>
    public double Dt { get; init; } = 0.01;

    // Small epsilon to diverge parallel streams
    private const double Epsilon = 1e-10;

    /// <summary>
    /// Generates chaotic 3D trajectories using the Lorenz equations.
    /// </summary>
    public void Generate(Span<double> xBuf, Span<double> yBuf, Span<double> zBuf, double x0, double y0, double z0)
    {
        if (xBuf.Length != yBuf.Length || xBuf.Length != zBuf.Length)
            throw new ArgumentException("All buffer lengths must be equal.");

        int i = 0;

        // Stage 1: AVX-512 (8 Parallel Streams)
        if (Avx512F.IsSupported)
            i = GenerateAvx512(xBuf, yBuf, zBuf, x0, y0, z0);
        // Stage 2: AVX2 (4 Parallel Streams)
        else if (Avx2.IsSupported)
            i = GenerateAvx2(xBuf, yBuf, zBuf, x0, y0, z0);

        // Stage 3: Scalar Fallback
        double x = (i == 0) ? x0 : xBuf[i - 1];
        double y = (i == 0) ? y0 : yBuf[i - 1];
        double z = (i == 0) ? z0 : zBuf[i - 1];

        for (; i < xBuf.Length; i++)
        {
            // Lorenz Equations (Euler Method):
            // dx = sigma * (y - x) * dt
            // dy = (x * (rho - z) - y) * dt
            // dz = (x * y - beta * z) * dt

            double dx = Sigma * (y - x) * Dt;
            double dy = (x * (Rho - z) - y) * Dt;
            double dz = (x * y - Beta * z) * Dt;

            x += dx;
            y += dy;
            z += dz;

            xBuf[i] = x;
            yBuf[i] = y;
            zBuf[i] = z;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateAvx512(Span<double> xBuf, Span<double> yBuf, Span<double> zBuf, double x0, double y0, double z0)
    {
        int count = Vector512<double>.Count; // 8
        if (xBuf.Length < count) return 0;

        // Step 1: Multi-State Initialization
        Span<double> seedsX = stackalloc double[count];
        Span<double> seedsY = stackalloc double[count];
        Span<double> seedsZ = stackalloc double[count];

        for (int k = 0; k < count; k++)
        {
            seedsX[k] = x0 + (k * Epsilon);
            seedsY[k] = y0 + (k * Epsilon);
            seedsZ[k] = z0 + (k * Epsilon);
        }

        Vector512<double> vX = Vector512.Create(seedsX);
        Vector512<double> vY = Vector512.Create(seedsY);
        Vector512<double> vZ = Vector512.Create(seedsZ);

        Vector512<double> vSigma = Vector512.Create(Sigma);
        Vector512<double> vRho = Vector512.Create(Rho);
        Vector512<double> vBeta = Vector512.Create(Beta);
        Vector512<double> vDt = Vector512.Create(Dt);

        int i = 0;
        for (; i <= xBuf.Length - count; i += count)
        {
            // dx
            var yMinusX = Avx512F.Subtract(vY, vX);
            var sigmaTimesDiff = Avx512F.Multiply(vSigma, yMinusX);
            var dx = Avx512F.Multiply(sigmaTimesDiff, vDt);

            // dy
            var rhoMinusZ = Avx512F.Subtract(vRho, vZ);
            var xTimesRhoMinusZ = Avx512F.Multiply(vX, rhoMinusZ);
            var dyInner = Avx512F.Subtract(xTimesRhoMinusZ, vY);
            var dy = Avx512F.Multiply(dyInner, vDt);

            // dz
            var xTimesY = Avx512F.Multiply(vX, vY);
            var betaTimesZ = Avx512F.Multiply(vBeta, vZ);
            var dzInner = Avx512F.Subtract(xTimesY, betaTimesZ);
            var dz = Avx512F.Multiply(dzInner, vDt);

            // Update
            vX = Avx512F.Add(vX, dx);
            vY = Avx512F.Add(vY, dy);
            vZ = Avx512F.Add(vZ, dz);

            vX.CopyTo(xBuf.Slice(i));
            vY.CopyTo(yBuf.Slice(i));
            vZ.CopyTo(zBuf.Slice(i));
        }
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GenerateAvx2(Span<double> xBuf, Span<double> yBuf, Span<double> zBuf, double x0, double y0, double z0)
    {
        int count = Vector256<double>.Count; // 4
        if (xBuf.Length < count) return 0;

        Span<double> seedsX = stackalloc double[count];
        Span<double> seedsY = stackalloc double[count];
        Span<double> seedsZ = stackalloc double[count];

        for (int k = 0; k < count; k++)
        {
            seedsX[k] = x0 + (k * Epsilon);
            seedsY[k] = y0 + (k * Epsilon);
            seedsZ[k] = z0 + (k * Epsilon);
        }

        Vector256<double> vX = Vector256.Create(seedsX);
        Vector256<double> vY = Vector256.Create(seedsY);
        Vector256<double> vZ = Vector256.Create(seedsZ);

        Vector256<double> vSigma = Vector256.Create(Sigma);
        Vector256<double> vRho = Vector256.Create(Rho);
        Vector256<double> vBeta = Vector256.Create(Beta);
        Vector256<double> vDt = Vector256.Create(Dt);

        int i = 0;
        for (; i <= xBuf.Length - count; i += count)
        {
            var dx = Avx2.Multiply(vDt, Avx2.Multiply(vSigma, Avx2.Subtract(vY, vX)));

            var term1 = Avx2.Multiply(vX, Avx2.Subtract(vRho, vZ));
            var dy = Avx2.Multiply(vDt, Avx2.Subtract(term1, vY));

            var term2 = Avx2.Multiply(vX, vY);
            var term3 = Avx2.Multiply(vBeta, vZ);
            var dz = Avx2.Multiply(vDt, Avx2.Subtract(term2, term3));

            vX = Avx2.Add(vX, dx);
            vY = Avx2.Add(vY, dy);
            vZ = Avx2.Add(vZ, dz);

            vX.CopyTo(xBuf.Slice(i));
            vY.CopyTo(yBuf.Slice(i));
            vZ.CopyTo(zBuf.Slice(i));
        }
        return i;
    }
}
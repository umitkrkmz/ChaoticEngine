using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Core
{
    public class ChenGenerator : IChaoticGenerator3D
    {
        // Chen System Parameters (Standard Chaotic Region: a=35, b=3, c=28)
        public double A { get; init; } = 35.0;
        public double B { get; init; } = 3.0;
        public double C { get; init; } = 28.0;

        // Time step for differential equations
        public double Dt { get; init; } = 0.005;

        // Small epsilon to diverge parallel streams
        private const double Epsilon = 1e-10;

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

            // Pre-calculation
            double cMinusA = C - A;

            for (; i < xBuf.Length; i++)
            {
                // Chen System Equations (Euler Integration):
                // dx = a * (y - x)
                // dy = (c - a) * x - x * z + c * y
                // dz = x * y - b * z

                double dx = A * (y - x) * Dt;
                double dy = (cMinusA * x - x * z + C * y) * Dt;
                double dz = (x * y - B * z) * Dt;

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

            // Load Constants
            Vector512<double> vA = Vector512.Create(A);
            Vector512<double> vB = Vector512.Create(B);
            Vector512<double> vC = Vector512.Create(C);
            Vector512<double> vDt = Vector512.Create(Dt);
            Vector512<double> vCminusA = Vector512.Create(C - A);

            int i = 0;
            for (; i <= xBuf.Length - count; i += count)
            {
                // --- dx Calculation ---
                // dx = a * (y - x) * dt
                var yMinusX = Avx512F.Subtract(vY, vX);
                var aTimesDiff = Avx512F.Multiply(vA, yMinusX);
                var dx = Avx512F.Multiply(aTimesDiff, vDt);

                // --- dy Calculation ---
                // dy = ((c - a) * x - x * z + c * y) * dt

                // Part 1: (c - a) * x
                var term1 = Avx512F.Multiply(vCminusA, vX);
                // Part 2: x * z
                var term2 = Avx512F.Multiply(vX, vZ);
                // Part 3: c * y
                var term3 = Avx512F.Multiply(vC, vY);

                // Combine: (Term1 - Term2 + Term3) * Dt
                var dyInner = Avx512F.Add(Avx512F.Subtract(term1, term2), term3);
                var dy = Avx512F.Multiply(dyInner, vDt);

                // --- dz Calculation ---
                // dz = (x * y - b * z) * dt
                var xy = Avx512F.Multiply(vX, vY);
                var bz = Avx512F.Multiply(vB, vZ);
                var dz = Avx512F.Multiply(Avx512F.Subtract(xy, bz), vDt);

                // --- State Update ---
                vX = Avx512F.Add(vX, dx);
                vY = Avx512F.Add(vY, dy);
                vZ = Avx512F.Add(vZ, dz);

                // --- Write to Memory ---
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

            Vector256<double> vX = Vector256.Create(seedsX);
            Vector256<double> vY = Vector256.Create(seedsY);
            Vector256<double> vZ = Vector256.Create(seedsZ);

            Vector256<double> vA = Vector256.Create(A);
            Vector256<double> vB = Vector256.Create(B);
            Vector256<double> vC = Vector256.Create(C);
            Vector256<double> vDt = Vector256.Create(Dt);
            Vector256<double> vCminusA = Vector256.Create(C - A);

            int i = 0;
            for (; i <= xBuf.Length - count; i += count)
            {
                // dx = a * (y - x) * dt
                var dx = Avx2.Multiply(vDt, Avx2.Multiply(vA, Avx2.Subtract(vY, vX)));

                // dy = ((c - a) * x - x * z + c * y) * dt
                var term1 = Avx2.Multiply(vCminusA, vX);
                var term2 = Avx2.Multiply(vX, vZ);
                var term3 = Avx2.Multiply(vC, vY);
                var dy = Avx2.Multiply(vDt, Avx2.Add(Avx2.Subtract(term1, term2), term3));

                // dz = (x * y - b * z) * dt
                var termXy = Avx2.Multiply(vX, vY);
                var termBz = Avx2.Multiply(vB, vZ);
                var dz = Avx2.Multiply(vDt, Avx2.Subtract(termXy, termBz));

                // Update States
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
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Core
{
    public class HenonGenerator : IChaoticGenerator2D
    {
        // Henon Map Classic Parameters (Chaotic region: a=1.4, b=0.3)
        public double A { get; init; } = 1.4;
        public double B { get; init; } = 0.3;

        // Small epsilon to diverge parallel streams
        private const double Epsilon = 1e-10;

        public void Generate(Span<double> xBuffer, Span<double> yBuffer, double x0, double y0)
        {
            if (xBuffer.Length != yBuffer.Length)
                throw new ArgumentException("X and Y buffer lengths must be equal.");

            int i = 0;

            // Stage 1: AVX-512 (8 Parallel Streams)
            if (Avx512F.IsSupported)
                i = GenerateAvx512(xBuffer, yBuffer, x0, y0);
            // Stage 2: AVX2 (4 Parallel Streams)
            else if (Avx2.IsSupported)
                i = GenerateAvx2(xBuffer, yBuffer, x0, y0);

            // Stage 3: Scalar Fallback
            // Resume from where SIMD left off, or start from initial conditions
            double x = (i == 0) ? x0 : xBuffer[i - 1];
            double y = (i == 0) ? y0 : yBuffer[i - 1];

            for (; i < xBuffer.Length; i++)
            {
                // Henon Equation:
                // x(n+1) = 1 - a * x(n)^2 + y(n)
                // y(n+1) = b * x(n)

                double nextX = 1.0 - A * (x * x) + y;
                double nextY = B * x;

                x = nextX;
                y = nextY;

                xBuffer[i] = x;
                yBuffer[i] = y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GenerateAvx512(Span<double> xBuffer, Span<double> yBuffer, double x0, double y0)
        {
            int count = Vector512<double>.Count; // 8
            if (xBuffer.Length < count) return 0;

            // Step 1: Multi-State Initialization
            Span<double> seedsX = stackalloc double[count];
            Span<double> seedsY = stackalloc double[count];

            for (int k = 0; k < count; k++)
            {
                // Shift each lane by epsilon to initialize. 
                // No modulo needed as Henon map operates in a bounded area.
                seedsX[k] = x0 + (k * Epsilon);
                seedsY[k] = y0 + (k * Epsilon);
            }

            Vector512<double> vX = Vector512.Create(seedsX);
            Vector512<double> vY = Vector512.Create(seedsY);

            Vector512<double> vA = Vector512.Create(A);
            Vector512<double> vB = Vector512.Create(B);
            Vector512<double> vOne = Vector512.Create(1.0);

            int i = 0;
            for (; i <= xBuffer.Length - count; i += count)
            {
                // Formula: x_next = 1 - a * x^2 + y
                var xSquared = Avx512F.Multiply(vX, vX);                // x^2
                var aXSquared = Avx512F.Multiply(vA, xSquared);         // a * x^2
                var oneMinusAx2 = Avx512F.Subtract(vOne, aXSquared);    // 1 - a * x^2
                var vNextX = Avx512F.Add(oneMinusAx2, vY);              // ... + y

                // Formula: y_next = b * x
                // NOTE: Must use 'old x' (vX) here, not vNextX. Order is important.
                var vNextY = Avx512F.Multiply(vB, vX);

                // State Update
                vX = vNextX;
                vY = vNextY;

                // Write to Memory
                vX.CopyTo(xBuffer.Slice(i));
                vY.CopyTo(yBuffer.Slice(i));
            }
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GenerateAvx2(Span<double> xBuffer, Span<double> yBuffer, double x0, double y0)
        {
            int count = Vector256<double>.Count; // 4
            if (xBuffer.Length < count) return 0;

            // Step 1: Multi-State Initialization
            Span<double> seedsX = stackalloc double[count];
            Span<double> seedsY = stackalloc double[count];

            for (int k = 0; k < count; k++)
            {
                seedsX[k] = x0 + (k * Epsilon);
                seedsY[k] = y0 + (k * Epsilon);
            }

            Vector256<double> vX = Vector256.Create(seedsX);
            Vector256<double> vY = Vector256.Create(seedsY);

            Vector256<double> vA = Vector256.Create(A);
            Vector256<double> vB = Vector256.Create(B);
            Vector256<double> vOne = Vector256.Create(1.0);

            int i = 0;
            for (; i <= xBuffer.Length - count; i += count)
            {
                // x_next = 1 - a * x^2 + y
                var xSquared = Avx2.Multiply(vX, vX);
                var aXSquared = Avx2.Multiply(vA, xSquared);
                var oneMinusAx2 = Avx2.Subtract(vOne, aXSquared);
                var vNextX = Avx2.Add(oneMinusAx2, vY);

                // y_next = b * x
                var vNextY = Avx2.Multiply(vB, vX);

                vX = vNextX;
                vY = vNextY;

                vX.CopyTo(xBuffer.Slice(i));
                vY.CopyTo(yBuffer.Slice(i));
            }
            return i;
        }
    }
}
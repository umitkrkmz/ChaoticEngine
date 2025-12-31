using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using ChaoticEngine.Core;

namespace ChaoticEngine.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        // 1. AUTOMATIC SELECTION:
        // If no specific request comes from the command line (if args are empty), we automatically issue the "*" (Run All) command. // This way, it doesn't ask for a menu and starts directly.
        if (args.Length == 0)
        {
            args = ["*"];
        }

        // 2. COMBINING RESULTS:
        // The JoinSummary option attempts to present the reports of all classes as a single report at the end of the test.
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.JoinSummary);

        // run benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}

// === BASE CLASS SETTINGS ===
[MemoryDiagnoser]
public abstract class BaseChaosBenchmark
{
    [Params(1_000_000)]
    public int N;

    protected double[] data;
    protected double[] bufX, bufY, bufZ;

    [GlobalSetup]
    public void Setup()
    {
        data = new double[N];
        bufX = new double[N];
        bufY = new double[N];
        bufZ = new double[N];
    }
}

// =========================================================
// 1. LOGISTIC MAP
// =========================================================
public class LogisticBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.5;
        for (int i = 0; i < N; i++)
        {
            x = 3.99 * x * (1 - x);
            data[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create1D(ChaosType.LogisticMap);
        generator.Generate(data, 0.5);
    }
}

// =========================================================
// 2. TENT MAP
// =========================================================
public class TentBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.5;
        for (int i = 0; i < N; i++)
        {
            x = x < 0.5 ? 1.9999 * x : 1.9999 * (1 - x);
            data[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create1D(ChaosType.TentMap);
        generator.Generate(data, 0.5);
    }
}

// =========================================================
// 3. SINE MAP (Bhaskara Farkı)
// =========================================================
public class SineBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.5;
        for (int i = 0; i < N; i++)
        {
            x = 4.0 * Math.Sin(Math.PI * x);
            data[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create1D(ChaosType.SineMap);
        generator.Generate(data, 0.5);
    }
}

// =========================================================
// 4. HENON MAP (2D)
// =========================================================
public class HenonBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.1, y = 0.1;
        for (int i = 0; i < N; i++)
        {
            double nx = 1 - 1.4 * x * x + y;
            y = 0.3 * x;
            x = nx;
            bufX[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create2D(ChaosType.HenonMap);
        generator.Generate(bufX, bufY, 0.1, 0.1);
    }
}

// =========================================================
// 5. LORENZ SYSTEM (3D)
// =========================================================
public class LorenzBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.1, y = 0.1, z = 0.1;
        double dt = 0.01;
        for (int i = 0; i < N; i++)
        {
            double dx = 10 * (y - x) * dt;
            double dy = (x * (28 - z) - y) * dt;
            double dz = (x * y - 8.0 / 3.0 * z) * dt;
            x += dx; y += dy; z += dz;
            bufX[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create3D(ChaosType.LorenzSystem);
        generator.Generate(bufX, bufY, bufZ, 0.1, 0.1, 0.1);
    }
}

// =========================================================
// 6. CHEN SYSTEM (3D)
// =========================================================
public class ChenBenchmarks : BaseChaosBenchmark
{
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        double x = 0.1, y = 0.1, z = 0.1;
        double dt = 0.005;
        for (int i = 0; i < N; i++)
        {
            double dx = 35 * (y - x) * dt;
            double dy = ((28 - 35) * x - x * z + 28 * y) * dt;
            double dz = (x * y - 3 * z) * dt;
            x += dx; y += dy; z += dz;
            bufX[i] = x;
        }
    }

    [Benchmark]
    public void AVX()
    {
        var generator = ChaosFactory.Create3D(ChaosType.ChenSystem);
        generator.Generate(bufX, bufY, bufZ, 0.1, 0.1, 0.1);
    }
}
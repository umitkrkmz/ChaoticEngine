using ChaoticEngine.Core;
using ChaoticEngine.Analysis;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        // === INTRODUCTION ===
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("================================================================");
        Console.WriteLine("   CHAOTIC ENGINE .NET 10 | HIGH-PERFORMANCE SECURITY SUITE     ");
        Console.WriteLine("================================================================");
        Console.ResetColor();

        Console.WriteLine($"[System] CPU Cores: {Environment.ProcessorCount}");
        Console.WriteLine($"[SIMD Support] AVX-512: {(Avx512F.IsSupported ? "ACTIVE" : "NONE")} | AVX2: {(Avx2.IsSupported ? "ACTIVE" : "NONE")}");
        Console.WriteLine($"[Timer] High Resolution: {Stopwatch.IsHighResolution}\n");

        int dataSize = 1_000_000; // 1 Million Samples
        int warmup = 3;           // JIT Warm-up rounds

        // =================================================================================
        // SECTION 1: PERFORMANCE LEAGUE (AVX OPTIMIZATION SHOWCASE)
        // =================================================================================
        PrintHeader("SECTION 1: ALGORITHM PERFORMANCE LEAGUE (1 Million Samples)");
        Console.WriteLine("{0,-15} | {1,-12} | {2,-12} | {3,-10} | {4,-10}", "ALGORITHM", "AVX (ms)", "SCALAR (ms)", "SPEEDUP", "ENTROPY");
        Console.WriteLine(new string('-', 75));

        RunBenchmark("Logistic Map", ChaosType.LogisticMap, dataSize, warmup);
        RunBenchmark("Tent Map", ChaosType.TentMap, dataSize, warmup);
        RunBenchmark("Sine Map", ChaosType.SineMap, dataSize, warmup); // Bhaskara optimization shines here
        RunBenchmark("Henon Map", ChaosType.HenonMap, dataSize, warmup);
        RunBenchmark("Lorenz Sys", ChaosType.LorenzSystem, dataSize, warmup);
        RunBenchmark("Chen Sys", ChaosType.ChenSystem, dataSize, warmup);

        Console.WriteLine(new string('-', 75));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("* Scalar: Standard C# loops (Reference implementation).");
        Console.WriteLine("* AVX: ChaoticEngine's optimized hardware-accelerated engine.");
        Console.ResetColor();

        // =================================================================================
        // SECTION 2: STEGANOGRAPHY DEMO (LSB - LOSSLESS DATA HIDING)
        // =================================================================================
        Console.WriteLine();
        PrintHeader("SECTION 2: REAL-WORLD SCENARIO (LSB STEGANOGRAPHY)");
        Console.WriteLine("Scenario: Hiding an encrypted image payload inside an audio signal using Chen Chaos.");

        // 1. Data Preparation
        Console.Write(">> Preparing Data... ");
        double[] coverAudio = new double[dataSize];
        double[] imagePayload = new double[dataSize];
        double[] chaosKey = new double[dataSize];

        // Simulation Data
        for (int i = 0; i < dataSize; i++) coverAudio[i] = Math.Sin(i * 0.05); // Sine wave audio
        var rnd = new Random(42);
        for (int i = 0; i < dataSize; i++) imagePayload[i] = rnd.Next(0, 2);   // Binary image data (0-1)
        Console.WriteLine("[OK]");

        // 2. Key Generation
        Console.Write(">> Generating Key with Chen System... ");
        var chen = ChaosFactory.Create3D(ChaosType.ChenSystem);
        chen.Generate(chaosKey, new double[dataSize], new double[dataSize], 0.1, 0.1, 0.1);

        // Normalize Key (for 0-1 binary key stream)
        for (int i = 0; i < dataSize; i++) chaosKey[i] = Math.Abs(chaosKey[i]) % 1.0;
        Console.WriteLine("[OK]");

        // 3. Encryption (XOR)
        Console.Write(">> Encrypting Payload (Stream Cipher)... ");
        int[] encryptedBits = new int[dataSize];
        for (int i = 0; i < dataSize; i++)
        {
            int keyBit = chaosKey[i] > 0.5 ? 1 : 0;
            encryptedBits[i] = (int)imagePayload[i] ^ keyBit;
        }
        Console.WriteLine("[OK]");

        // 4. Embedding (LSB)
        Console.Write(">> Embedding into Audio (LSB 1-bit)... ");
        double[] stegoAudio = new double[dataSize];
        for (int i = 0; i < dataSize; i++)
        {
            short pcm = (short)(coverAudio[i] * 32767); // Double -> PCM
            pcm = (short)((pcm & ~1) | encryptedBits[i]); // Replace LSB
            stegoAudio[i] = pcm / 32767.0; // PCM -> Double
        }
        Console.WriteLine("[OK]");

        // =================================================================================
        // SECTION 3: ANALYSIS & REPORTING
        // =================================================================================
        Console.WriteLine();
        PrintHeader("SECTION 3: LABORATORY RESULTS");

        // Analysis A: Imperceptibility
        double audioMse = QualityMetrics.CalculateMse(coverAudio, stegoAudio);
        double audioPsnr = QualityMetrics.CalculatePsnr(audioMse, 2.0); // Audio range is 2.0 (-1 to 1)

        Console.WriteLine($"[1] AUDIO QUALITY (Steganography Imperceptibility):");
        Console.WriteLine($"   - MSE (Error Rate): {audioMse:E4}");
        Console.ForegroundColor = audioPsnr > 60 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"   - PSNR: {audioPsnr:F2} dB (Target: >60 dB)");
        Console.ResetColor();

        // Analysis B: Data Recovery (Extraction & Decryption)
        double[] recoveredPayload = new double[dataSize];
        for (int i = 0; i < dataSize; i++)
        {
            short pcm = (short)(stegoAudio[i] * 32767);
            int extractedBit = pcm & 1; // Extract bit
            int keyBit = chaosKey[i] > 0.5 ? 1 : 0;
            recoveredPayload[i] = extractedBit ^ keyBit; // Decrypt
        }

        double payloadMse = QualityMetrics.CalculateMse(imagePayload, recoveredPayload);
        double npcr = QualityMetrics.CalculateNpcr(imagePayload, recoveredPayload);

        Console.WriteLine($"\n[2] DATA INTEGRITY (Decryption Check):");
        Console.WriteLine($"   - MSE (Error): {payloadMse:F10}");
        Console.ForegroundColor = payloadMse < 1e-9 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"   - NPCR (Loss): %{npcr:F4}");

        if (payloadMse < 1e-9)
            Console.WriteLine("   >> RESULT: DATA RECOVERED 100% LOSSLESSLY.");
        else
            Console.WriteLine("   >> RESULT: DATA LOSS DETECTED!");
        Console.ResetColor();

        Console.WriteLine("\n=== BENCHMARK COMPLETED (Press any key to exit) ===");
        Console.ReadKey();
    }

    // --- HELPER METHODS ---
    static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.ResetColor();
    }

    static void RunBenchmark(string name, ChaosType type, int size, int warmup)
    {
        double[] buf = new double[size];

        // 1. AVX Measurement (Using Factory)
        Action avxAction = () => {
            if (type == ChaosType.LorenzSystem || type == ChaosType.ChenSystem)
                ChaosFactory.Create3D(type).Generate(buf, new double[size], new double[size], 0.1, 0.1, 0.1);
            else if (type == ChaosType.HenonMap)
                ChaosFactory.Create2D(type).Generate(buf, new double[size], 0.1, 0.1);
            else
                ChaosFactory.Create1D(type).Generate(buf, 0.5);
        };
        double tAvx = MeasureTime(avxAction, warmup);

        // 2. Scalar Measurement (Reference Code)
        Action scalarAction = () => ScalarReference.Run(type, size);
        double tScalar = MeasureTime(scalarAction, warmup);

        // 3. Entropy
        double entropy = QualityMetrics.CalculateEntropy(buf.AsSpan(0, Math.Min(size, 50000)));

        // Output
        double speedup = tScalar / tAvx;
        Console.Write("{0,-15} | {1,-12:F4} | {2,-12:F4} | ", name, tAvx, tScalar);

        // Colorize Speedup
        if (speedup > 8.0) Console.ForegroundColor = ConsoleColor.Magenta; // Excellent
        else if (speedup > 2.0) Console.ForegroundColor = ConsoleColor.Green; // Good
        else Console.ForegroundColor = ConsoleColor.White;

        Console.Write("{0,-10:F1}x", speedup);
        Console.ResetColor();
        Console.WriteLine($" | {entropy:F6}");
    }

    static double MeasureTime(Action action, int warmup)
    {
        for (int i = 0; i < warmup; i++) action(); // Warmup
        long start = Stopwatch.GetTimestamp();
        action();
        long end = Stopwatch.GetTimestamp();
        return (end - start) * 1000.0 / Stopwatch.Frequency;
    }
}

// === REFERENCE SCALAR IMPLEMENTATION (FOR BENCHMARKING ONLY) ===
// This class is not part of the library; it represents standard 
// (unoptimized) C# performance for comparison.
static class ScalarReference
{
    public static void Run(ChaosType type, int size)
    {
        double[] b = new double[size];
        switch (type)
        {
            case ChaosType.LogisticMap: Logistic(b); break;
            case ChaosType.TentMap: Tent(b); break;
            case ChaosType.SineMap: Sine(b); break;
            case ChaosType.HenonMap: Henon(b); break;
            case ChaosType.LorenzSystem: Lorenz(b); break;
            case ChaosType.ChenSystem: Chen(b); break;
        }
    }

    static void Logistic(double[] b) { double x = 0.5; for (int i = 0; i < b.Length; i++) { x = 3.99 * x * (1 - x); b[i] = x; } }
    static void Tent(double[] b) { double x = 0.5; for (int i = 0; i < b.Length; i++) { x = x < 0.5 ? 1.9999 * x : 1.9999 * (1 - x); b[i] = x; } }
    // Sine Map: Scalar uses Math.Sin (SLOW)
    static void Sine(double[] b) { double x = 0.5; for (int i = 0; i < b.Length; i++) { x = 4.0 * Math.Sin(Math.PI * x); b[i] = x; } }
    static void Henon(double[] xB) { double x = 0.1, y = 0.1; for (int i = 0; i < xB.Length; i++) { double nx = 1 - 1.4 * x * x + y; y = 0.3 * x; x = nx; xB[i] = x; } }
    static void Lorenz(double[] xB) { double x = 0.1, y = 0.1, z = 0.1; double dt = 0.01; for (int i = 0; i < xB.Length; i++) { double dx = 10 * (y - x) * dt; double dy = (x * (28 - z) - y) * dt; double dz = (x * y - 8.0 / 3.0 * z) * dt; x += dx; y += dy; z += dz; xB[i] = x; } }
    static void Chen(double[] xB) { double x = 0.1, y = 0.1, z = 0.1; double dt = 0.005; for (int i = 0; i < xB.Length; i++) { double dx = 35 * (y - x) * dt; double dy = ((28 - 35) * x - x * z + 28 * y) * dt; double dz = (x * y - 3 * z) * dt; x += dx; y += dy; z += dz; xB[i] = x; } }
}
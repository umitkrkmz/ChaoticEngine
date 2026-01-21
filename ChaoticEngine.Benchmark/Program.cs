using System.Diagnostics;
using System.Text;
using ChaoticEngine.Analysis;
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;

// --- 1. INTRO SCREEN AND BANNER ---
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
   ______ __                  __  _        ______            _            
  / ____// /_   ____ _ ____  / /_(_)_____ / ____/____   ____ _(_)___  ___ 
 / /    / __ \ / __ `// __ \/ __// // ___// __/  / __ \ / __ `// // __ \/ _ \
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;
/ /___ / / / // /_/ // /_/ / /_ / // /__ / /___ / / / // /_/ // // / / /  __/
\____//_/ /_/ \__,_/ \____/\__//_/ \___//_____//_/ /_/ \__, //_//_/ /_/\___/ 
                                                      /____/                 
   High-Performance Chaotic Cryptography Library | v1.0.0
");
Console.ResetColor();

// --- 2. HARDWARE CAPABILITY ANALYSIS ---
Console.WriteLine(" [1] System Capability Check:");
if (System.Runtime.Intrinsics.X86.Avx512F.IsSupported)
    PrintStatus(">> AVX-512 Acceleration", "AVAILABLE (Ultra Mode) 🚀", ConsoleColor.Green);
else if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
    PrintStatus(">> AVX2 Acceleration", "AVAILABLE (Turbo Mode) 🔥", ConsoleColor.Green);
else if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported)
    PrintStatus(">> ARM NEON Acceleration", "AVAILABLE (Mobile Mode) 📱", ConsoleColor.Green);
else
    PrintStatus(">> SIMD Acceleration", "NOT FOUND (Scalar Mode) 🐢", ConsoleColor.Yellow);

// --- 3. SANITY CHECK (INTEGRITY TEST) ---
// We select the most complex algorithm (Lorenz 3D) to verify the engine's correctness.
Console.WriteLine("\n [2] Integrity Self-Test (Lorenz 3D):");
string secret = "ChaoticEngine: Where Speed meets Chaos.";
byte[] originalBytes = Encoding.UTF8.GetBytes(secret);
byte[] buffer = new byte[originalBytes.Length];
Array.Copy(originalBytes, buffer, originalBytes.Length);

byte[] key = new byte[32]; new Random().NextBytes(key);
byte[] iv = new byte[16]; new Random().NextBytes(iv);

// Encrypt
ChaosCipher3D<IntegerLorenz>.Process(buffer, key, iv);
Console.WriteLine($"    Encrypted Hex: {BitConverter.ToString(buffer)}");

// Decrypt (Symmetric stream cipher: running the process again decrypts it)
ChaosCipher3D<IntegerLorenz>.Process(buffer, key, iv);
string decrypted = Encoding.UTF8.GetString(buffer);

if (secret == decrypted)
    PrintStatus(">> Encryption/Decryption Cycle", "PASS ✅", ConsoleColor.Green);
else
    PrintStatus(">> Encryption/Decryption Cycle", "FAIL ❌", ConsoleColor.Red);

// --- 4. BENCHMARK ARENA ---
Console.WriteLine("\n [3] BENCHMARK ARENA (128 MB Data Processing):");
Console.WriteLine(new string('-', 75));
Console.WriteLine($"{"Algorithm",-20} | {"Dim",-5} | {"Speed (GB/s)",-15} | {"Randomness (Chi2)",-15}");
Console.WriteLine(new string('-', 75));

// Prepare Test Data (Fill with random noise to simulate realistic high-entropy data)
int dataSize = 128 * 1024 * 1024; // 128 MB
byte[] benchSourceData = new byte[dataSize];
new Random().NextBytes(benchSourceData);

// --- LET THE RACE BEGIN ---

// 1D Algorithms
RunBenchmark1D<IntegerTentMap>("Tent Map");
RunBenchmark1D<IntegerLogisticMap>("Logistic Map");
RunBenchmark1D<IntegerSineMap>("Sine Map");

// 2D Algorithms
RunBenchmark2D<IntegerHenonMap>("Henon Map");

// 3D Algorithms
RunBenchmark3D<IntegerLorenz>("Lorenz System");
RunBenchmark3D<IntegerChen>("Chen System");

Console.WriteLine(new string('-', 75));
Console.WriteLine("\nAll systems operational. Press any key to exit...");
Console.ReadKey();

// --- HELPER METHODS (To avoid code duplication) ---

void RunBenchmark1D<T>(string name) where T : struct, IChaoticPrimitive
{
    // Create a clean copy of data to ensure fair conditions for every run
    byte[] workData = new byte[dataSize];
    benchSourceData.CopyTo(workData, 0);

    GC.Collect(); // Force Garbage Collection to clean up RAM before the run
    var sw = Stopwatch.StartNew();

    ChaosCipher<T>.Process(workData, key, iv);

    sw.Stop();
    ReportResult(name, "1D", sw.Elapsed.TotalSeconds, workData);
}

void RunBenchmark2D<T>(string name) where T : struct, IChaoticPrimitive2D
{
    byte[] workData = new byte[dataSize];
    benchSourceData.CopyTo(workData, 0);

    GC.Collect();
    var sw = Stopwatch.StartNew();

    ChaosCipher2D<T>.Process(workData, key, iv);

    sw.Stop();
    ReportResult(name, "2D", sw.Elapsed.TotalSeconds, workData);
}

void RunBenchmark3D<T>(string name) where T : struct, IChaoticPrimitive3D
{
    byte[] workData = new byte[dataSize];
    benchSourceData.CopyTo(workData, 0);

    GC.Collect();
    var sw = Stopwatch.StartNew();

    ChaosCipher3D<T>.Process(workData, key, iv);

    sw.Stop();
    ReportResult(name, "3D", sw.Elapsed.TotalSeconds, workData);
}

void ReportResult(string name, string dim, double timeSeconds, Span<byte> data)
{
    double throughput = (dataSize / (1024.0 * 1024.0 * 1024.0)) / timeSeconds;

    // Calculate Chi-Square only on the first 1MB for speed (Representative sample)
    double chi2 = ImageMetrics.CalculateChiSquare(data.Slice(0, 1024 * 1024));

    PrintRow(name, dim, throughput, chi2);
}

void PrintStatus(string label, string status, ConsoleColor color)
{
    Console.Write($"{label,-40}");
    Console.ForegroundColor = color;
    Console.WriteLine(status);
    Console.ResetColor();
}

void PrintRow(string name, string type, double speed, double chi2)
{
    string status = chi2 < 290 ? "PASS" : "WARN"; // Chi2 < 256+epsilon is ideal
    // Color coding for speed
    ConsoleColor speedColor = speed > 4.0 ? ConsoleColor.Green : (speed > 2.0 ? ConsoleColor.Yellow : ConsoleColor.White);

    Console.Write($"{name,-20} | {type,-5} | ");
    Console.ForegroundColor = speedColor;
    Console.Write($"{speed,-15:F2}");
    Console.ResetColor();
    Console.Write($" | {chi2,-10:F2} ({status})");
    Console.WriteLine();
}

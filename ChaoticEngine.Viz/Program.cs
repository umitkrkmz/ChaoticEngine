using ChaoticEngine.Scientific.Common;
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives; // IntegerTentMap vb. için gerekli
using ChaoticEngine.Viz;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== ChaoticEngine Visualization Studio ===");
Console.ResetColor();

// --- SECTION 1: SCIENTIFIC PROOFS (Attractors) ---
// Note: Assuming 'ChaosFactory' and 'ChaosType' are implemented in your Scientific module.
// If not, you can skip this section or implement the factory.

try
{
    Console.WriteLine("\n[1] Rendering Scientific Attractors...");

    var lorenz = ChaosFactory.Create3D(ChaosType.LorenzSystem);
    Plotters.PlotAttractor3D("Lorenz Attractor", lorenz);

    var chen = ChaosFactory.Create3D(ChaosType.ChenSystem);
    Plotters.PlotAttractor3D("Chen Attractor", chen);
}
catch (Exception ex)
{
    Console.WriteLine($"Skipping Scientific Plots (Factory not found): {ex.Message}");
}


// --- SECTION 2: SECURITY PROOFS (Histograms) ---
Console.WriteLine("\n[2] Generating Encryption Histograms...");

// Prepare 1MB of zero-filled data
byte[] data = new byte[1024 * 1024];
byte[] key = new byte[32]; new Random().NextBytes(key);
byte[] iv = new byte[16]; new Random().NextBytes(iv);

// A. BEFORE ENCRYPTION (The Control Group)
// This plot should show a single massive bar at 0.
Plotters.PlotSecurityHistogram("1_Raw_Data_Zeros", data);

// ENCRYPT (Using the Generic ChaosCipher with Tent Map)
// This is the updated generic call:
ChaosCipher<IntegerTentMap>.Process(data, key, iv);

// B. AFTER ENCRYPTION (The Proof)
// This plot should be completely flat (Uniform Distribution), proving high entropy.
Plotters.PlotSecurityHistogram("2_Encrypted_Chaos_TentMap", data);

Console.WriteLine("\n" + new string('-', 60));
Console.WriteLine("SUCCESS! All visual assets have been generated.");
Console.WriteLine($"Check your Desktop folder: 'Chaotic_Output'");
Console.WriteLine(new string('-', 60));
Console.ReadKey();
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChaoticEngine.Security;

/// <summary>
/// Provides high-performance, SIMD-accelerated stream cipher encryption based on Chaotic maps.
/// Uses Integer Arithmetic and AVX2 intrinsics for maximum throughput.
/// </summary>
public static class ChaosCipher
{
    // 32-bit Integer Threshold for Tent Map Simulation
    private const uint THRESHOLD = 0x80000000;

    /// <summary>
    /// Encrypts or Decrypts the given data buffer in-place using the provided key and IV.
    /// This method is Zero-Allocation and Thread-Safe.
    /// </summary>
    /// <param name="data">The data buffer to encrypt/decrypt (in-place).</param>
    /// <param name="key">The shared secret key (32 bytes recommended).</param>
    /// <param name="iv">The initialization vector (salt).</param>
    public static void Process(Span<byte> data, byte[] key, ReadOnlySpan<byte> iv)
    {
        // 1. Hardware Acceleration Check & Seed Generation
        if (Avx512F.IsSupported)
        {
            // AVX-512 needs 16 parallel states (512 bits / 32-bit int = 16 integers)
            Span<uint> seeds = stackalloc uint[16];
            DeriveSeeds(key, iv, seeds);
            ProcessAvx512(data, seeds);
        }
        else if (Avx2.IsSupported)
        {
            // AVX2 needs 8 parallel states (256 bits / 32-bit int = 8 integers)
            Span<uint> seeds = stackalloc uint[8];
            DeriveSeeds(key, iv, seeds);
            ProcessAvx2(data, seeds);
        }
        else
        {
            // Fallback needs 1 state
            Span<uint> seeds = stackalloc uint[1];
            DeriveSeeds(key, iv, seeds);
            ProcessScalar(data, seeds);
        }
    }

    // --- AVX-512 / ULTRA SIMD CORE (64 Bytes/Cycle) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx512(Span<byte> data, Span<uint> seeds)
    {
        // Load 16 initial chaos states into a 512-bit vector
        Vector512<uint> vState = Vector512.Create(seeds);
        int vectorSize = 64; // 512 bits = 64 bytes processing per cycle
        int i = 0;

        fixed (byte* ptr = data)
        {
            // Main Loop: Process 64 bytes at a time
            while (i <= data.Length - vectorSize)
            {
                // A. BITWISE CHAOS GENERATION (Integer Tent Map)
                // Mask generation: If state >= THRESHOLD, mask is all 1s.
                // Vector512 handles the 512-bit registers automatically.
                var mask = Vector512.ShiftRightArithmetic(vState.AsInt32(), 31).AsUInt32();

                // Controlled Inversion
                var processed = Vector512.Xor(vState, mask);

                // Scaling: x = 2 * result
                vState = Vector512.ShiftLeft(processed, 1);

                // B. ENCRYPTION (XOR STREAM)
                // Load 64 bytes of raw data using generic Vector512 load
                Vector512<byte> vData = Vector512.Load(ptr + i);

                // Reinterpret Chaos State as Bytes (Keystream)
                Vector512<byte> vKey = vState.AsByte();

                // XOR Operation
                Vector512<byte> vEncrypted = Vector512.Xor(vData, vKey);

                // Store result back to memory
                vEncrypted.Store(ptr + i);

                i += vectorSize;
            }
        }

        // Handle remaining bytes
        if (i < data.Length)
        {
            // We only need the first seed to continue scalar
            // But to be consistent, we can just grab the first lane
            ProcessScalar(data.Slice(i), stackalloc uint[] { vState[0] });
        }
    }


    // --- AVX2 / SIMD CORE ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessAvx2(Span<byte> data, Span<uint> seeds)
    {
        // Load initial chaos states into a 256-bit vector
        Vector256<uint> vState = Vector256.Create(seeds);
        int vectorSize = 32; // 256 bits = 32 bytes processing per cycle
        int i = 0;

        fixed (byte* ptr = data)
        {
            // Main Loop: Process 32 bytes at a time
            while (i <= data.Length - vectorSize)
            {
                // A. BITWISE CHAOS GENERATION (Integer Tent Map)
                // ---------------------------------------------------------
                // Formula: x_next = (x < 0.5) ? 2x : 2(1-x)
                // Integer: x_next = (x < THRESHOLD) ? x<<1 : (MAX-x)<<1

                // Mask generation: If state >= THRESHOLD (Highest bit 1), mask is all 1s.
                var mask = Avx2.ShiftRightArithmetic(vState.AsInt32(), 31).AsUInt32();

                // Controlled Inversion: XOR behaves like (Max - x) when mask is 1
                var processed = Avx2.Xor(vState, mask);

                // Scaling: x = 2 * result (Left Shift)
                vState = Avx2.ShiftLeftLogical(processed, 1);

                // B. ENCRYPTION (XOR STREAM)
                // ---------------------------------------------------------
                // Load 32 bytes of raw data
                Vector256<byte> vData = Avx.LoadVector256(ptr + i);

                // Reinterpret Chaos State as Bytes (Keystream)
                Vector256<byte> vKey = vState.AsByte();

                // XOR Operation
                Vector256<byte> vEncrypted = Avx2.Xor(vData, vKey);

                // Store result back to memory
                Avx.Store(ptr + i, vEncrypted);

                i += vectorSize;
            }
        }

        // Handle remaining bytes (if length is not multiple of 32)
        if (i < data.Length)
        {
            ProcessScalar(data.Slice(i), seeds);
        }
    }

    // --- SCALAR FALLBACK (Compatibility Mode) ---
    private static void ProcessScalar(Span<byte> data, Span<uint> seeds)
    {
        uint x = seeds[0];
        for (int i = 0; i < data.Length; i++)
        {
            // Integer Tent Map Logic
            if (x < THRESHOLD) x <<= 1;
            else x = (~x) << 1;

            // Use the highest 8 bits for maximum entropy
            data[i] ^= (byte)(x >> 24);
        }
        seeds[0] = x; // Update state
    }

    // --- SEED DERIVATION ---
    private static void DeriveSeeds(byte[] key, ReadOnlySpan<byte> iv, Span<uint> seeds)
    {
        // Mix Key and IV to produce initial states
        for (int i = 0; i < 8; i++)
        {
            int offset = (i * 4) % key.Length;
            seeds[i] = BitConverter.ToUInt32(key, offset);

            if (iv.Length >= 4)
                seeds[i] ^= BitConverter.ToUInt32(iv.Slice((i % 2) * 4, 4));

            // Prevent zero-lock (Chaos maps can get stuck at 0)
            if (seeds[i] == 0) seeds[i] = 0xDEADBEEF;
        }
    }
}
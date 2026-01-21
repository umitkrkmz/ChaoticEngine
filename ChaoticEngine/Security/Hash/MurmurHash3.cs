using System.Runtime.CompilerServices;

namespace ChaoticEngine.Security.Hash;

/// <summary>
/// Provides high-speed mixing constants and functions based on MurmurHash3 finalizer.
/// </summary>
public static class MurmurHash3
{
    // MurmurHash3 32-bit Finalizer Constants
    public const uint SCRAMBLER1 = 0x85EBCA6B;
    public const uint SCRAMBLER2 = 0xC2B2AE35;

    /// <summary>
    /// Applies a double-round avalanche mixing to the input state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Mix(uint k)
    {
        k *= SCRAMBLER1;
        k ^= k >> 16;
        k *= SCRAMBLER2;
        k ^= k >> 13;
        return k;
    }
}
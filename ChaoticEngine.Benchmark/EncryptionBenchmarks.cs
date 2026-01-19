using BenchmarkDotNet.Attributes;
using ChaoticEngine.Security;
using System.Security.Cryptography;

namespace ChaoticEngine.Benchmark;

[MemoryDiagnoser]
public class EncryptionBenchmarks
{
    private byte[] _data;
    private byte[] _key;
    private byte[] _iv;
    private byte[] _nonce; // For ChaCha20

    // Test different payload sizes: 4KB (Packet), 1MB (Frame)
    [Params(4096, 1_048_576)]
    public int PayloadSize;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[PayloadSize];
        _key = new byte[32];
        _iv = new byte[16];
        _nonce = new byte[12];

        Random.Shared.NextBytes(_data);
        Random.Shared.NextBytes(_key);
        Random.Shared.NextBytes(_iv);
        Random.Shared.NextBytes(_nonce);
    }

    // --- 1. OUR HERO: CHAOS CIPHER (v2.0) ---
    [Benchmark(Baseline = true)]
    public void Chaos_v2_Integer()
    {
        ChaosCipher.Process(_data, _key, _iv);
    }

    // --- 2. COMPETITOR: AES-256 (CBC) ---
    [Benchmark]
    public void Standard_AES()
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        encryptor.TransformBlock(_data, 0, _data.Length, _data, 0);
    }

    // --- 3. COMPETITOR: ChaCha20 ---
    [Benchmark]
    public void Google_ChaCha20()
    {
        using var chacha = new ChaCha20Poly1305(_key);
        byte[] tag = new byte[16];
        byte[] cipher = new byte[_data.Length]; // Allocation simulation

        chacha.Encrypt(_nonce, _data, cipher, tag, null);
    }
}
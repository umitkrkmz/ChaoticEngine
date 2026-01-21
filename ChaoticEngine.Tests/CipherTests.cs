using Xunit;
using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;
using ChaoticEngine.Security.Standard;
using System.Security.Cryptography;

namespace ChaoticEngine.Tests;

public class CipherTests
{
    // Test all 1D algorithms.
    [Theory]
    [InlineData(typeof(IntegerTentMap))]
    [InlineData(typeof(IntegerLogisticMap))]
    [InlineData(typeof(IntegerSineMap))]
    public void Cipher_EncryptDecrypt_ShouldReturnOriginalData_1D(Type primitiveType)
    {
        // Calling Generic Methods with Reflection (A practical way for testing)
        var method = typeof(CipherTests).GetMethod(nameof(GenericTest1D))!
            .MakeGenericMethod(primitiveType);
        method.Invoke(this, null);
    }

    // Test 3D Algorithms
    [Fact]
    public void Cipher_EncryptDecrypt_Lorenz_ShouldWork()
    {
        GenericTest3D<IntegerLorenz>();
    }

    [Fact]
    public void Cipher_EncryptDecrypt_Chen_ShouldWork()
    {
        GenericTest3D<IntegerChen>();
    }

    // Test 2D Algorithms
    [Fact]
    public void Cipher_EncryptDecrypt_Henon_ShouldWork()
    {
        GenericTest2D<IntegerHenonMap>();
    }

    // --- GENERIC TEST IMPLEMENTATIONS ---

    public void GenericTest1D<TPrimitive>() where TPrimitive : struct, IChaoticPrimitive
    {
        RunCycleTest((d, k, i) => ChaosCipher<TPrimitive>.Process(d, k, i));
    }

    public void GenericTest2D<TPrimitive>() where TPrimitive : struct, IChaoticPrimitive2D
    {
        RunCycleTest((d, k, i) => ChaosCipher2D<TPrimitive>.Process(d, k, i));
    }

    public void GenericTest3D<TPrimitive>() where TPrimitive : struct, IChaoticPrimitive3D
    {
        RunCycleTest((d, k, i) => ChaosCipher3D<TPrimitive>.Process(d, k, i));
    }

    private void RunCycleTest(Action<Span<byte>, byte[], byte[]> processAction)
    {
        // Arrange
        byte[] original = new byte[1024 * 1024]; // 1MB Test Data
        RandomNumberGenerator.Fill(original);

        byte[] buffer = new byte[original.Length];
        Array.Copy(original, buffer, original.Length);

        byte[] key = new byte[32];
        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(iv);

        // Act 1: Encrypt
        processAction(buffer, key, iv);

        // Assert 1: Data should be changed
        Assert.NotEqual(original, buffer);

        // Act 2: Decrypt (Symmetric Stream Cipher: Encrypt again to decrypt)
        processAction(buffer, key, iv);

        // Assert 2: Data should be restored
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void ChaosStream_ShouldBeCompatibleWithMemoryStream()
    {
        byte[] data = new byte[1000];
        new Random().NextBytes(data);
        byte[] key = new byte[32];
        byte[] iv = new byte[16];

        // Write Encrypted
        using var ms = new MemoryStream();
        using (var cs = new ChaosStream<IntegerTentMap>(ms, key, iv))
        {
            cs.Write(data, 0, data.Length);
        }
        byte[] encrypted = ms.ToArray();

        // Read Decrypted
        using var msRead = new MemoryStream(encrypted);
        using (var csRead = new ChaosStream<IntegerTentMap>(msRead, key, iv))
        {
            byte[] decrypted = new byte[data.Length];
            int read = csRead.Read(decrypted, 0, decrypted.Length);

            Assert.Equal(data.Length, read);
            Assert.Equal(data, decrypted);
        }
    }
}
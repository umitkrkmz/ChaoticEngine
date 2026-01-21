// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;

namespace ChaoticEngine.Security.Standard;

/// <summary>
/// A Stream wrapper that applies Chaotic Encryption/Decryption on the fly.
/// <br/>Supports random access (Seeking) via Counter Mode (CTR) keystream generation.
/// </summary>
/// <typeparam name="TPrimitive">The chaotic algorithm to use (e.g., IntegerTentMap).</typeparam>
public class ChaosStream<TPrimitive> : Stream
    where TPrimitive : struct, IChaoticPrimitive
{
    private readonly Stream _baseStream;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private long _position;

    // Keystream Buffering for Block Operations
    private readonly byte[] _keystreamBuffer = new byte[4096];
    private long _keystreamBlockIndex = -1;

    /// <summary>
    /// Initializes a new instance of the ChaosStream class.
    /// </summary>
    /// <param name="baseStream">The underlying stream to read from or write to.</param>
    /// <param name="key">The 256-bit secret key.</param>
    /// <param name="iv">The 128-bit initialization vector.</param>
    public ChaosStream(Stream baseStream, byte[] key, byte[] iv)
    {
        _baseStream = baseStream;
        _key = key;
        _iv = iv;
        _position = 0;
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanWrite => _baseStream.CanWrite;
    public override bool CanSeek => _baseStream.CanSeek;
    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _position;
        set
        {
            if (_baseStream.CanSeek) _baseStream.Position = value;
            _position = value;
        }
    }

    /// <summary>
    /// Reads a sequence of bytes from the current stream and advances the position within the stream.
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _baseStream.Read(buffer, offset, count);
        if (read == 0) return 0;

        ApplyChaosXor(buffer, offset, read, _position);
        _position += read;

        return read;
    }

    /// <summary>
    /// Writes a sequence of bytes to the current stream and advances the current position within this stream.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        // Copy buffer to avoid modifying the original data (Encrypt-then-Write)
        byte[] temp = new byte[count];
        Buffer.BlockCopy(buffer, offset, temp, 0, count);

        ApplyChaosXor(temp, 0, count, _position);
        _baseStream.Write(temp, 0, count);

        _position += count;
    }

    /// <summary>
    /// Generates chaos keystream for the specific file position and applies XOR.
    /// This enables Random Access (Seek) support by calculating the block index.
    /// </summary>
    private void ApplyChaosXor(byte[] buffer, int offset, int count, long streamPos)
    {
        int end = offset + count;
        int current = offset;
        long currentPos = streamPos;

        while (current < end)
        {
            // Determine which 4KB block we are in
            long blockIndex = currentPos / _keystreamBuffer.Length;
            int offsetInBlock = (int)(currentPos % _keystreamBuffer.Length);

            // Generate new keystream block if we moved to a new block
            if (blockIndex != _keystreamBlockIndex)
            {
                GenerateKeystreamBlock(blockIndex);
            }

            int remainingInBlock = _keystreamBuffer.Length - offsetInBlock;
            int toProcess = Math.Min(end - current, remainingInBlock);

            // Apply Stream Cipher (XOR)
            for (int i = 0; i < toProcess; i++)
            {
                buffer[current + i] ^= _keystreamBuffer[offsetInBlock + i];
            }

            current += toProcess;
            currentPos += toProcess;
        }
    }

    // Deterministically generates the keystream for a specific block index
    private void GenerateKeystreamBlock(long blockIndex)
    {
        // Counter Mode (CTR):
        // We modify the IV based on the block index so each block has a unique keystream.
        byte[] counterIv = new byte[_iv.Length];
        Array.Copy(_iv, counterIv, _iv.Length);

        // XOR the block index into the first 8 bytes of the IV
        ulong counter = (ulong)blockIndex;
        for (int i = 0; i < 8; i++)
        {
            counterIv[i] ^= (byte)(counter >> (i * 8));
        }

        // Fill buffer with chaos
        Array.Clear(_keystreamBuffer, 0, _keystreamBuffer.Length);
        ChaosCipher<TPrimitive>.Process(_keystreamBuffer, _key, counterIv);

        _keystreamBlockIndex = blockIndex;
    }

    public override void Flush() => _baseStream.Flush();

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = _baseStream.Seek(offset, origin);
        _position = newPos;
        return newPos;
    }

    public override void SetLength(long value) => _baseStream.SetLength(value);
}
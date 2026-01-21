// Copyright (c) 2026 ChaoticEngine Contributors. Licensed under the MIT License.

using ChaoticEngine.Security.Cipher;
using ChaoticEngine.Security.Primitives;

namespace ChaoticEngine.Security.Standard;

/// <summary>
/// Specialized Stream wrapper for 2D Chaotic Systems (e.g. Henon Map).
/// </summary>
public class ChaosStream2D<TPrimitive> : Stream
    where TPrimitive : struct, IChaoticPrimitive2D
{
    private readonly Stream _baseStream;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private long _position;
    private readonly byte[] _keystreamBuffer = new byte[4096];
    private long _keystreamBlockIndex = -1;

    public ChaosStream2D(Stream baseStream, byte[] key, byte[] iv)
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
        set { if (_baseStream.CanSeek) _baseStream.Position = value; _position = value; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _baseStream.Read(buffer, offset, count);
        if (read == 0) return 0;
        ApplyChaosXor(buffer, offset, read, _position);
        _position += read;
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        byte[] temp = new byte[count];
        Buffer.BlockCopy(buffer, offset, temp, 0, count);
        ApplyChaosXor(temp, 0, count, _position);
        _baseStream.Write(temp, 0, count);
        _position += count;
    }

    private void ApplyChaosXor(byte[] buffer, int offset, int count, long streamPos)
    {
        int end = offset + count;
        int current = offset;
        long currentPos = streamPos;

        while (current < end)
        {
            long blockIndex = currentPos / _keystreamBuffer.Length;
            int offsetInBlock = (int)(currentPos % _keystreamBuffer.Length);

            if (blockIndex != _keystreamBlockIndex) GenerateKeystreamBlock(blockIndex);

            int remainingInBlock = _keystreamBuffer.Length - offsetInBlock;
            int toProcess = Math.Min(end - current, remainingInBlock);

            for (int i = 0; i < toProcess; i++)
                buffer[current + i] ^= _keystreamBuffer[offsetInBlock + i];

            current += toProcess;
            currentPos += toProcess;
        }
    }

    private void GenerateKeystreamBlock(long blockIndex)
    {
        byte[] counterIv = new byte[_iv.Length];
        Array.Copy(_iv, counterIv, _iv.Length);
        ulong counter = (ulong)blockIndex;
        for (int i = 0; i < 8; i++) counterIv[i] ^= (byte)(counter >> (i * 8));

        Array.Clear(_keystreamBuffer, 0, _keystreamBuffer.Length);
        // Calls 2D Cipher Engine
        ChaosCipher2D<TPrimitive>.Process(_keystreamBuffer, _key, counterIv);
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
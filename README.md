<div align="center">

# 🌌 ChaoticEngine v2.0
### The Hybrid Chaos Framework: Scientific Simulation & High-Performance Encryption

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![SIMD](https://img.shields.io/badge/Hardware_Accel-AVX2_%2F_AVX--512-blueviolet?style=for-the-badge)](https://en.wikipedia.org/wiki/Advanced_Vector_Extensions)
[![Security](https://img.shields.io/badge/Security-Zero--Allocation-red?style=for-the-badge)]()
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/badge/Release-v2.0.0-blue?style=for-the-badge&logo=github)](https://github.com/umitkrkmz/ChaoticEngine/releases)

<p align="center">
  <b>ChaoticEngine</b> is a dual-purpose library for .NET 10.<br>
  It features a <b>Scientific Edition</b> for high-precision double-floating point simulations and a new <b>Security Edition</b> for ultra-fast, integer-based real-time encryption.
</p>

[Security Benchmarks](#-security-benchmarks-v20) •
[Scientific Benchmarks](#-scientific-benchmarks-v10) •
[Methodology](#-benchmark-methodology) •
[Installation](#-installation) •
[Usage](#-usage)

</div>

---

## 🔥 What's New in v2.0?

Version 2.0 introduces the **ChaosCipher** engine, designed for high-throughput scenarios, capable of handling 4K/1080p Video Streaming workloads with ease.

* **🚀 Integer Arithmetic Core:** Replaced floating-point math with bitwise integer operations (XOR, Shift, Rotate) to bypass FPU bottlenecks.
* **💉 SIMD Injection:** Uses `Vector256<uint>` (AVX2) to process **32 bytes** of data in a single CPU cycle.
* **🚫 Zero-Allocation Architecture:** The encryption engine operates **In-Place** using `Span<T>` and `pointers`. It generates **0 Bytes** of Garbage Collection (GC) pressure, ensuring smooth video playback without micro-stutters.

---

## 🛡️ Security Benchmarks (v2.0)

We compared **ChaosLink (ChaosCipher)** against the industry standard **AES-256 (AES-NI accelerated)** and Google's **ChaCha20**.

| Algorithm | Type | Time (Mean) | Speed (approx) | Allocation | GC Pressure |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **AES-256 (CBC)** | Block Cipher | 937.9 µs | *Reference* | 528 B | Low |
| **ChaCha20** | Stream Cipher | 2,702.6 µs | 0.3x (Slower) | 1,048 KB | **High** |
| **ChaosCipher v2** | **Stream Cipher** | **31.3 µs** | **🚀 30x Faster** | **0 B** | **None** |

> **Analysis:** Even with hardware-accelerated AES-NI, standard AES is ~30x slower than ChaosCipher. ChaCha20 suffers from high memory allocation (~1MB per frame), causing GC spikes. **Based on the 1MB payload test, ChaosCipher demonstrates a theoretical throughput exceeding 30 GB/s, making it ideal for real-time systems where standard algorithms introduce latency.**

---

## 🔬 Scientific Benchmarks (v1.0)

For researchers requiring double-precision accuracy (e.g., for Phase Space Analysis or Butterfly Effect simulations), the Scientific Engine leverages AVX-512 optimizations.

| Algorithm | Type | Standard Scalar (ms) | ChaoticEngine AVX (ms) | Speedup |
| :--- | :---: | :---: | :---: | :---: |
| **Sine Map** | 1D | 19.97 ms | **2.01 ms** | **9.9x** |
| **Tent Map** | 1D | 5.09 ms | **0.66 ms** | **7.7x** |
| **Logistic Map** | 1D | 2.05 ms | **0.80 ms** | **2.6x** |
| **Lorenz System** | 3D | 4.70 ms | **3.02 ms** | **1.6x** |

---

## 📏 Benchmark Methodology

Transparency is key to scientific validity. All results were obtained using **BenchmarkDotNet v0.15.8**, the industry-standard tool for .NET performance tracking.

* **Hardware:** Intel Core i7-9750H @ 2.60GHz (12 Logical Cores).
* **Environment:** .NET 10.0 SDK, Windows 11 (x64), Release Build.
* **Test Conditions:**
    * **Payload:** 1 MB (1,048,576 bytes) random binary data (representing a large video frame).
    * **Warmup:** 10+ iterations per method to stabilize JIT compiler.
    * **Measurement:** Arithmetic Mean of 100+ iterations.
    * **Memory:** Measured using `MemoryDiagnoser` to track Gen0/Gen1/Gen2 GC collections.
* **Verification:** AES and ChaCha20 implementations use the standard `System.Security.Cryptography` libraries for fair comparison.


---

## 📦 Installation

### Option 1: NuGet (Local / Manual)
Since this is a research-grade library, you can download the latest `.nupkg` file from the **[Releases](https://github.com/umitkrkmz/ChaoticEngine/releases)** page.

1. Download `ChaoticEngine.2.0.0.nupkg`.
2. Add it to your local NuGet source or install directly via CLI:

```bash
dotnet add package ChaoticEngine --source "C:\Path\To\Your\LocalPackages"
```

### Option 2: Source Code
This library is designed for **.NET 10.**
```bash
git clone https://github.com/umitkrkmz/ChaoticEngine.git
```

### Enable Unsafe Blocks
Since v2.0 uses high-performance pointer arithmetic, you must enable `unsafe` blocks in your consuming project's `.csproj` file:

```xml
<PropertyGroup>
   <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

---

## 💻 Usage
**1. Security Edition (Encryption)**\
Encrypt a byte array (or video frame) in-place with zero allocation.

```csharp
using ChaoticEngine.Security;

// Inputs
byte[] data = GetVideoFrame(); // e.g., 1MB buffer
byte[] key  = new byte[32];    // Shared Secret
byte[] iv   = new byte[16];    // Random Salt

// Encrypt (In-Place) - Super Fast!
ChaosCipher.Process(data, key, iv);

// Send 'data' over network...

// Decrypt (Same operation)
ChaosCipher.Process(data, key, iv);
```

**2. Scientific Edition (Simulation)**\
Perfect for verifying Steganography or Image Encryption results.

```csharp
using ChaoticEngine.Core;

// Create a 3D Lorenz Generator
var engine = ChaosFactory.Create3D(ChaosType.LorenzSystem);

// Buffers for X, Y, Z coordinates
double[] x = new double[1000];
double[] y = new double[1000];
double[] z = new double[1000];

// Generate
engine.Generate(x, y, z, x0: 0.1, y0: 0.1, z0: 0.1);
```

---

## 🧠 Supported Algorithms

| Type     | Algorithm              | Chaos Characteristics                 | Best Use Case                  |
|----------|------------------------|---------------------------------------|--------------------------------|
| **1D**   | **🌪️Logistic Map**     | Polynomial, Population dynamics       | Fast PRNG, Basic Encryption    |
| **1D**   | **⛺Tent Map**         | Piecewise Linear                      | High-Speed Stream Ciphers      |
| **1D**   | **〰️Sine Map**          | Trigonometric (Highly Non-linear)     | **Ultra-Fast** (via SIMD Hack) |
| **2D**   | **🌀Henon Map**        | Quadratic, Strange Attractor          | Image Encryption, Data Hiding  |
| **3D**   | **🦋Lorenz System**    | Differential, Butterfly Effect        | Modeling, Key Generation       |
| **3D**   | **🐉Chen System**      | Differential, Double Scroll           | High-Sensitivity Crypto        |

---

## 📄 License
Distributed under the MIT License. See [`LICENSE`](LICENSE) for more information.
<div align="center">

# 🌌 ChaoticEngine
### High-Performance Chaos Theory & Security Library for .NET 10

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![SIMD](https://img.shields.io/badge/Hardware_Accel-AVX2_%2F_AVX--512-blueviolet?style=for-the-badge)](https://en.wikipedia.org/wiki/Advanced_Vector_Extensions)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Release](https://img.shields.io/badge/Release-v1.0.0-blue?style=for-the-badge&logo=github)](https://github.com/umitkrkmz/ChaoticEngine/releases)
[![Status](https://img.shields.io/badge/Status-Stable-success?style=for-the-badge)](https://github.com/umitkrkmz/ChaoticEngine)

<p align="center">
  <b>ChaoticEngine</b> is a research-grade library designed for <b>Cryptography</b>, <b>Steganography</b>, and <b>High-Entropy Simulations</b>.<br>
  It leverages modern .NET 10 intrinsics to unlock the full potential of your CPU using <b>AVX-512</b> and <b>AVX2</b> instruction sets.
</p>

[Features](#-key-features) •
[Benchmarks](#-performance-benchmarks) •
[Installation](#-installation) •
[Usage](#-usage) •
[Algorithms](#-supported-algorithms)

</div>

---

## 🚀 Key Features

### ⚡ Adaptive Hardware Acceleration
The engine automatically detects CPU capabilities at runtime and selects the fastest execution path without user intervention:
* **Gear 1: AVX-512 (512-bit)** - Processes **8** independent chaotic streams per cycle.
* **Gear 2: AVX2 (256-bit)** - Processes **4** independent chaotic streams per cycle.
* **Gear 3: Scalar Fallback** - Universal compatibility for older hardware.

### 🧮 Advanced Optimization Techniques
* **Interleaved Multi-State Simulation:** Overcomes the sequential dependency of chaos equations ($x_{n+1} = f(x_n)$) by simulating multiple independent "universes" in parallel vector lanes.
* **Branchless Logic:** Uses SIMD masking and blending instead of `if-else` branches (e.g., in Tent Map) to prevent CPU pipeline stalls.
* **Bhaskara Approximation:** Uses optimized algebraic approximations for trigonometric functions in SIMD (Sine Map) to avoid costly `Math.Sin` calls.

### 🛠️ Core Capabilities
* **🧬 Comprehensive Algorithm Suite:** Implements 6 distinct chaotic algorithms (1D Maps, 2D Attractors, 3D Differential Systems).
* **🛡️ Cryptographic Quality:** Validated high Shannon Entropy. Suitable for Stream Ciphers and PRNGs.
* **🕵️ Steganography Tools:** Built-in utilities for **LSB (Least Significant Bit)** data hiding with **Zero-Loss (MSE: 0.0)** recovery capability.
* **📊 Analysis Module:** Includes `QualityMetrics` for MSE, PSNR, RMSE, NPCR, and Entropy calculations.
* **🏭 Zero-Allocation Architecture:** Validated with **BenchmarkDotNet**. The core engine produces **0 Bytes** of garbage per generation cycle using `Span<T>`.
---

## 🏎️ Performance Benchmarks

Performance tests were conducted using **BenchmarkDotNet** (the industry standard for .NET performance benchmarking) to ensure scientific accuracy.

**System Specs:** Intel Core i7-9750H | .NET 10 | AVX2 Mode Active  
**Dataset:** 1 Million Samples (Double Precision)

| Algorithm | Type | Standard Scalar (ms) | ChaoticEngine AVX (ms) | Speedup (Approx) | Allocation |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Sine Map** | 1D | 19.97 ms | **2.01 ms** | **🚀 9.9x** | 0 B |
| **Tent Map** | 1D | 5.09 ms | **0.66 ms** | **🔥 7.7x** | 0 B |
| **Logistic Map** | 1D | 2.05 ms | **0.80 ms** | **⚡ 2.6x** | 0 B |
| **Henon Map** | 2D | 4.08 ms | **1.50 ms** | **⚡ 2.7x** | 0 B |
| **Lorenz System** | 3D | 4.70 ms | **3.02 ms** | **⏩ 1.6x** | 0 B |
| **Chen System** | 3D | 5.02 ms | **2.94 ms** | **⏩ 1.7x** | 0 B |

### 🏆 Why is it so fast?
1.  **Sine Map (~10x Speedup):** Replaces the standard `Math.Sin` (which is slow) with a high-precision **Bhaskara I Approximation** implemented in AVX intrinsics.
2.  **Tent Map (~7.7x Speedup):** Uses **Branchless Programming**. Instead of CPU-expensive `if-else` checks, we use SIMD masking/blending instructions.
3.  **Memory Efficiency:** As shown in the "Allocation" column, the generation loop allocates **0 Bytes** of managed memory, preventing Garbage Collector pauses during high-frequency simulations.

---

## 📦 Installation

### Option 1: NuGet (Local / Manual)
Since this is a research-grade library, you can download the latest `.nupkg` file from the **[Releases](https://github.com/umitkrkmz/ChaoticEngine/releases)** page.

1. Download `ChaoticEngine.1.0.0.nupkg`.
2. Add it to your local NuGet source or install directly via CLI:

```bash
dotnet add package ChaoticEngine --source "C:\Path\To\Your\LocalPackages"
```

### Option 2: Source Code
This library is designed for **.NET 10.**
```bash
git clone https://github.com/umitkrkmz/ChaoticEngine.git
```

---

## 💻 Usage
**1. Generating Chaos (The Factory Pattern)**\
You don't need to worry about hardware support; the factory handles it.

```csharp
using ChaoticEngine.Core;

// Create a generator (Auto-detects AVX-512/AVX2)
var engine = ChaosFactory.Create1D(ChaosType.SineMap);

// Generate 1 Million chaotic numbers
double[] buffer = new double[1_000_000];
engine.Generate(buffer, initialCondition: 0.5);
```

**2. Signal Analysis & Quality Control**\
Perfect for verifying Steganography or Image Encryption results.

```csharp
using ChaoticEngine.Analysis;

double[] original = LoadAudio("cover.wav");
double[] stego    = LoadAudio("stego.wav");

// Calculate Imperceptibility (PSNR)
double mse  = QualityMetrics.CalculateMse(original, stego);
double psnr = QualityMetrics.CalculatePsnr(mse, maxValue: 2.0);

Console.WriteLine($"PSNR: {psnr} dB"); // > 60 dB is invisible to human perception
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
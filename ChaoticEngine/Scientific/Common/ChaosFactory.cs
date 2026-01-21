using ChaoticEngine.Scientific.Generators;

namespace ChaoticEngine.Scientific.Common;

/// <summary>
/// A static factory to instantiate high-performance SIMD-accelerated chaotic generators.
/// Use this factory to easily create 1D, 2D, or 3D engines with standard scientific parameters.
/// </summary>
public static class ChaosFactory
{
    /// <summary>
    /// Creates a 1D chaotic map generator.
    /// </summary>
    /// <param name="type">The type of 1D map (Logistic, Tent, Sine).</param>
    /// <returns>An instance implementing <see cref="IChaoticGenerator1D"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if a multi-dimensional type is passed.</exception>
    public static IChaoticGenerator1D Create1D(ChaosType type)
    {
        return type switch
        {
            ChaosType.LogisticMap => new LogisticGenerator { GrowthRate = 3.99 }, // Chaos region > 3.57
            ChaosType.TentMap => new TentGenerator { Mu = 1.9999 },               // Full chaos near 2.0
            ChaosType.SineMap => new SineGenerator { R = 4.0 },                   // Full chaos at 4.0
            _ => throw new ArgumentException($"The selected type '{type}' is not a supported 1D algorithm.")
        };
    }

    /// <summary>
    /// Creates a 2D chaotic map generator.
    /// </summary>
    /// <param name="type">The type of 2D map (e.g., Henon).</param>
    /// <returns>An instance implementing <see cref="IChaoticGenerator2D"/>.</returns>
    public static IChaoticGenerator2D Create2D(ChaosType type)
    {
        return type switch
        {
            // Standard Henon parameters: a=1.4, b=0.3
            ChaosType.HenonMap => new HenonGenerator { A = 1.4, B = 0.3 },
            _ => throw new ArgumentException($"The selected type '{type}' is not a supported 2D algorithm.")
        };
    }

    /// <summary>
    /// Creates a 3D chaotic system generator (Differential Equations).
    /// </summary>
    /// <param name="type">The type of 3D system (Lorenz, Chen).</param>
    /// <returns>An instance implementing <see cref="IChaoticGenerator3D"/>.</returns>
    public static IChaoticGenerator3D Create3D(ChaosType type)
    {
        return type switch
        {
            // Lorenz: Sigma=10 (Prandtl), Rho=28 (Rayleigh), Beta=8/3 (Geometric)
            ChaosType.LorenzSystem => new LorenzGenerator { Sigma = 10, Rho = 28, Beta = 8.0 / 3.0, Dt = 0.01 },

            // Chen: a=35, b=3, c=28 (Standard chaotic attractor)
            ChaosType.ChenSystem => new ChenGenerator { A = 35, B = 3, C = 28, Dt = 0.005 },

            _ => throw new ArgumentException($"The selected type '{type}' is not a supported 3D algorithm.")
        };
    }
}
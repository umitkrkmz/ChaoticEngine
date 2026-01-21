namespace ChaoticEngine.Scientific.Common;

/// <summary>
/// Represents a 1-Dimensional chaotic map generator (e.g., Logistic, Tent, Sine).
/// Generates a single stream of double-precision values.
/// </summary>
public interface IChaoticGenerator1D
{
    /// <summary>
    /// Generates the chaotic sequence and fills the provided buffer.
    /// </summary>
    /// <param name="buffer">The destination span to fill with chaotic values. Length determines the number of iterations.</param>
    /// <param name="x0">The initial condition (seed). Must be between 0.0 and 1.0 for most maps.</param>
    void Generate(Span<double> buffer, double x0);
}

/// <summary>
/// Represents a 2-Dimensional chaotic map generator (e.g., Hénon Map).
/// Generates two coupled streams (X and Y).
/// </summary>
public interface IChaoticGenerator2D
{
    /// <summary>
    /// Generates coupled chaotic sequences for X and Y coordinates.
    /// </summary>
    /// <param name="xBuf">Buffer for the X-coordinate sequence.</param>
    /// <param name="yBuf">Buffer for the Y-coordinate sequence.</param>
    /// <param name="x0">Initial X position.</param>
    /// <param name="y0">Initial Y position.</param>
    /// <exception cref="ArgumentException">Thrown if buffer lengths are not equal.</exception>
    void Generate(Span<double> xBuf, Span<double> yBuf, double x0, double y0);
}

/// <summary>
/// Represents a 3-Dimensional continuous chaotic system (e.g., Lorenz, Chen).
/// Generates three coupled streams (X, Y, Z) usually via differential equation integration (Euler/RK4).
/// </summary>
public interface IChaoticGenerator3D
{
    /// <summary>
    /// Generates coupled chaotic trajectories for X, Y, and Z coordinates.
    /// </summary>
    /// <param name="xBuf">Buffer for the X-coordinate trajectory.</param>
    /// <param name="yBuf">Buffer for the Y-coordinate trajectory.</param>
    /// <param name="zBuf">Buffer for the Z-coordinate trajectory.</param>
    /// <param name="x0">Initial X position.</param>
    /// <param name="y0">Initial Y position.</param>
    /// <param name="z0">Initial Z position.</param>
    /// <exception cref="ArgumentException">Thrown if buffer lengths are not equal.</exception>
    void Generate(Span<double> xBuf, Span<double> yBuf, Span<double> zBuf, double x0, double y0, double z0);
}
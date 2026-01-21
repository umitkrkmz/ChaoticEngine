namespace ChaoticEngine.Scientific.Common;

/// <summary>
/// Defines the supported chaotic maps and systems available in the Scientific Engine.
/// </summary>
public enum ChaosType
{
    // --- 1D Algorithms (Time Series) ---

    /// <summary>
    /// <b>Logistic Map:</b> A polynomial mapping of degree 2.
    /// <br/>Classic example of how complex, chaotic behaviour can arise from very simple non-linear dynamical equations.
    /// <br/><i>Equation: x = r * x * (1 - x)</i>
    /// </summary>
    LogisticMap,

    /// <summary>
    /// <b>Tent Map:</b> A piecewise linear, continuous map.
    /// <br/>Optimized for high-speed generation due to simple arithmetic operations.
    /// <br/><i>Equation: x = min(x, 1-x) * mu</i>
    /// </summary>
    TentMap,

    /// <summary>
    /// <b>Sine Map:</b> A unimodal map based on the sine function.
    /// <br/>Offers highly non-linear behavior similar to Logistic map but with trigonometric properties.
    /// <br/><i>Equation: x = r * sin(pi * x)</i>
    /// </summary>
    SineMap,

    // --- 2D Algorithms (Phase Space) ---

    /// <summary>
    /// <b>Hénon Map:</b> A discrete-time dynamical system that exhibits a strange attractor.
    /// <br/>Maps a point (x, y) in the plane to a new point.
    /// <br/><i>Ideal for Image Encryption and 2D scattering.</i>
    /// </summary>
    HenonMap,

    // --- 3D Algorithms (Differential Systems) ---

    /// <summary>
    /// <b>Lorenz System:</b> A system of ordinary differential equations (ODE).
    /// <br/>Famous for the "Butterfly Effect". Models atmospheric convection.
    /// <br/><i>Features: 3D Trajectories, Continuous Chaos.</i>
    /// </summary>
    LorenzSystem,

    /// <summary>
    /// <b>Chen System:</b> A dynamical system similar to Lorenz but with more complex topological structure (Double Scroll).
    /// <br/>Often used in secure communications due to higher sensitivity.
    /// </summary>
    ChenSystem
}
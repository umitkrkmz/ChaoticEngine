namespace ChaoticEngine.Core;

// Interface for 1D Chaotic Maps (Logistic, Tent, Sine)
public interface IChaoticGenerator1D
{
    void Generate(Span<double> buffer, double x0);
}

// Interface for 2D Chaotic Maps (Henon)
public interface IChaoticGenerator2D
{
    void Generate(Span<double> xBuf, Span<double> yBuf, double x0, double y0);
}

// Interface for 3D Chaotic Systems (Lorenz, Chen)
public interface IChaoticGenerator3D
{
    void Generate(Span<double> xBuf, Span<double> yBuf, Span<double> zBuf, double x0, double y0, double z0);
}
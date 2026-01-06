namespace ChaoticEngine.Core;

public static class ChaosFactory
{
    public static IChaoticGenerator1D Create1D(ChaosType type)
    {
        return type switch
        {
            ChaosType.LogisticMap => new LogisticGenerator { GrowthRate = 3.99 },
            ChaosType.TentMap => new TentGenerator { Mu = 1.9999 },
            ChaosType.SineMap => new SineGenerator { R = 4.0 },
            _ => throw new ArgumentException("Selected type is not a supported 1D algorithm.")
        };
    }

    public static IChaoticGenerator2D Create2D(ChaosType type)
    {
        return type switch
        {
            ChaosType.HenonMap => new HenonGenerator { A = 1.4, B = 0.3 },
            _ => throw new ArgumentException("Selected type is not a supported 2D algorithm.")
        };
    }

    public static IChaoticGenerator3D Create3D(ChaosType type)
    {
        return type switch
        {
            ChaosType.LorenzSystem => new LorenzGenerator { Sigma = 10, Rho = 28, Beta = 8.0 / 3.0, Dt = 0.01 },
            ChaosType.ChenSystem => new ChenGenerator { A = 35, B = 3, C = 28, Dt = 0.005 },
            _ => throw new ArgumentException("Selected type is not a supported 3D algorithm.")
        };
    }
}
using ChaoticEngine.Analysis;              // ImageMetrics
using ChaoticEngine.Scientific.Common;
using ChaoticEngine.Scientific.Generators; // Assuming this exists from Scientific module
using ScottPlot;

namespace ChaoticEngine.Viz;

public static class Plotters
{
    // Helper to get the absolute path to the Desktop (bypassing OneDrive sync folders if possible)
    private static string GetOutputDirectory()
    {
        // Target: C:\Users\YourName\Desktop\Chaotic_Output
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string desktopPath = Path.Combine(userProfile, "Desktop", "Chaotic_Output");

        if (!Directory.Exists(desktopPath))
        {
            Directory.CreateDirectory(desktopPath);
        }
        return desktopPath;
    }

    /// <summary>
    /// Renders Scientific 3D Attractors (Lorenz, Chen) to verify the Butterfly Effect.
    /// </summary>
    public static void PlotAttractor3D(string title, IChaoticGenerator3D generator, int points = 50000)
    {
        Console.Write($"[-] Generating Plot: {title}... ");

        // 1. Generate Data (Using Scientific Engine)
        double[] x = new double[points];
        double[] y = new double[points];
        double[] z = new double[points];

        // Start from a slight offset (0.1, 0.1, 0.1)
        generator.Generate(x, y, z, 0.1, 0.1, 0.1);

        // 2. Setup Plot
        var plt = new Plot();
        plt.Title(title);
        plt.XLabel("X Axis");
        plt.YLabel("Z Axis"); // X-Z plane usually shows the best butterfly shape

        // Create Scatter Plot (Point Cloud)
        var sp = plt.Add.Scatter(x, z);
        sp.MarkerSize = 1;
        sp.Color = Colors.BlueViolet.WithOpacity(0.5); // Aesthetic transparency
        sp.LineWidth = 0; // No connecting lines, just points

        // 3. Save to Desktop
        string fileName = $"{title.Replace(" ", "_")}.png";
        string fullPath = Path.Combine(GetOutputDirectory(), fileName);

        plt.SavePng(fullPath, 800, 600);
        Console.WriteLine($"DONE.\n    Saved to: {fullPath}");
    }

    /// <summary>
    /// Renders Encryption Histograms to verify cryptographic security (Uniform Distribution).
    /// </summary>
    public static void PlotSecurityHistogram(string title, byte[] data)
    {
        Console.Write($"[-] Generating Histogram: {title}... ");

        // 1. Calculate Histogram (Using Security Engine -> ImageMetrics)
        long[] hist = ImageMetrics.CalculateHistogram(data);

        // Convert to ScottPlot format
        double[] values = Array.ConvertAll(hist, item => (double)item);
        double[] positions = Enumerable.Range(0, 256).Select(x => (double)x).ToArray();

        // 2. Setup Plot
        var plt = new Plot();
        plt.Title($"Security Check: {title}");
        plt.XLabel("Byte Value (0-255)");
        plt.YLabel("Frequency");

        // Add Bars
        var bar = plt.Add.Bars(positions, values);
        bar.Color = title.Contains("Raw") ? Colors.Gray : Colors.Red; // Gray for raw, Red for encrypted

        // Ideal Distribution Line (Expected Mean)
        double expected = data.Length / 256.0;
        var line = plt.Add.HorizontalLine(expected);
        line.Color = Colors.Green;
        line.LinePattern = LinePattern.Dashed;
        line.LineWidth = 2;
        line.LabelText = "Ideal Uniformity";

        // 3. Save to Desktop
        string fileName = $"Hist_{title.Replace(" ", "_")}.png";
        string fullPath = Path.Combine(GetOutputDirectory(), fileName);

        plt.SavePng(fullPath, 800, 400);
        Console.WriteLine($"DONE.\n    Saved to: {fullPath}");
    }
}
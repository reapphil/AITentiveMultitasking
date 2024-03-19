
using System;

/// <summary>
/// A Gaussian or normal distribution.
/// </summary>
public class NormalDistribution
{
    public double Mean { get; private set; }

    public double Variance { get; private set; }

    public double Sigma { get; private set; }


    private bool _useLast;

    private double _y2;


    public NormalDistribution(double mean, double sigma)
    {
        Mean = mean;
        Sigma = sigma;
        Variance = sigma * sigma;
    }


    /// <summary>
    /// Sample a value from distribution for a given random variable.
    /// </summary>
    /// <param name="rnd">Generator for a random variable between 0-1 (inclusive)</param>
    /// <returns>A value from the distribution</returns>
    public double Sample(System.Random rnd)
    {
        double x1, x2, w, y1;

        if (_useLast)
        {
            y1 = _y2;
            _useLast = false;
        }
        else
        {
            do
            {
                x1 = 2.0 * rnd.NextDouble() - 1.0;
                x2 = 2.0 * rnd.NextDouble() - 1.0;
                w = x1 * x1 + x2 * x2;
            }
            while (w >= 1.0);

            w = Math.Sqrt(-2.0 * Math.Log(w) / w);
            y1 = x1 * w;
            _y2 = x2 * w;
            _useLast = true;
        }

        return Mean + y1 * Sigma;
    }
}
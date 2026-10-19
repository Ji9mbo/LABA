using System;
using System.Collections.Generic;

namespace AtmosphereCalc.Models;

public class AtmosphereInput
{
    public double Altitude { get; set; }
}

public class AtmosphereDataResult
{
    public double Altitude { get; set; }
    public double Pressure { get; set; }
    public double Density { get; set; }
    public double TemperatureC { get; set; }
    public double TemperatureK { get; set; }
}

public class AtmosphereCalculator
{
    private const double MaxAltitude = 20000.0;

    public AtmosphereDataResult Calculate(double h)
    {
        const double P0 = 101325.0, T0 = 288.15, g = 9.80665;
        const double L = 0.0065, R = 8.31446, M = 0.0289644, Rs = R / M;
        double pressure, density, temperature;

        if (h < 11000)
        {
            temperature = T0 - L * h;
            pressure = P0 * Math.Pow(1.0 - (L * h / T0), (g * M) / (R * L));
        }
        else
        {
            temperature = 216.65;
            double P11 = 22632.1;
            pressure = P11 * Math.Exp(-g * M * (h - 11000) / (R * temperature));
        }

        density = pressure / (Rs * temperature);

        return new AtmosphereDataResult
        {
            Altitude = h,
            Pressure = Math.Round(pressure, 2),
            Density = Math.Round(density, 4),
            TemperatureC = Math.Round(temperature - 273.15, 2),
            TemperatureK = Math.Round(temperature, 2)
        };
    }
}
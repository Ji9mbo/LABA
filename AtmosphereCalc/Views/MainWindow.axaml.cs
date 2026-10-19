using AtmosphereCalc.Controllers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.Views;

public partial class MainWindow : Window
{
    private const double MaxAltitude = 20000.0;
    private const double MaxPressure = 101325.0;
    private bool _isUpdating = false;
    private readonly AtmosphereController _controller = new();

    public MainWindow()
    {
        InitializeComponent();

        AltitudeSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value" && !_isUpdating)
            {
                _isUpdating = true;
                try
                {
                    AltitudeInput.Text = Math.Round(AltitudeSlider.Value).ToString(CultureInfo.InvariantCulture);
                    ProcessInput(AltitudeInput.Text);
                }
                finally { _isUpdating = false; }
            }
        };

        AltitudeInput.TextChanged += (s, e) =>
        {
            if (!_isUpdating)
            {
                _isUpdating = true;
                try { ProcessInput(AltitudeInput.Text); }
                finally { _isUpdating = false; }
            }
        };

        GraphCanvas.SizeChanged += (s, e) => DrawGraph();

        UpdateCalculations(0);
    }

    private void ProcessInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        string normalizedInput = input.Replace(',', '.');

        if (!double.TryParse(normalizedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double altitude))
        {
            ShowError("Только числа");
            return;
        }

        if (altitude < 0 || altitude > MaxAltitude)
        {
            ShowError($"От 0 до {MaxAltitude} м");
            return;
        }

        ClearError();
        if (Math.Abs(AltitudeSlider.Value - altitude) > 0.1)
            AltitudeSlider.Value = altitude;

        UpdateCalculations(altitude);
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorMessage.IsVisible = true;
        AltitudeInput.Foreground = Brushes.Red;
        CurrentPointMarker.IsVisible = false;
        MarkerLabel.IsVisible = false;
    }

    private void ClearError()
    {
        ErrorMessage.IsVisible = false;
        AltitudeInput.Foreground = Brushes.Black;
        CurrentPointMarker.IsVisible = true;
        MarkerLabel.IsVisible = true;
    }

    private void UpdateCalculations(double altitude)
    {
        string requestJson = $"{{\"altitude\": {altitude.ToString(CultureInfo.InvariantCulture)}}}";
        string responseJson = _controller.GetAtmosphereData(requestJson);

        using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!root.GetProperty("success").GetBoolean()) return;

        var data = root.GetProperty("data");
        double pressure = data.GetProperty("pressure").GetDouble();
        double density = data.GetProperty("density").GetDouble();
        double tempCelsius = data.GetProperty("temperatureC").GetDouble();

        PressureOutput.Text = $"{pressure.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(",", " ")} Па";
        DensityOutput.Text = $"{density.ToString("0.0000", CultureInfo.InvariantCulture)} кг/м³";
        TempOutput.Text = $"{tempCelsius.ToString("0.00", CultureInfo.InvariantCulture)} °C";

        UpdateTemperatureVisuals(tempCelsius);
        UpdateMarkerPosition(altitude, pressure);
    }

    private void UpdateTemperatureVisuals(double celsius)
    {
        double minTemp = -56.5;
        double maxTemp = 15.0;
        double t = (Math.Max(minTemp, Math.Min(maxTemp, celsius)) - minTemp) / (maxTemp - minTemp);

        var bgCold1 = Color.Parse("#F8FAFC");
        var bgCold2 = Color.Parse("#E0F2FE");
        var bgHot1 = Color.Parse("#F8FAFC");
        var bgHot2 = Color.Parse("#FEF2F2");

        var iconCold1 = Color.Parse("#7DD3FC");
        var iconCold2 = Color.Parse("#0284C7");
        var iconHot1 = Color.Parse("#FCA5A5");
        var iconHot2 = Color.Parse("#DC2626");

        TempCard.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Interpolate(bgCold1, bgHot1, t), 0),
                new GradientStop(Interpolate(bgCold2, bgHot2, t), 1)
            }
        };

        TempIcon.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Interpolate(iconCold1, iconHot1, t), 0),
                new GradientStop(Interpolate(iconCold2, iconHot2, t), 1)
            }
        };
    }

    private Color Interpolate(Color c1, Color c2, double t)
    {
        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);
        return Color.FromRgb(r, g, b);
    }

    private void DrawGraph()
    {
        if (GraphCanvas.Bounds.Width == 0 || GraphCanvas.Bounds.Height == 0) return;

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;

        var points = new List<Point>();
        var fillPoints = new List<Point>();
        int segments = 100;

        var calc = new AtmosphereCalc.Models.AtmosphereCalculator();

        for (int i = 0; i <= segments; i++)
        {
            double alt = (MaxAltitude / segments) * i;
            var res = calc.Calculate(alt);

            double x = (alt / MaxAltitude) * width;
            double y = height - ((res.Pressure / MaxPressure) * height);

            var pt = new Point(x, y);
            points.Add(pt);
            fillPoints.Add(pt);
        }

        PressureCurve.Points = points;

        fillPoints.Add(new Point(width, height));
        fillPoints.Add(new Point(0, height));
        PressureArea.Points = fillPoints;

        if (!ErrorMessage.IsVisible)
        {
            var currentRes = calc.Calculate(AltitudeSlider.Value);
            UpdateMarkerPosition(AltitudeSlider.Value, currentRes.Pressure);
        }
    }

    private void UpdateMarkerPosition(double altitude, double pressure)
    {
        if (GraphCanvas.Bounds.Width == 0) return;

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;
        double x = (altitude / MaxAltitude) * width;
        double y = height - ((pressure / MaxPressure) * height);

        Canvas.SetLeft(CurrentPointMarker, x);
        Canvas.SetTop(CurrentPointMarker, y);

        Canvas.SetLeft(MarkerLabel, x);
        Canvas.SetTop(MarkerLabel, y);
        MarkerText.Text = $"{altitude:F0} м\n{pressure:F0} Па";
    }
}
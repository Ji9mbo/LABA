using System.Text.Json;
using AtmosphereCalc.Models;

namespace AtmosphereCalc.Controllers;

public class AtmosphereController
{
    private readonly AtmosphereCalculator _calculator = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string GetAtmosphereData(string requestJson)
    {
        var input = JsonSerializer.Deserialize<AtmosphereInput>(requestJson, _jsonOptions) ?? new AtmosphereInput();

        if (input.Altitude < 0 || input.Altitude > 20000)
        {
            return JsonSerializer.Serialize(new { success = false, message = "Высота должна быть от 0 до 20000 м" }, _jsonOptions);
        }

        var result = _calculator.Calculate(input.Altitude);

        return JsonSerializer.Serialize(new { success = true, data = result }, _jsonOptions);
    }
}
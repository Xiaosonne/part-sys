using System.Text.Json;

namespace InventorySystem.Infrastructure.Services;

internal static class FormDataConverter
{
    public static Dictionary<string, object> Convert(Dictionary<string, object>? formData)
    {
        if (formData == null)
            return new Dictionary<string, object>();

        var result = new Dictionary<string, object>();
        foreach (var kvp in formData)
        {
            result[kvp.Key] = ConvertValue(kvp.Value);
        }
        return result;
    }

    public static object ConvertValue(object? value)
    {
        if (value == null)
            return string.Empty;

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Null => string.Empty,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) ? intVal :
                                       jsonElement.TryGetInt64(out var longVal) ? longVal :
                                       jsonElement.TryGetDouble(out var doubleVal) ? doubleVal :
                                       jsonElement.GetRawText(),
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Array => jsonElement.EnumerateArray()
                    .Select(e => ConvertValue(e))
                    .ToList(),
                JsonValueKind.Object => jsonElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertValue(p.Value)),
                _ => jsonElement.GetRawText()
            };
        }

        return value;
    }
}

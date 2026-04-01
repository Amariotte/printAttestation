using System.Text.Json;

namespace InteroperabiliteProject.Tools
{
    public static class Utilitaire
    {
        static bool TryGetPropertyValue(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            value = default;
            return false;
        }
    }
}

using System.Text.Json;

namespace Uns.Explorer.Services
{
    public class FormatService
    {
        public string GetString(UnsEventMessage message, UnsEventContentType? contentType = null)
        {
            if (message != null)
            {
                var inputContentType = contentType;
                if (inputContentType == null) inputContentType = message.ContentType;

                switch (inputContentType)
                {
                    case UnsEventContentType.JSON: return ConvertJson(message.Content);
                    case UnsEventContentType.SPARKPLUG_B_METRIC: return ConvertJson(message.Content);
                    default: return ConvertPlainText(message.Content);
                }
            }

            return null;
        }

        private static string ConvertPlainText(byte[] content)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetString(content);
            }
            catch { }

            return null;
        }

        private static string ConvertJson(byte[] content)
        {
            try
            {
                var options = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                var json= JsonSerializer.Deserialize<JsonElement>(content);

                return JsonSerializer.Serialize(json, options);
            }
            catch { }

            return null;
        }
    }
}

using Newtonsoft.Json.Linq;

namespace Backend.Common.Extensions
{
    /// <summary>
    /// JSON Extension for getting values
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Finds a value
        /// </summary>
        public static string GetValue(this JToken jtoken, string parameter)
        {
            return ((JObject)jtoken).GetValue(parameter, StringComparison.OrdinalIgnoreCase)?.ToString();
        }


        /// <summary>
        /// Checks if the json is empty
        /// </summary>
        public static bool IsEmpty(this JToken token)
        {
            return (token.Type == JTokenType.Null);
        }


        /// <summary>
        /// Clean up a json and remove the empty children
        /// </summary>
        public static JToken RemoveEmptyChildren(this JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                JObject copy = new JObject();
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    JToken child = prop.Value;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!IsEmpty(child))
                    {
                        copy.Add(prop.Name, child);
                    }
                }
                return copy;
            }
            else if (token.Type == JTokenType.Array)
            {
                JArray copy = new JArray();
                foreach (JToken item in token.Children())
                {
                    JToken child = item;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!IsEmpty(child))
                    {
                        copy.Add(child);
                    }
                }
                return copy;
            }
            return token;
        }

    }
}

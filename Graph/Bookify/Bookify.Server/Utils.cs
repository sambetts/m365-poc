using Microsoft.Kiota.Abstractions.Serialization;
using System.Text;

namespace Bookify.Server;

public class Utils
{
    public async static Task<T?> DeserializeGraphJson<T>(string json, ParsableFactory<T> factory) where T : IParsable
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var root = await ParseNodeFactoryRegistry.DefaultInstance.GetRootParseNodeAsync("application/json", stream);

        // Pass the required factory argument to GetObjectValue
        return root.GetObjectValue(factory);
    }
}

using System.Text.Json.Serialization;
using LANChatServer.Models;

namespace LANChatServer
{
    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(NetworkMessage))]
    [JsonSerializable(typeof(System.Collections.Generic.List<NetworkMessage>))]
    public partial class JsonContext : JsonSerializerContext
    {
    }
}

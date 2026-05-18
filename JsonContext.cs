using System.Text.Json.Serialization;
using LANChatPro.Models;
using LANChatPro.Storage;
using System.Collections.Generic;

namespace LANChatPro
{
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
    [JsonSerializable(typeof(PeerInfo))]
    [JsonSerializable(typeof(ChatMessage))]
    [JsonSerializable(typeof(NetworkMessage))]
    [JsonSerializable(typeof(FileTransferInfo))]
    [JsonSerializable(typeof(AppConfig))]
    [JsonSerializable(typeof(List<PeerInfo>))]
    [JsonSerializable(typeof(List<ChatMessage>))]
    [JsonSerializable(typeof(List<NetworkMessage>))]
    [JsonSerializable(typeof(Dictionary<string, List<ChatMessage>>))]
    public partial class JsonContext : JsonSerializerContext
    {
    }
}

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MindDuck.Contracts;

namespace MindDuck.Server.WebSockets
{
    public sealed class SocketMessageSender
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public Task SendAsync(WebSocket webSocket, ServerMessage message)
        {
            var envelope = Envelope.For(message);
            string json = JsonSerializer.Serialize(envelope, JsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            return webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None
            );
        }
    }
}
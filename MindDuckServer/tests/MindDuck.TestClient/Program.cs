using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MindDuck.Contracts;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    static async Task Main(string[] args)
    {
        using HttpClient httpClient = new HttpClient();

        Console.WriteLine("1. Test tworzenia pokoju");
        HttpResponseMessage response = await httpClient.PostAsync("http://localhost:5000/api/rooms", null);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Błąd: {response.StatusCode}");
            return;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Odpowiedź z serwera: {jsonResponse}");

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        string roomCode = doc.RootElement.GetProperty("roomCode").GetString()!;
        Console.WriteLine($"Utworzono pokój: {roomCode}\n");


        Console.WriteLine("\n2. Test ruchu bez dołączenia do pokoju");
        using (ClientWebSocket clientWebSocket = new ClientWebSocket())
        {
            await clientWebSocket.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
            await SendEnvelopeAsync(clientWebSocket, new PlayCard { CardInstanceId = 1 });

            Envelope? errNotInRoom = await ReceiveEnvelopeAsync(clientWebSocket);
            string expectedErrorResponse = nameof(Error);
            string actualErrorResponse = errNotInRoom?.Type ?? "null";

            Console.WriteLine($"Oczekiwano: {expectedErrorResponse}, otrzymano: {actualErrorResponse}");

            Console.WriteLine("\n\n3. Test złego formatu JSON");
            await SendRawAsync(clientWebSocket, "{to nie jest poprawny json");

            Envelope? errBadJson = await ReceiveEnvelopeAsync(clientWebSocket);
            expectedErrorResponse = nameof(Error);
            actualErrorResponse = errBadJson?.Type ?? "null";

            Console.WriteLine($"Oczekiwano: {expectedErrorResponse}, otrzymano: {actualErrorResponse}");

            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "koniec", CancellationToken.None);
        }

        Console.WriteLine("\n\n4. Test dołączenia dwóch graczy i startu meczu");
        using ClientWebSocket playerP1 = new ClientWebSocket();
        using ClientWebSocket playerP2 = new ClientWebSocket();

        await playerP1.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
        await playerP2.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
        Console.WriteLine("Obaj gracze połączyli się z WebSocketem\n");

        await SendEnvelopeAsync(playerP1, new JoinRoom { RoomCode = roomCode, Nickname = "Player1" });
        Envelope? p1Joined = await ReceiveEnvelopeAsync(playerP1);
        string expectedP1Response = nameof(RoomJoined);
        string actualP1Response = p1Joined?.Type ?? "null";
        Console.WriteLine($"Player1 - oczekiwano: {expectedP1Response}, otrzymano: {actualP1Response}\n");

        await SendEnvelopeAsync(playerP2, new JoinRoom { RoomCode = roomCode, Nickname = "Player2" });
        Envelope? p2Joined = await ReceiveEnvelopeAsync(playerP2);
        string expectedP2Response = nameof(RoomJoined);
        string actualP2Response = p2Joined?.Type ?? "null";
        Console.WriteLine($"Player2 - oczekiwano: {expectedP2Response}, otrzymano: {actualP2Response}\n");

        Envelope? p1Started = await ReceiveEnvelopeAsync(playerP1);
        Envelope? p1State = await ReceiveEnvelopeAsync(playerP1);
        string expectedStartResponses = $"{nameof(MatchStarted)}, {nameof(StateUpdate)}";
        string actualP1Responses = $"{p1Started?.Type ?? "null"}, {p1State?.Type ?? "null"}";
        Console.WriteLine($"Player1 - oczekiwano: {expectedStartResponses}, otrzymano: {actualP1Responses}\n");

        Envelope? p2Started = await ReceiveEnvelopeAsync(playerP2);
        Envelope? p2State = await ReceiveEnvelopeAsync(playerP2);
        string actualP2Responses = $"{p2Started?.Type ?? "null"}, {p2State?.Type ?? "null"}";
        Console.WriteLine($"Player2 - oczekiwano: {expectedStartResponses}, otrzymano: {actualP2Responses}");

        Console.WriteLine("\n\n5. Test niepoprawnego ruchu");
        await SendEnvelopeAsync(playerP1, new PlayCard { CardInstanceId = -999 });

        Envelope? rejected = await ReceiveEnvelopeAsync(playerP1);
        string expectedResponse = nameof(ActionRejected);
        string actualResponse = rejected?.Type ?? "null";

        Console.WriteLine($"Oczekiwano: {expectedResponse}, otrzymano: {actualResponse}");

        Console.WriteLine("\n6. Test poprawnego zagrania karty");
        int cardInstanceId = GetFirstCardInstanceId(p1State);
        Console.WriteLine($"Zagranie pierwszej karty z ręki, instanceId={cardInstanceId}");
        
        await SendEnvelopeAsync(playerP1, new PlayCard { CardInstanceId = cardInstanceId });

        Envelope? p1Update = await ReceiveEnvelopeAsync(playerP1);
        Envelope? p2Update = await ReceiveEnvelopeAsync(playerP2);

        Console.WriteLine($"P1 otrzymał: {p1Update?.Type ?? "null"}");
        Console.WriteLine($"P2 otrzymał: {p2Update?.Type ?? "null"}");

        await playerP1.CloseAsync(WebSocketCloseStatus.NormalClosure, "koniec", CancellationToken.None);
        await playerP2.CloseAsync(WebSocketCloseStatus.NormalClosure, "koniec", CancellationToken.None);
    }

    private static async Task SendEnvelopeAsync(WebSocket ws, ClientMessage payload)
    {
        var envelope = Envelope.For(payload);
        string json = JsonSerializer.Serialize(envelope, JsonOptions);
        await SendRawAsync(ws, json);
    }

    private static async Task SendRawAsync(WebSocket ws, string text)
    {
        Console.WriteLine($"Klient wysyła do serwera: {text}");
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<Envelope?> ReceiveEnvelopeAsync(WebSocket ws)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        byte[] buffer = new byte[1024 * 16];
        WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Serwer wysyła do klienta: {json}");
        return JsonSerializer.Deserialize<Envelope>(json, JsonOptions);
    }

    private static int GetFirstCardInstanceId(Envelope? stateUpdate)
    {
        if (stateUpdate is null ||
            stateUpdate.Type != nameof(StateUpdate) ||
            stateUpdate.Payload is not JsonElement payload)
        {
            throw new InvalidOperationException("Expected a StateUpdate message.");
        }

        JsonElement hand = payload.GetProperty("state").GetProperty("you").GetProperty("hand");

        if (hand.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The player's hand is empty.");
        }

        return hand[0].GetProperty("instanceId").GetInt32();
    }
}
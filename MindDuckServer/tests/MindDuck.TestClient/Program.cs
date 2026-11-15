using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MindDuck.Contracts;

class Program
{
    static async Task Main(string[] args)
    {
        string baseUrl = "http://localhost:5000";
        string wsUrl = "ws://localhost:5000/ws";

        using HttpClient httpClient = new HttpClient();

        Console.WriteLine("Test tworzenia pokoju");
        Console.WriteLine($"Wysyłam zapytanie POST do {baseUrl}/api/rooms...");

        HttpResponseMessage response = await httpClient.PostAsync($"{baseUrl}/api/rooms", null);
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Błąd, serwer zwrócił status: {response.StatusCode}");
            return;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Odpowiedź z serwera: {jsonResponse}");

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        string roomCode = doc.RootElement.GetProperty("roomCode").GetString();
        Console.WriteLine($"Udało się, otrzymany kod pokoju to: {roomCode}\n");


        Console.WriteLine("Test połączenia WebSocket i dołączania do pokoju");
        Console.WriteLine($"Próba nawiązania połączenia WebSocket z {wsUrl}...");

        using ClientWebSocket ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
        Console.WriteLine("Połączono z serwerem przez WebSocket!");

        var joinRoomMsg = new JoinRoom
        {
            RoomCode = roomCode,
            Nickname = "TestowyGracz"
        };

        var envelope = Envelope.For(joinRoomMsg);

        string envelopeJson = JsonSerializer.Serialize(envelope);
        byte[] sendBytes = Encoding.UTF8.GetBytes(envelopeJson);

        Console.WriteLine($"Wysyłanie pakietu JSON przez WebSocket: {envelopeJson}");
        
        await ws.SendAsync(
            new ArraySegment<byte>(sendBytes), 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None
        );

        byte[] receiveBuffer = new byte[1024];
        WebSocketReceiveResult result = await ws.ReceiveAsync(
            new ArraySegment<byte>(receiveBuffer), 
            CancellationToken.None
        );

        string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
        Console.WriteLine($"Odebrano odpowiedź z serwera przez WebSocket:\n{receivedMessage}\n");

        await ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure, 
            "Test zakończony", 
            CancellationToken.None
        );
        Console.WriteLine("Połączenie WebSocket zamknięte, wszytsko działa.");
    }
}
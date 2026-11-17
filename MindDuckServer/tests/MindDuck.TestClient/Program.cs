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
        using HttpClient httpClient = new HttpClient();

        Console.WriteLine("Test tworzenia pokoju");
        Console.WriteLine($"Wysyłam zapytanie POST do http://localhost:5000/api/rooms...");

        HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5000/api/rooms", null);
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Błąd: {response.StatusCode}");
            return;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Odpowiedź z serwera: {jsonResponse}");

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        string roomCode = doc.RootElement.GetProperty("roomCode").GetString();
        Console.WriteLine($"Otrzymany kod pokoju to: {roomCode}\n");


        Console.WriteLine("Test połączenia WebSocket i dołączania do pokoju");
        Console.WriteLine($"Łączę się z WebSocketem pod ws://localhost:5000/ws...");

        using ClientWebSocket ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
        Console.WriteLine("Połączono z serwerem przez WebSocket\n");

        var joinRoomMsg = new JoinRoom
        {
            RoomCode = roomCode,
            Nickname = "TestowyGracz"
        };

        var envelope = Envelope.For(joinRoomMsg);

        string envelopeJson = JsonSerializer.Serialize(envelope);
        byte[] sendBytes = Encoding.UTF8.GetBytes(envelopeJson);

        Console.WriteLine($"Wysyłam pakiet JSON do dołączenia do pokoju przez WebSocket: {envelopeJson}");
        
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
        Console.WriteLine($"Serwer odebrał i odpowiedział przez WebSocket:\n{receivedMessage}\n");

        await ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure, 
            "koniec testu", 
            CancellationToken.None
        );
        Console.WriteLine("Zamykam połączenie WebSocket, wszytsko działa.");
    }
}
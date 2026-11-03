namespace MindDuck.Contracts
{
    /// <summary>Base class of every message the CLIENT sends to the server over WebSocket.</summary>
    public abstract class ClientMessage
    {
    }

    /// <summary>Base class of every message the SERVER sends to a client over WebSocket.</summary>
    public abstract class ServerMessage
    {
    }

    /// <summary>
    /// What actually travels over the WebSocket: { "type": "PlayCard", "payload": { "cardInstanceId": 17 } }.
    /// Type = the C# class name of the message. See MessageTypes for the reverse lookup.
    /// </summary>
    public sealed class Envelope
    {
        public string Type { get; set; }
        public object Payload { get; set; }

        public static Envelope For(ClientMessage message)
        {
            return new Envelope { Type = message.GetType().Name, Payload = message };
        }

        public static Envelope For(ServerMessage message)
        {
            return new Envelope { Type = message.GetType().Name, Payload = message };
        }
    }
}

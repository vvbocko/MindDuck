using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using MindDuck.Contracts;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MindDuck.Client.Networking
{
    public sealed class WebSocketGameConnection : IGameConnection
    {
                public event Action<RoomJoined> RoomJoined;
                public event Action<MatchStarted> MatchStarted;
                public event Action<StateUpdate> StateUpdated;
                public event Action<DecisionRequired> DecisionRequired;
                public event Action<ActionRejected> ActionRejected;
                public event Action<bool, int> OpponentConnectionChanged;
                public event Action<MatchEnded> MatchEnded;
                public event Action ConnectionLost;
                public event Action<Error> Error;

        private WebSocket websocket;

        public Task ConnectAsync(string url)
        {

            websocket = new WebSocket($"ws://{url}");

            websocket.OnOpen += () => {
                Debug.Log("Connection open!");
            };
            websocket.OnError += (e) => Debug.Log("Error! " + e);
            websocket.OnClose += (code) => {
                Debug.Log("Connection closed!");
                ConnectionLost.Invoke();
            };

            websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + message);
                var root = JObject.Parse(message);

                string typeName = root["type"]!.Value<string>()!;

                object payload;
                switch (typeName)
                {
                    case "RoomJoined":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.RoomJoined))!;
                        RoomJoined.Invoke((MindDuck.Contracts.RoomJoined)payload);
                        break;
                    case "MatchStarted":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.MatchStarted))!;
                        MatchStarted.Invoke((MindDuck.Contracts.MatchStarted)payload);
                        break;
                    case "StateUpdated":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.StateUpdate))!;
                        StateUpdated.Invoke((MindDuck.Contracts.StateUpdate)payload);
                        break;
                    case "DecisionRequired":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.DecisionRequired))!;
                        DecisionRequired.Invoke((MindDuck.Contracts.DecisionRequired)payload);
                        break;
                    case "ActionRejected":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.ActionRejected))!;
                        ActionRejected.Invoke((MindDuck.Contracts.ActionRejected)payload);
                        break;
                    case "OpponentDisconnected":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.OpponentDisconnected))!;
                        OpponentConnectionChanged.Invoke(true, (int)payload);
                        break;
                    case "OpponentReconnected":
                        OpponentConnectionChanged.Invoke(false, 0);
                        break;
                    case "MatchEnded":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.MatchEnded))!;
                        MatchEnded.Invoke((MindDuck.Contracts.MatchEnded)payload);
                        break;
                    case "Error":
                        payload = root["payload"]!.ToObject(typeof(MindDuck.Contracts.Error))!;
                        Error.Invoke((MindDuck.Contracts.Error)payload);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown type: {typeName}");
                }
            };

            //await websocket.Connect();
            return Task.CompletedTask;
        }

        public async void JoinRoom(string roomCode, string nickname)
        {
            JoinRoom message = new JoinRoom
            {
                RoomCode = roomCode,
                Nickname = nickname
            };
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }

        public async void Rejoin(string roomCode, string playerToken)
        {
            Rejoin message = new Rejoin
            {
                RoomCode = roomCode,
                PlayerToken = playerToken
                
            };
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }
        
        public async void PlayCard(int cardInstanceId)
        {
            PlayCard message = new PlayCard
            {
                CardInstanceId = cardInstanceId
            };
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }

        public async void Attack(int creatureInstanceId)
        {
            Attack message = new Attack
            {
                CreatureInstanceId = creatureInstanceId
            };
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }

        public async void SubmitDecision(int decisionId, string optionId)
        {
            SubmitDecision message = new SubmitDecision
            {
                DecisionId = decisionId,
                OptionId = optionId
            };
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }

        public async void LeaveRoom()
        {
            LeaveRoom message = new LeaveRoom{};
            string json = JsonConvert.SerializeObject(message);
            byte[] bytesToSend = Encoding.UTF8.GetBytes(json);
            await websocket.Send(bytesToSend);
        }
    }
}
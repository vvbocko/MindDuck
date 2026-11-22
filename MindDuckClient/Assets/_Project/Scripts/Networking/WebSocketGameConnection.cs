using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MindDuck.Contracts;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft;

namespace MindDuck.Client.Networking
{
    public sealed class WebSocketGameConnection : IGameConnection
    {
        #pragma warning disable CS0067
                public event Action<RoomJoined> RoomJoined;
                public event Action<MatchStarted> MatchStarted;
                public event Action<StateUpdate> StateUpdated;
                public event Action<DecisionRequired> DecisionRequired;
                public event Action<ActionRejected> ActionRejected;
                public event Action<bool, int> OpponentConnectionChanged;
                public event Action<MatchEnded> MatchEnded;
                public event Action ConnectionLost;
        #pragma warning restore CS0067

        private WebSocket websocket;

        // ---- Client -> Server ----------------------------------------------------------

        /// <summary>Open the connection. "Task" = it finishes later; use "await".</summary>
        public async void ConnectAsync(string url, string port)
        {

            websocket = new WebSocket($"ws://{url}:{port}");

            websocket.OnOpen += () => Debug.Log("Connection open!");
            websocket.OnError += (e) => Debug.Log("Error! " + e);
            websocket.OnClose += (code) => Debug.Log("Connection closed!");

            websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + message);
            };

            await websocket.Connect();
        }

        public void Rejoin(string roomCode, string playerToken) {}
        public void JoinRoom(string roomCode, string nickname) {}
        public void PlayCard(int cardInstanceId) {}
        public void Attack(int creatureInstanceId) {}
        public void SubmitDecision(int decisionId, string optionId) {}
        public void LeaveRoom() {}
    }
}
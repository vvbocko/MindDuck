//   Creates the objects the game screen needs: connection and the card catalog and hands those to MatchController so it can work wit any IGameConnection
//   The room code and nickname will come from the main menu / lobby (networking/menus owner). For now they are typed into the Inspector for testing

using System;
using MindDuck.Client.Cards;
using MindDuck.Client.Match;
using MindDuck.Client.Networking;
using UnityEngine;

namespace MindDuck.Client.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private bool useFakeServer = true;
        [SerializeField] private string serverUrl = "ws://localhost:5000/ws";

        [Header("Test values (later from the menu)")]
        [SerializeField] private string roomCode = "TEST";
        [SerializeField] private string nickname = "Player";

        [Header("Scene objects")]
        [SerializeField] private MatchController matchController;

        private IGameConnection _connection;

        // "async void" is OK for Unity's Start: Unity calls it, and we can "await" inside.
        private async void Start()
        {
            try
            {
                ICardCatalog catalog = new LocalCardCatalog();

                if (useFakeServer)
                {
                    _connection = new FakeGameConnection();
                }
                else
                {
                    _connection = new WebSocketGameConnection();
                }

                matchController.Initialize(_connection, catalog);

                await _connection.ConnectAsync(serverUrl);
                _connection.JoinRoom(roomCode, nickname);
            }
            catch (Exception exception)
            {
                // Without this, an error inside "async void" can be easy to miss.
                Debug.LogException(exception);
            }
        }
    }
}

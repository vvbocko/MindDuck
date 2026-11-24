using UnityEngine;
using TMPro;
using MindDuck.Client.Networking;



public class ConnectButtonScript : MonoBehaviour
{
    public TMP_InputField url;
    public TMP_InputField port;
    private WebSocketGameConnection ws = new WebSocketGameConnection();

    public void ConnectButton()
    {
        ws.ConnectAsync(url.text, port.text);
    }

    public WebSocketGameConnection getWS()
    {
        return ws;
    }


}


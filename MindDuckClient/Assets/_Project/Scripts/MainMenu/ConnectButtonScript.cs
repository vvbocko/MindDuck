using UnityEngine;
using TMPro;
using MindDuck.Client.Networking;



public class ConnectButtonScript : MonoBehaviour
{
    public TMP_InputField url;
    public TMP_InputField port;

    public void ConnectButton()
    {
        WebSocketGameConnection ws = new WebSocketGameConnection();
        ws.ConnectAsync(url.text, port.text);
    }


}


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MindDuck.Client.Networking;

public class test : MonoBehaviour
{

    public Button button;
    public WebSocketGameConnection ws;
    public TMP_Text text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ws.ConnectionEstablished += testtest();
    }

    void OnDestroy()
    {
        // ws.ConnectionEstablished -= testtest();
    }

    // Update is called once per frame
    void testtest()
    {
        text.text = "O_o";
    }
}

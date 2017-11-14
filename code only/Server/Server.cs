using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;


public class ServerClient
{
    public int connectionId;
    public int ObjectId;
    public string playerName;
    public GameObject gameObject;
    public List<ButtonState> Inputs = new List<ButtonState>();
}

public class ButtonState
{
    public KeyCode Key { get; set; }
    public bool IsPressed { get; set; }
    public ButtonState(KeyCode key, bool isPressed)
    {
        Key = key;
        IsPressed = isPressed;
    }
}
public class Server : MonoBehaviour
{
    
    public List<Transform> SpawnPositions;
    private List<bool> SpawnPositionsUsed = new List<bool>();

    private List<ServerClient> Clients = new List<ServerClient>();
    private StateUpdateManager stateUpdateManagerScript;

    private const int MAX_CONNECTION = 100;

    private int port = 5705;
    private int hostId;
    private int webHostId;
    private int reliableChannel;
    private int reliableFragChannel;
    private int unreliableChannel;
    private int unreliableCh;
    private byte error;

    private bool isConnected = false;


    public GameObject playerPrefab;
    public List<ButtonState> GetInput(int cnnId)
    {
        return Clients.Find(x => x.connectionId == cnnId).Inputs;
    }

    // Use this for initialization
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        reliableFragChannel = cc.AddChannel(QosType.ReliableFragmented);

        unreliableChannel = cc.AddChannel(QosType.UnreliableFragmented);
        unreliableCh = cc.AddChannel(QosType.Unreliable);

        cc.PacketSize = 1024;


        HostTopology Topo = new HostTopology(cc, MAX_CONNECTION);
        hostId = NetworkTransport.AddHost(Topo, port, null);
        webHostId = NetworkTransport.AddWebsocketHost(Topo, port, null);
        Debug.Log("server started");

        isConnected = true;

        stateUpdateManagerScript = GameObject.Find("NetworkManager").GetComponent<StateUpdateManager>();
        // has to be in the correct order

        for (int i = 0; i < SpawnPositions.Count; i++)
        {
            SpawnPositionsUsed.Add(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isConnected)
            return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                Debug.Log("Player " + connectionId + " has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:       //3
                bool isBitpacked = BitConverter.ToBoolean(recBuffer, 0);
                if (isBitpacked)
                {
                    int messageNumber = BitConverter.ToInt16(recBuffer, sizeof(bool));
                    switch (messageNumber)
                    {
                        case 7:
                            Send("LAG|", reliableChannel, connectionId);
                            OnInput(recBuffer, connectionId);
                            break;
                        default:
                            break;
                    }

                    return;
                }

                string msg = Encoding.Unicode.GetString(recBuffer, 1, dataSize);
                // Debug.Log("recieving from  " + connectionId + " : " + msg );

                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "NAMEIS":
                        Debug.Log("nameis : " + splitData[1]);
                        OnNameIs(connectionId, splitData[1]);
                        break;
                    case "CNN":
                        Debug.Log("CNN: " + msg);
                        break;
                    case "DC":
                        Debug.Log("DC: " + connectionId);
                        break;
                    case "INPUT":
                        Debug.Log("INPUT from " + connectionId);
                        Send("LAG|", unreliableCh, connectionId);
                        OnInput(splitData[1], connectionId);
                        break;
                    default:
                        Debug.Log("Invalid message: " + splitData[1]);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                OnDisconnection(connectionId);
                Debug.Log("Player " + connectionId + " has disconnected");
                break;
        }
    }

    private void OnConnection(int cnnId)
    {
        ServerClient c = new ServerClient();
        c.connectionId = cnnId;
        c.Inputs.Add(new ButtonState(KeyCode.A, false));
        c.Inputs.Add(new ButtonState(KeyCode.W, false));
        c.Inputs.Add(new ButtonState(KeyCode.S, false));
        c.Inputs.Add(new ButtonState(KeyCode.D, false));
        c.Inputs.Add(new ButtonState(KeyCode.Q, false));
        c.Inputs.Add(new ButtonState(KeyCode.E, false));
        c.Inputs.Add(new ButtonState(KeyCode.Z, false));
        c.Inputs.Add(new ButtonState(KeyCode.Space, false));
        c.Inputs.Add(new ButtonState(KeyCode.Mouse0, false));
        c.Inputs.Add(new ButtonState(KeyCode.Mouse1, false));
        c.playerName = "TEMP";
        Clients.Add(c);
        // ask the client name and send the other client names with the request
        string msg = "ASKNAME|" + cnnId + "|";
        foreach (ServerClient sc in Clients)
        {
            msg += sc.playerName + '%' + sc.connectionId + '|';
        }
        msg = msg.Trim('|');
        Send(msg, reliableChannel, cnnId);
        Debug.Log("askname send");

        // send the current scene to the player
        stateUpdateManagerScript.SendSceneData(cnnId);
       
        // spawn the player on the server
        SpawnPlayer(Clients.Find(x => x.connectionId == cnnId).playerName , cnnId);

    }
    private void SpawnPlayer(string playerName, int cnnId)
    {
        GameObject playerGameObject = Instantiate(playerPrefab) as GameObject;
        // check if there are more players than spawnPositions
        bool foundSpot = false;
        for (int i = 0; i < SpawnPositionsUsed.Count; i++)
        {
            if (!SpawnPositionsUsed[i])
            {
                playerGameObject.transform.Translate(SpawnPositions[i].position);
                SpawnPositionsUsed[i] = true;
                foundSpot = true;
                break;
            }
        }
        if (!foundSpot)
        {
            playerGameObject.transform.Translate(new Vector3(0,30,0));
        }

        string msg = "SPAWNPOS|" + cnnId + "|" + SpawnPositions[cnnId].position.x + ":" + SpawnPositions[cnnId].position.y + ":" + SpawnPositions[cnnId].position.z;

        ServerClient client = Clients.Find(x => x.connectionId == cnnId);

        client.gameObject = playerGameObject;
        client.playerName = playerName;
        client.connectionId = cnnId;

        playerGameObject.GetComponent<NetworkObject>().cnnId = cnnId;
    }
    private void OnDisconnection(int cnnId)
    {
        ServerClient disconnectedClient = Clients.Find(x => x.connectionId == cnnId);
        Destroy(disconnectedClient.gameObject);
        Clients.Remove(disconnectedClient);
        Debug.Log("client disconnected:" + cnnId);
        Send("DC|" + cnnId, reliableChannel, Clients);
    }
    private void Send(string message, int channelId, int cnnId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(Clients.Find(x => x.connectionId == cnnId));

        Send(message, channelId, c);
    }
    private void Send(string message, int channelId, List<ServerClient> clients)
    {
        byte[] msg = Encoding.Unicode.GetBytes(message);

        byte[] msg2 = new byte[msg.Length + sizeof(bool)];
        Array.Copy(BitConverter.GetBytes(false), 0, msg2, 0, sizeof(bool));
        Array.Copy(msg, 0, msg2, sizeof(bool), message.Length * sizeof(char));
        foreach (ServerClient serverClient in clients)
       {
         NetworkTransport.Send(hostId, serverClient.connectionId, channelId, msg2, msg2.Length, out error);
       }
}
    private void Send(byte[] message, int channelId, int cnnId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(Clients.Find(x => x.connectionId == cnnId));

        Send(message, channelId, c);
    }
    private void Send(byte[] message, int channelId, List<ServerClient> clients)
    {
        byte[] msg = new byte[message.Length + sizeof(bool)];
        Array.Copy(BitConverter.GetBytes(true), 0, msg, 0, sizeof(bool));
        Array.Copy(message, 0, msg, sizeof(bool), message.Length);

        foreach (ServerClient serverClient in clients)
        {
            NetworkTransport.Send(hostId, serverClient.connectionId, channelId, msg, msg.Length, out error);
        }
    }
    private void OnNameIs(int cnnId, string playerName)
    {
        // link the name to the connection Id
        Clients.Find(x => x.connectionId == cnnId).playerName = playerName;
        // set text above the player
        ServerClient client = Clients.Find(x => x.connectionId == cnnId);
       // Clients.Find(x => x.connectionId == cnnId).gameObject.transform.Find("PlayerName").GetComponent<TextMesh>().text = playerName;
        // tell everybody that a new player has connected
        Send("CNN|" + playerName + '|' + cnnId + '|' + client.ObjectId, reliableChannel, Clients);
    }

    
    // Lockstep interpolation

    private void OnInput(string msg, int cnnId)
    {
        // only if there are clients
        if (Clients.Count > 0)
        {
            Debug.Log(msg);
            msg.ToLower();
            int index = 0;
            for (int i = 0; i < Clients[0].Inputs.Count; i++)
            {
                // down input : 0
                // up input : 1

                char isPressed = msg[index++];

                if (isPressed == '1')
                {
                    Clients.Find(x => x.connectionId == cnnId).Inputs[i].IsPressed = true;
                }
                else
                {
                    Clients.Find(x => x.connectionId == cnnId).Inputs[i].IsPressed = false;
                }
            }
        }
    }

    private void OnInput(byte[] msg, int cnnId)
    {
        // only if there are clients
        if (Clients.Count > 0)
        {
            int index = 0;
            index += sizeof(bool);

            index += sizeof(Int16);

            for (int i = 0; i < Clients[0].Inputs.Count; i++)
            {
                // down input : 0
                // up input : 1

                bool isPressed = BitConverter.ToBoolean(msg, index);
                index += sizeof(bool);

                if (isPressed)
                {
                    Clients.Find(x => x.connectionId == cnnId).Inputs[i].IsPressed = true;
                }
                else
                {
                    Clients.Find(x => x.connectionId == cnnId).Inputs[i].IsPressed = false;
                }
            }
        }
    }
    /// <summary>
    /// chanelId 0 -> Unreliable ||
    /// chanelId 1 -> Reliable
    /// </summary>
    /// <param name="mssg"></param>
    public void SendToAll(string mssg , int channelId = 0)
    {
        switch (channelId)
        {
            case 0:
                Send(mssg, unreliableChannel, Clients);
                break;
            case 1:
                Send(mssg, reliableChannel, Clients);
                break;
            case 3:
                Send(mssg, reliableFragChannel, Clients);
                break;
            case 4:
                Send(mssg, unreliableCh, Clients);
                break;
            default:
                Debug.Log("wrong channelID -> " + channelId);
                break;
        }
  
    }
    /// <summary>
    /// chanelId 0 -> Unreliable ||
    /// chanelId 1 -> Reliable
    /// </summary>
    /// <param name="mssg"></param>
    public void SendToClient(string mssg , int clientConnectionId, int channelId = 0)
    {
        switch (channelId)
        {
            case 0:
                Send(mssg, unreliableChannel, clientConnectionId);
                break;
            case 1:
                Send(mssg, reliableChannel, clientConnectionId);
                break;
            case 3:
                Send(mssg, reliableFragChannel, clientConnectionId);
                break;
            case 4:
                Send(mssg, unreliableCh, clientConnectionId);
                break;
            default:
                Debug.Log("wrong channelID -> " + channelId);
                break;
        }

    }



    /// <summary>
    /// chanelId 0 -> Unreliable ||
    /// chanelId 1 -> Reliable
    /// </summary>
    /// <param name="mssg"></param>
    public void SendToAll(byte[] msg, int channelId = 0)
    {
        switch (channelId)
        {
            case 0:
                Send(msg, unreliableChannel, Clients);
                break;
            case 1:
                Send(msg, reliableChannel, Clients);
                break;
            case 3:
                Send(msg, reliableFragChannel, Clients);
                break;
            case 4:
                Send(msg, unreliableCh, Clients);
                break;
            default:
                Debug.Log("wrong channelID -> " + channelId);
                break;
        }

    }
    /// <summary>
    /// chanelId 0 -> Unreliable ||
    /// chanelId 1 -> Reliable
    /// </summary>
    /// <param name="mssg"></param>
    public void SendToClient(byte[] msg, int clientConnectionId, int channelId = 0)
    {
        switch (channelId)
        {
            case 0:
                Send(msg, unreliableChannel, clientConnectionId);
                break;
            case 1:
                Send(msg, reliableChannel, clientConnectionId);
                break;
            case 3:
                Send(msg, reliableFragChannel, clientConnectionId);
                break;
            case 4:
                Send(msg, unreliableCh, clientConnectionId);
                break;
            default:
                Debug.Log("wrong channelID -> " + channelId);
                break;
        }

    }
}

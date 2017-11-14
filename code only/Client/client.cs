using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;

public enum ObjectType
{
    NoObject,
    Object1,
    Object2,
    Object3,
    Object4,
    Object5,
    Object6,
    Object7,
    Object8,
    Object9,
    Object10
}


public class Inputs
{
    public bool left;
    public bool right;
    public bool forward;
    public bool back;
}

[SerializeField]
public class Object
{
    public GameObject GameObject { set; get; }
    public int ObjectId { set; get; }
    public Object(int objId , GameObject gameObj)
    {
        ObjectId = objId;
        GameObject = gameObj;
    }
}
public class client : MonoBehaviour
{
 

    private const int MAX_CONNECTION = 100;

    private int port = 5705;
    private int hostId;
    private int webHostId;
    private int reliableChannel;
    private int reliableFragChannel;
    private int unreliableFragChannel;
    private int unreliableChannel;
    private int ourClientId;
    private int connectionId;
    private float connectionTime;
    public bool isConnected = false;
    private byte error;
    public GameObject PlayerObject;
    private string playerName;
    private int MyObjectId { get; set; }


    public List<GameObject> ObjectPrefabs = new List<GameObject>();
    private UpdateStateManager updateStateManager;

    // Use this for initialization
    void Start()
    {
        MyObjectId = -1;
        updateStateManager = GameObject.Find("NetworkManager").GetComponent<UpdateStateManager>();
    }
    public void Connect()
    {
        if (isConnected)
            return;

        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if (pName == "")
        {
            int rand = UnityEngine.Random.Range(0,999);
            pName = "guest" + rand;
            //Debug.Log("random name: guest" + rand);
        }
        playerName = pName;

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        reliableFragChannel = cc.AddChannel(QosType.ReliableFragmented);
        unreliableFragChannel = cc.AddChannel(QosType.UnreliableFragmented);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        cc.PacketSize = 1024;

        HostTopology Topo = new HostTopology(cc, MAX_CONNECTION);
        hostId = NetworkTransport.AddHost(Topo, 0);
        //webHostId = NetworkTransport.AddWebsocketHost(Topo, port, null);

        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", port, 0, out error);


        connectionTime = Time.time;
        isConnected = true;
        GameObject.Find("Canvas").transform.Find("Start").gameObject.SetActive(false);

    }
    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
 
        GUIStyle style = new GUIStyle();
 
        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
 
 
        string text = string.Format(" {0:0.0} ms ({1:0.} delayInput))", deltaTime * 1000 , DeltaInputTime * 1000);
        GUI.Label(rect, text, style);
    }
    // Update is called once per frame
    float prevTime;
    float deltaTime;
    float startTime;
    float DeltaInputTime;
    void Update()
    {
        if (!isConnected)
            return;
        int recHostId;
        int connectionId;
        int channelId;
        int bufferSize = 50000;
        byte[] recBuffer = new byte[bufferSize];
        int dataSize;

        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recData)
        {
            // time sinds the last recieved packet

             
            case NetworkEventType.DataEvent:       //3
                bool isBitpacked = BitConverter.ToBoolean(recBuffer, 0);
                if (isBitpacked)
                {
                    int messageNumber = BitConverter.ToInt16(recBuffer, sizeof(bool));
                    switch (messageNumber)
                    {
                        case 2: // state updates
                            updateStateManager.UpdateStates(recBuffer);
                           // updateStateManager.AddUpdateState(recBuffer);
                            deltaTime = Time.time - prevTime;
                            prevTime = Time.time;
                            break;
                        case 5: // scene data
                            CreateScene(recBuffer);
                            break;
                        default:
                            break;
                    }

                    return;
                }
                string msg = Encoding.Unicode.GetString(recBuffer, 1, dataSize);
                //Debug.Log("recieving: " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "ASKNAME":
                        //Debug.Log("ASKNAME: " + msg);
                        OnAskName(splitData);
                        break;
                    case "CNN": // new player connected to the server
                        break;
                    case "DC":
                        Debug.Log("DC: " + connectionId);
                     //   PlayerDisconect(int.Parse(splitData[1]));
                        break;
                    case "STATE":
                        //STATE|id|POS|1|x:y:z|ROT|1|x:y:z|VEL|0|x:y:z|
                        updateStateManager.UpdateStates(msg);
                        break;
                    case "SPAWNPOS":
                        //  Debug.Log("SPAWNPOS: ");
                        //SpawnPlayer(playerName, int.Parse(splitData[1]), splitData[2]);
                        break;
                    case "SPAWN":
                        //   Debug.Log("SPAWN: ");
                        msg = msg.Remove(0, msg.IndexOf('|') + 1);
                        SpawnObject( msg );
                        break;
                    case "OBJID":
                        //  Debug.Log("OBJID: ");
                        // get my object id from the server
                        MyObjectId = int.Parse(splitData[1]);
                        break;
                    case "SCENEDATA":
                        //  Debug.Log("SCENEDATA: ");
                        //CreateScene(msg);
                        break;
                    case "DEL":
                        updateStateManager.DestroyObject(int.Parse(splitData[1]));
                        break;
                    case "LAG":
                        DeltaInputTime = Time.time - startTime;
                        break;
                    default:
                        Debug.Log("Invalid message: " + msg);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                break;
        }
    }

    private void CreateScene(byte[] message)
    {
        int messageindex = 0;

        // first val
        messageindex += sizeof(bool); 

        // state
        messageindex += sizeof(Int16); 

        // objCount
        int objectCount = BitConverter.ToInt16(message, messageindex); // objCount
        messageindex += sizeof(Int16);

        // go over all the objects
        for (int i = 0; i < objectCount; i++)
        {
            GameObject objectToSpawn = null;
            // object id
            int objId = BitConverter.ToInt16(message, messageindex);
            messageindex += sizeof(Int16);

            // object type
            int objType = BitConverter.ToInt16(message, messageindex);
            messageindex += sizeof(Int16);

            //position
            Vector3 pos = new Vector3();
            pos.x = BitConverter.ToInt16(message, messageindex) / 100.0f;
            messageindex += sizeof(Int16);
            pos.y = BitConverter.ToInt16(message, messageindex) / 100.0f;
            messageindex += sizeof(Int16);
            pos.z = BitConverter.ToInt16(message, messageindex) / 100.0f;
            messageindex += sizeof(Int16);

            //rotation
            Vector3 rot = new Vector3();
            rot.x = BitConverter.ToSingle(message, messageindex);
            messageindex += sizeof(float);
            rot.y = BitConverter.ToSingle(message, messageindex);
            messageindex += sizeof(float);
            rot.z = BitConverter.ToSingle(message, messageindex);
            messageindex += sizeof(float);

            //
            if (objId == MyObjectId)
            {
                Debug.Log("This is me! spawning playerObj id's   " + objId + "     " + MyObjectId);
                objectToSpawn = Instantiate(PlayerObject, pos, Quaternion.Euler(rot)) as GameObject;
                objectToSpawn.GetComponent<NetworkObject>().ObjectId = objId;
            }
            else
            {
                objectToSpawn = Instantiate(ObjectPrefabs[(int)objType], pos, Quaternion.Euler(rot)) as GameObject;
                objectToSpawn.GetComponent<NetworkObject>().ObjectId = objId;
            }
        }
    }

    private void OnAskName(string[] data)
    {
        ourClientId = int.Parse(data[1]);

        Send("NAMEIS|" + playerName, reliableChannel);
    }

    private void SpawnObject(string msg)
    {
        //Debug.Log(msg);
        GameObject objectToSpawn = null;
        msg = msg.Trim('|');
        string[] splitMsg = msg.Split('|');
        int index = 0;
        // get object id
        int objectId = int.Parse(splitMsg[index++]);
     

        // get object type 
        ObjectType objectType = (ObjectType)int.Parse(splitMsg[index++]);

        // get position
        Vector3 pos = new Vector3();
        string[] positions = splitMsg[index++].Split(':');
        pos.x = float.Parse(positions[0]);
        pos.y = float.Parse(positions[1]);
        pos.z = float.Parse(positions[2]);

        // get rotation
        Vector3 rot = new Vector3();
        string[] rotation = splitMsg[index++].Split(':');
        rot.x = float.Parse(rotation[0]);
        rot.y = float.Parse(rotation[1]);
        rot.z = float.Parse(rotation[2]);

        // Debug.Log((int)objectType);
        if (MyObjectId == -1)
        {
            //Debug.Log("ERROR!!! player has no objectId yet");
        }
        if (objectId == MyObjectId)
        {
           objectToSpawn = Instantiate(PlayerObject, pos, Quaternion.Euler(rot)) as GameObject;
           objectToSpawn.GetComponent<NetworkObject>().ObjectId = objectId;
        }
        else
        {
            objectToSpawn = Instantiate(ObjectPrefabs[(int)objectType], pos, Quaternion.Euler(rot)) as GameObject;
            objectToSpawn.GetComponent<NetworkObject>().ObjectId = objectId;
        }
    }


    private void Send(string message, int channelId)
    {
        if (!isConnected)
            return;

        //Debug.Log("Sending :" + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);

        byte[] msg2 = new byte[msg.Length + sizeof(bool)];
        Array.Copy(BitConverter.GetBytes(false), 0, msg2, 0, sizeof(bool));
        Array.Copy(msg, 0, msg2, sizeof(bool), message.Length * sizeof(char));

        NetworkTransport.Send(hostId, connectionId, channelId, msg2, msg2.Length , out error);
        
    }
    private void Send(byte[] message, int channelId)
    {
        if (!isConnected)
            return;
        byte[] msg = new byte[message.Length + sizeof(bool)];
        Array.Copy(BitConverter.GetBytes(true), 0, msg, 0, sizeof(bool));
        Array.Copy(message, 0, msg, sizeof(bool), message.Length);

        NetworkTransport.Send(hostId, connectionId, channelId, msg, msg.Length , out error);
    }


    // Snapshot interpolation
    public void SendInput(byte[] inputMessage)
    {
        //string msg = "INPUT|" + input.left + "%" + input.right + "%" + input.forward + "%" + input.back;
        startTime = Time.time;
        Send(inputMessage, reliableChannel);
    }
}

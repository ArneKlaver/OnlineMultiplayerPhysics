using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;



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

[Serializable]
public class ChildNetworkObject
{
    public GameObject Object;
    public int ChildObjectId { get; set; }
    public bool SendPosition = false;
    public bool SendRotation = false;

    public ChildNetworkObject(GameObject obj, int id)
    {
        Object = obj;
        ChildObjectId = id;
    }
}

public class StateUpdateManager : MonoBehaviour {
    public List<NetworkObject> Objects { set; get; } 
   // private int ObjectsPerPacket = 10;
    Server serverScript;
    float sendRate = 15;

    // Use this for initialization
    void Start () {
        Objects = new List<NetworkObject>();
        serverScript = GameObject.Find("NetworkManager").GetComponent<Server>();
        float repeatRate = 60 / sendRate /60;
        InvokeRepeating("SendStates", 0, repeatRate);//repeatRate);
    }
	
	// Update is called once per frame
	void Update () {
        //SendStates();
    }

    public void DestroyObject(NetworkObject networkObjScript,int objId)
    {
        //int objId = Objects.Find(x => x.Object == networkObjScript.gameObject).ObjectId;
        string msg = "DEL|" + objId;

        serverScript.SendToAll(msg, 1);
    }
    public void AddObject(GameObject netObject, ObjectType objType, bool IsPlayer = false, int playerCnnId = 0)
    {
    
        // add gameObject
        // go over the objects and see if there is a open id
        int id = 0;
        for (id = 0; id < Objects.Count; id++)
        {
            bool found = true;
            foreach (NetworkObject NetworkObject in Objects)
            {
                if (NetworkObject.ObjectId == id)
                {
                    found = false;
                    break ;
                }
            }

            if (found)
            {
                break ;
            }
        }

       // Debug.Log("addObject with id  " + id);
        netObject.GetComponent<NetworkObject>().ObjectId = id;

        if (IsPlayer)
        {
            if (id == -1)
            {
                Debug.Log("ERROR!!!! player id is -1");
            }
            // if the object is a player send to that player this object is him
            string idMsg = "OBJID|" + id + "|";
            serverScript.SendToClient(idMsg, playerCnnId, 1);
            Debug.Log(idMsg);
        }
        Objects.Add(netObject.GetComponent<NetworkObject>());

        // send spawn object to clients
        //  spawn - id  - object Type - position - rotation 
        string msg = "SPAWN|";
        msg += id + "|";
        msg += (int)objType + "|";
        msg += Math.Round(gameObject.transform.position.x ,2) +":" + Math.Round(gameObject.transform.position.y , 2)+ ":" + Math.Round(gameObject.transform.position.z , 2)+ "|";
        msg += Math.Round(gameObject.transform.rotation.x , 2) +":" + Math.Round(gameObject.transform.rotation.y ,2)+ ":" + Math.Round(gameObject.transform.rotation.z ,2)+ "|";
       // Debug.Log(msg);

        serverScript.SendToAll(msg , 1);
    }
    private void SendStates()
    {
        // STATE|objId|sendPos?|pos|sendRot?|rot|sendVel?|vel|
        // STATE|4||1|0:0:0||1|0:0:0||1|0:0:0| -> POS ROT VEL
        // string msg = "STATE|";
        int msgOffset = 0;
        int totalChildCount = 0;
        foreach (NetworkObject obj in Objects)
        {
            totalChildCount += obj.ChildNetworkObjects.Count;
        }

        //int maxSize = (Objects.Count * ((sizeof(Int16) * 6) + (sizeof(bool) * 2) + sizeof(Int16))) + (sizeof(Int16) * 2);
        int maxSize = 0;
        maxSize += (Objects.Count * (((sizeof(Int16) * 3) + (sizeof(float) * 3)) + (sizeof(bool) * 2) + (sizeof(Int16) * 2))); // object states
        //  PARENT                        positions          rotations             sendpos and sendrot   objectId
        maxSize += (sizeof(Int16) * 2); // amount of objects

        maxSize += (sizeof(Int16));  // child count
        maxSize += (Objects.Count * (((sizeof(Int16) * 3) + (sizeof(float) * 3)) + (sizeof(bool) * 2) + (sizeof(Int16) * 2))); // object states
        // CHILD                          positions          rotations             sendpos and sendrot     childObjectId


        byte[] msgBuffer = new byte[maxSize];

        // what message is this? STATE -> 2
        Array.Copy(BitConverter.GetBytes(2), 0, msgBuffer, 0, sizeof(Int16));
        msgOffset += sizeof(Int16);
        // amount of objects
        Array.Copy(BitConverter.GetBytes(Objects.Count), 0, msgBuffer, msgOffset, sizeof(Int16));
        msgOffset += sizeof(Int16);


        int sizeOffParentState = 0;
        for (int i = 0; i < Objects.Count ; i++)
        {
            // if the object does not exist skip and delete
            if (!Objects[i].Object.gameObject)
            {
                //Debug.Log("object does not have a gameObject atached");
                Objects.Remove(Objects[i]);
                continue;
            }
            if (Objects[i].IsAtRest)
            {
                continue;
            }
            // send  position
            byte[] posbuff = new byte[sizeof(Int16) * 3];
            byte[] sendPos = new byte[sizeof(bool)];
            sizeOffParentState += 1; // sendpos
            if (Objects[i].SendPosition)
            {
                sizeOffParentState += sizeof(Int16) * 3;

                Array.Copy(BitConverter.GetBytes(true), 0, sendPos, 0, sizeof(bool));

                Vector3 pos = Objects[i].Object.transform.position;
                Array.Copy(BitConverter.GetBytes((int)(pos.x * 100)), 0, posbuff, 0 * sizeof(Int16), sizeof(Int16));
                Array.Copy(BitConverter.GetBytes((int)(pos.y * 100)), 0, posbuff, 1 * sizeof(Int16), sizeof(Int16));
                Array.Copy(BitConverter.GetBytes((int)(pos.z * 100)), 0, posbuff, 2 * sizeof(Int16), sizeof(Int16));
            }
            else
            {
                Array.Copy(BitConverter.GetBytes(false), 0, sendPos, 0, sizeof(bool));
            }
            // send rotation
            byte[] rotbuff = new byte[sizeof(float) * 3];
            byte[] sendRot = new byte[sizeof(bool)];

            sizeOffParentState += 1; // sendrot
            if (Objects[i].SendRotation)
            {
                sizeOffParentState += sizeof(float) * 3;

                Array.Copy(BitConverter.GetBytes(true), 0, sendRot, 0, sizeof(bool));

                Vector3 rot = Objects[i].Object.transform.rotation.eulerAngles;
                Array.Copy(BitConverter.GetBytes(rot.x), 0, rotbuff, 0 * sizeof(float), sizeof(float));
                Array.Copy(BitConverter.GetBytes(rot.y), 0, rotbuff, 1 * sizeof(float), sizeof(float));
                Array.Copy(BitConverter.GetBytes(rot.z), 0, rotbuff, 2 * sizeof(float), sizeof(float));
            }
            else
            {
                Array.Copy(BitConverter.GetBytes(false), 0, sendRot, 0, sizeof(bool));
            }

            sizeOffParentState += sizeof(Int16); // state
            sizeOffParentState += sizeof(Int16); // object id
            sizeOffParentState += sizeof(Int16); // amount of objects


            // object id
            Array.Copy(BitConverter.GetBytes(Objects[i].ObjectId), 0, msgBuffer, msgOffset, sizeof(Int16));
            msgOffset += sizeof(Int16);
            //position
            Array.Copy(sendPos, 0, msgBuffer, msgOffset, sizeof(bool));
            msgOffset += sizeof(bool);
            if (Objects[i].SendPosition)
            {
                Array.Copy(posbuff, 0, msgBuffer, msgOffset, posbuff.Length);
                msgOffset += posbuff.Length;
            }
            //rotation
            Array.Copy(sendRot, 0, msgBuffer, msgOffset, sizeof(bool));
            msgOffset += sizeof(bool);
            if (Objects[i].SendRotation)
            {
                Array.Copy(rotbuff, 0, msgBuffer, msgOffset, rotbuff.Length);
                msgOffset += rotbuff.Length;
            }


            // child objects
            // child count
            Array.Copy(BitConverter.GetBytes(Objects[i].ChildNetworkObjects.Count), 0, msgBuffer, msgOffset, sizeof(Int16));
            msgOffset += sizeof(Int16);

            if (Objects[i].ChildNetworkObjects.Count > 0)
            {
                for (int j = 0; j < Objects[i].ChildNetworkObjects.Count; j++)
                {
                    GameObject childObj = Objects[i].ChildNetworkObjects[j].Object;

                    int sizeOffchildState = 0;

                    // send  position
                    byte[] childposbuff = new byte[sizeof(Int16) * 3];
                    byte[] childsendPos = new byte[sizeof(bool)];
                    sizeOffchildState += 1; // sendpos
                    if (Objects[i].ChildNetworkObjects[j].SendPosition)
                    {
                        sizeOffchildState += sizeof(Int16) * 3;

                        Array.Copy(BitConverter.GetBytes(true), 0, childsendPos, 0, sizeof(bool));

                        Vector3 pos = childObj.transform.position;
                        Array.Copy(BitConverter.GetBytes((int)(pos.x * 100)), 0, childposbuff, 0 * sizeof(Int16), sizeof(Int16));
                        Array.Copy(BitConverter.GetBytes((int)(pos.y * 100)), 0, childposbuff, 1 * sizeof(Int16), sizeof(Int16));
                        Array.Copy(BitConverter.GetBytes((int)(pos.z * 100)), 0, childposbuff, 2 * sizeof(Int16), sizeof(Int16));
                    }
                    else
                    {
                        Array.Copy(BitConverter.GetBytes(false), 0, childsendPos, 0, sizeof(bool));
                    }
                    // send rotation
                    byte[] childrotbuff = new byte[sizeof(float) * 3];
                    byte[] childsendRot = new byte[sizeof(bool)];

                    sizeOffchildState += 1; // sendrot
                    if (Objects[i].ChildNetworkObjects[j].SendRotation)
                    {
                        sizeOffchildState += sizeof(float) * 3;

                        Array.Copy(BitConverter.GetBytes(true), 0, childsendRot, 0, sizeof(bool));

                        Vector3 rot = childObj.transform.rotation.eulerAngles;
                        Array.Copy(BitConverter.GetBytes(rot.x), 0, childrotbuff, 0 * sizeof(float), sizeof(float));
                        Array.Copy(BitConverter.GetBytes(rot.y), 0, childrotbuff, 1 * sizeof(float), sizeof(float));
                        Array.Copy(BitConverter.GetBytes(rot.z), 0, childrotbuff, 2 * sizeof(float), sizeof(float));
                    }
                    else
                    {
                        Array.Copy(BitConverter.GetBytes(false), 0, childsendRot, 0, sizeof(bool));
                    }

                    sizeOffchildState += sizeof(Int16); // object id
                    sizeOffchildState += sizeof(Int16); // amount of objects


                    // object id
                    Array.Copy(BitConverter.GetBytes(Objects[i].ChildNetworkObjects[j].ChildObjectId), 0, msgBuffer, msgOffset, sizeof(Int16));
                    msgOffset += sizeof(Int16);
                    //position
                    Array.Copy(childsendPos, 0, msgBuffer, msgOffset, sizeof(bool));
                    msgOffset += sizeof(bool);
                    if (Objects[i].ChildNetworkObjects[j].SendPosition)
                    {
                        Array.Copy(childposbuff, 0, msgBuffer, msgOffset, posbuff.Length);
                        msgOffset += childposbuff.Length;
                    }
                    //rotation
                    Array.Copy(childsendRot, 0, msgBuffer, msgOffset, sizeof(bool));
                    msgOffset += sizeof(bool);
                    if (Objects[i].ChildNetworkObjects[j].SendRotation)
                    {
                        Array.Copy(childrotbuff, 0, msgBuffer, msgOffset, rotbuff.Length);
                        msgOffset += childrotbuff.Length;
                    }
                }

            }
        }
        

        if (Objects.Count > 0)
        {
            byte[] message = new byte[msgOffset];
            Array.Copy(msgBuffer, message, msgOffset);
            serverScript.SendToAll(message);
        }
    }

    public void SendSceneData(int cnnId)
    {
        int msgOffset = 0;
        int maxSize = (Objects.Count * (((sizeof(Int16) * 3) + (sizeof(float) * 3)) + (sizeof(bool) * 2) + sizeof(Int16)*2)) + (sizeof(Int16) * 2);
        byte[] msgBuffer = new byte[maxSize];

        // what message is this? STATE -> 2
        Array.Copy(BitConverter.GetBytes(5), 0, msgBuffer, 0, sizeof(Int16));
        msgOffset += sizeof(Int16);
        // amount of objects
        Array.Copy(BitConverter.GetBytes(Objects.Count), 0, msgBuffer, msgOffset, sizeof(Int16));
        msgOffset += sizeof(Int16);

        for (int i = 0; i < Objects.Count; i++)
        {
            int sizeOffParentState = 0;

                // send  position
                byte[] posbuff = new byte[sizeof(Int16) * 3];

                    sizeOffParentState += sizeof(Int16) * 3;


                    Vector3 pos = Objects[i].Object.transform.position;
                    Array.Copy(BitConverter.GetBytes((Int16)(pos.x * 100)), 0, posbuff, 0 * sizeof(Int16), sizeof(Int16));
                    Array.Copy(BitConverter.GetBytes((Int16)(pos.y * 100)), 0, posbuff, 1 * sizeof(Int16), sizeof(Int16));
                    Array.Copy(BitConverter.GetBytes((Int16)(pos.z * 100)), 0, posbuff, 2 * sizeof(Int16), sizeof(Int16));
      
                // send rotation
                byte[] rotbuff = new byte[sizeof(float) * 3];

                    sizeOffParentState += sizeof(float) * 3;

                    Vector3 rot = Objects[i].Object.transform.rotation.eulerAngles;
                    Array.Copy(BitConverter.GetBytes((rot.x )), 0, rotbuff, 0 * sizeof(float), sizeof(float));
                    Array.Copy(BitConverter.GetBytes((rot.y )), 0, rotbuff, 1 * sizeof(float), sizeof(float));
                    Array.Copy(BitConverter.GetBytes((rot.z )), 0, rotbuff, 2 * sizeof(float), sizeof(float));
      

                sizeOffParentState += sizeof(Int16); // state
                sizeOffParentState += sizeof(Int16); // object id
                sizeOffParentState += sizeof(Int16); // amount of objects




                // object id
                Array.Copy(BitConverter.GetBytes(Objects[i].ObjectId), 0, msgBuffer, msgOffset, sizeof(Int16));
                msgOffset += sizeof(Int16);


            // send the object type
            Array.Copy(BitConverter.GetBytes((Int16)Objects[i].Object.GetComponent<NetworkObject>().objType), 0, msgBuffer, msgOffset, sizeof(Int16));
            msgOffset += sizeof(Int16);

            //pos
            Array.Copy(posbuff, 0, msgBuffer, msgOffset, posbuff.Length);
                    msgOffset += posbuff.Length;
                

             //rot
             Array.Copy(rotbuff, 0, msgBuffer, msgOffset, rotbuff.Length);
             msgOffset += rotbuff.Length;
               
          }
        
        // send the message if there is a message
        if (Objects.Count > 0)
        {
            serverScript.SendToClient(msgBuffer, cnnId, 3);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class UpdateStateManager : MonoBehaviour {
    Dictionary<int,Object> objects = new Dictionary<int,Object>();
    List<byte[]> StateUpdateMessages = new List<byte[]>();
  //  private int stateBufferSize = 10;
  //  private int sendrate = 30; 
    // Use this for initialization
    void Start () {
        //float repeatRate = 60.0f / sendrate / 60;
       // InvokeRepeating("UpdateStates", 0, repeatRate);//repeatRate);
    }
    public void AddObject(int objId, GameObject gameObject)
    {

        objects.Add(objId, new Object(objId, gameObject));
    }
    public void DestroyObject(int objId)
    {
        Object objToDestroy;
        objects.TryGetValue(objId , out objToDestroy); // objects.Find(x => x.ObjectId == objId);
        // remove from scene
        DestroyImmediate(objToDestroy.GameObject);
        // remove from list
        objects.Remove(objId);
    }

    public void UpdateStates(string message)
    {
        // plit per object

        message = message.Trim('%');
        string[] perObjectData;
        perObjectData = message.Split('%');
        int index = 1;

        foreach (string objectData in perObjectData)
        {
          // Get the index from the object that nees to be updated
            //Debug.Log(objectData);
            string[] splitObjectData;
            splitObjectData = objectData.Split('|');
            int id = int.Parse(splitObjectData[index++]);


            // finding object to update
            Object obj = null;
            objects.TryGetValue(id, out obj); ;// = objects.Find(l => l.ObjectId == id);
            if (obj == null)
            {
                Debug.Log("No object found with id " + id + " UpdateStateManager. This might be normal a few times per object");
                index = 0;
                continue;
            }

            // get positions if the first value is 1
            Vector3 pos = new Vector3();
            if (int.Parse(splitObjectData[index++]) == 1)
            {
                string[] posData;
                posData = splitObjectData[index++].Split(':');

                if (posData.Length < 3)
                {
                    Debug.Log("ERROR not enoug data in UpdateStates  positions : " + splitObjectData.Length + " " + message);
                    continue;
                }
                pos.x = float.Parse(posData[0]);
                pos.y = float.Parse(posData[1]);
                pos.z = float.Parse(posData[2]);

                obj.GameObject.transform.position = Vector3.Lerp(obj.GameObject.transform.position, pos , 1f);

            }
            // get rotations
            Vector3 rot = new Vector3();
            if (int.Parse(splitObjectData[index++]) == 1)
            {
                string[] rotData;
                rotData = splitObjectData[index++].Split(':');

                if (rotData.Length < 3)
                {
                    Debug.Log("ERROR not enoug data in UpdateStates  rotation : " + splitObjectData.Length + " " + message);
                    continue;
                }
                rot.x = float.Parse(rotData[0]);
                rot.y = float.Parse(rotData[1]);
                rot.z = float.Parse(rotData[2]);

                obj.GameObject.transform.rotation =  Quaternion.Lerp(obj.GameObject.transform.rotation,Quaternion.Euler(rot),1f);
            }

            // get velocitys

            Vector3 vel = new Vector3();
            if (int.Parse(splitObjectData[index++]) == 1)
            {
                string[] velData;
                velData = splitObjectData[index++].Split(':');

                if (velData.Length < 3)
                {
                    Debug.Log("ERROR not enoug data in UpdateStates  velocity : " + splitObjectData.Length + " " + message);
                    continue;
                }
                vel.x = float.Parse(velData[0]);
                vel.y = float.Parse(velData[1]);
                vel.z = float.Parse(velData[2]);
            }

            int childCount = int.Parse(splitObjectData[index++]);
            if (obj.GameObject.GetComponent<NetworkObject>().ChildNetworkObjects.Count != childCount)
            {
                Debug.Log("Wrong amount of child objects !!!");
                return;
            }
            for (int i = 0; i < childCount; i++)
            {
                int childId = int.Parse(splitObjectData[index++]);
                GameObject childObject = obj.GameObject.GetComponent<NetworkObject>().ChildNetworkObjects[childId];

                    // get positions if the first value is 1
                Vector3 childPos = new Vector3();
                if (int.Parse(splitObjectData[index++]) == 1)
                {
                    string[] posData;
                    posData = splitObjectData[index++].Split(':');

                    if (posData.Length < 3)
                    {
                        Debug.Log("ERROR not enoug data in UpdateStates  positions : " + splitObjectData.Length + " " + message);
                        continue;
                    }
                    childPos.x = float.Parse(posData[0]);
                    childPos.y = float.Parse(posData[1]);
                    childPos.z = float.Parse(posData[2]);

                    childObject.transform.position = Vector3.Lerp(childObject.transform.position, childPos, 0.95f);

                }
                // get rotations
                Vector3 childRot = new Vector3();
                if (int.Parse(splitObjectData[index++]) == 1)
                {
                    string[] rotData;
                    rotData = splitObjectData[index++].Split(':');

                    if (rotData.Length < 3)
                    {
                        Debug.Log("ERROR not enoug data in UpdateStates  rotation : " + splitObjectData.Length + " " + message);
                        continue;
                    }
                    childRot.x = float.Parse(rotData[0]);
                    childRot.y = float.Parse(rotData[1]);
                    childRot.z = float.Parse(rotData[2]);

                    childObject.transform.rotation = Quaternion.Lerp(childObject.transform.rotation, Quaternion.Euler(childRot), 0.95f);
                }

            }


            index = 0;
        }
    }

    public void AddUpdateState(byte[] message)
    {
        StateUpdateMessages.Add(message);
    }

    public void UpdateStates(byte[] message)
    {
        // save message in list to acount for jitter

      //  if (StateUpdateMessages.Count > stateBufferSize)
      //  {
      //
      //      byte[] message = StateUpdateMessages[0];
            // plit per object
            int messageindex = 0;
            messageindex += sizeof(bool); // first val

            messageindex += sizeof(Int16); // state

            int objectCount = BitConverter.ToInt16(message, messageindex); // objCount
            messageindex += sizeof(Int16);

            for (int i = 0; i < objectCount; i++)
            {
                int objId = BitConverter.ToInt16(message, messageindex);
                messageindex += sizeof(Int16);

                // finding object to update
                Object obj = null;

                objects.TryGetValue(objId, out obj);
                if (obj == null)
                {
                  //  Debug.Log("No object found with id " + objId + " UpdateStateManager. This might be normal a few times per object");
                    bool hasP = BitConverter.ToBoolean(message, messageindex);
                    messageindex += sizeof(bool);
                    if (hasP)
                    {
                        messageindex += sizeof(Int16) * 3;
                    }
                    bool hasR = BitConverter.ToBoolean(message, messageindex);
                    messageindex += sizeof(bool);
                    if (hasR)
                    {
                        messageindex += sizeof(Int16) * 3;
                    }
                    int childC = BitConverter.ToInt16(message, messageindex);
                    messageindex += sizeof(Int16);
                    for (int j = 0; j < childC; j++)
                    {
                        bool hasPo = BitConverter.ToBoolean(message, messageindex);
                        messageindex += sizeof(bool);
                        if (hasPo)
                        {
                            messageindex += sizeof(Int16) * 3;
                        }
                        bool hasRo = BitConverter.ToBoolean(message, messageindex);
                        messageindex += sizeof(bool);
                        if (hasRo)
                        {
                            messageindex += sizeof(Int16) * 3;
                        }
                    }
                    continue;
                }


                Vector3 pos = new Vector3();
                Vector3 rot = new Vector3();

                bool hasPos = BitConverter.ToBoolean(message, messageindex);
                messageindex += sizeof(bool);
                if (hasPos)
                {
                    pos.x = BitConverter.ToInt16(message, messageindex) / 100.0f;
                    messageindex += sizeof(Int16);
                    pos.y = BitConverter.ToInt16(message, messageindex) / 100.0f;
                    messageindex += sizeof(Int16);
                    pos.z = BitConverter.ToInt16(message, messageindex) / 100.0f;
                    messageindex += sizeof(Int16);

                    obj.GameObject.transform.position = Vector3.Slerp(obj.GameObject.transform.position, pos, 1.0f);

                }

                bool hasRot = BitConverter.ToBoolean(message, messageindex);
                messageindex += sizeof(bool);
                if (hasRot)
                {
                    rot.x = BitConverter.ToSingle(message, messageindex);
                    messageindex += sizeof(float);
                    rot.y = BitConverter.ToSingle(message, messageindex);
                    messageindex += sizeof(float);
                    rot.z = BitConverter.ToSingle(message, messageindex);
                    messageindex += sizeof(float);

                    obj.GameObject.transform.rotation = Quaternion.Slerp(obj.GameObject.transform.rotation, Quaternion.Euler(rot), 1.0f);
                }

                int childCOunt = BitConverter.ToInt16(message, messageindex);
                messageindex += sizeof(Int16);

                if (obj.GameObject.GetComponent<NetworkObject>().ChildNetworkObjects.Count != childCOunt)
                {
                    Debug.Log("Wrong amount of child objects !!!");
                    return;
                }

                // child objects
                for (int j = 0; j < childCOunt; j++)
                {
                    int ChildobjId = BitConverter.ToInt16(message, messageindex);
                    messageindex += sizeof(Int16);

                    GameObject childObject = obj.GameObject.GetComponent<NetworkObject>().ChildNetworkObjects[j];

                    bool childHasPos = BitConverter.ToBoolean(message, messageindex);
                    messageindex += sizeof(bool);

                    if (childHasPos)
                    {
                        pos.x = BitConverter.ToInt16(message, messageindex) / 100.0f;
                        messageindex += sizeof(Int16);
                        pos.y = BitConverter.ToInt16(message, messageindex) / 100.0f;
                        messageindex += sizeof(Int16);
                        pos.z = BitConverter.ToInt16(message, messageindex) / 100.0f;
                        messageindex += sizeof(Int16);

                        childObject.transform.position = Vector3.Slerp(childObject.transform.position, pos, 1.0f);
                    }

                    bool childRasRot = BitConverter.ToBoolean(message, messageindex);
                    messageindex += sizeof(bool);
                    if (childRasRot)
                    {
                        rot.x = BitConverter.ToSingle(message, messageindex);
                        messageindex += sizeof(float);
                        rot.y = BitConverter.ToSingle(message, messageindex);
                        messageindex += sizeof(float);
                        rot.z = BitConverter.ToSingle(message, messageindex);
                        messageindex += sizeof(float);

                        childObject.transform.rotation = Quaternion.Slerp(childObject.transform.rotation, Quaternion.Euler(rot), 1.0f);
                    }

                  //  StateUpdateMessages.Remove(StateUpdateMessages[0]);
                }
         //   }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour 
{
    private StateUpdateManager stateUpdateManagerScript;

    public ObjectType objType = ObjectType.NoObject;
    public bool isPlayer = false;
    public int cnnId = -1;
    public GameObject Object { get; set; }

    public string PlayerName { get; set; }
    public int ObjectId { get; set; }
    public bool SendPosition = false;
    public bool SendRotation = false;
    public bool IsAtRest = false;

    public List<ChildNetworkObject> ChildNetworkObjects = new List<ChildNetworkObject>();



    // Use this for initialization
    void Start()
    {
        Object = gameObject;
        // give a id to the childs
        for (int i = 0; i < ChildNetworkObjects.Count; i++)
        {
            ChildNetworkObjects[i].ChildObjectId = i;
        }


        // add this object to the manager
        stateUpdateManagerScript = GameObject.Find("NetworkManager").GetComponent<StateUpdateManager>();
        stateUpdateManagerScript.AddObject(gameObject, objType , isPlayer , cnnId);
        if (objType == ObjectType.NoObject)
        {
            Debug.LogWarning("objType noObject  :  " + gameObject.name);
        }
    }

    private void OnDestroy()
    {
        stateUpdateManagerScript.DestroyObject(this,ObjectId);
    }
    // Update is called once per frame
    void Update()
    {
        IsAtRest = gameObject.GetComponent<Rigidbody>().IsSleeping();
    }
}

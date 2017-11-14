using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour {

    private UpdateStateManager stateUpdateManagerScript;
    public int ObjectId;
    public List<GameObject> ChildNetworkObjects = new List<GameObject>();

    // Use this for initialization
    void Start()
    {
        stateUpdateManagerScript = GameObject.Find("NetworkManager").GetComponent<UpdateStateManager>();
        stateUpdateManagerScript.AddObject(ObjectId, gameObject);

    }

    // Update is called once per frame
    void Update()
    {

    }

}

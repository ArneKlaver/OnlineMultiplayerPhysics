using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInput : MonoBehaviour {

    static Server serverScript;
    private void Start()
    {

        serverScript = GameObject.Find("NetworkManager").GetComponent<Server>();

    }


    public static bool IsKeyPressed(KeyCode keyCode , int cnnId)
    {
        List<ButtonState> Inputs = serverScript.GetInput(cnnId);

        ButtonState button = Inputs.Find(x => x.Key == keyCode);
        if (button == null)
        {
            Debug.Log("ERROR!!! Key not found");
            return false;
        }
        return button.IsPressed;
    }
}

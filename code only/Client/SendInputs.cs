using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
public class SendInputs : MonoBehaviour {
   
    private bool hasInput = false;
    private client net;
    private List<ButtonState> keysToCheck = new List<ButtonState>();

	// Use this for initialization
	void Start () {
        net = FindObjectOfType<client>();
        // has to be in the correct order
        keysToCheck.Add(new ButtonState(KeyCode.A, false));
        keysToCheck.Add(new ButtonState(KeyCode.W, false));
        keysToCheck.Add(new ButtonState(KeyCode.S, false));
        keysToCheck.Add(new ButtonState(KeyCode.D, false));
        keysToCheck.Add(new ButtonState(KeyCode.Q, false));
        keysToCheck.Add(new ButtonState(KeyCode.E, false));
        keysToCheck.Add(new ButtonState(KeyCode.Z, false));
        keysToCheck.Add(new ButtonState(KeyCode.Space, false));
        keysToCheck.Add(new ButtonState(KeyCode.Mouse0, false));
        keysToCheck.Add(new ButtonState(KeyCode.Mouse1, false));

    }

    // Update is called once per frame
    void Update() {
        
        hasInput = false;
        byte[] inputs = new byte[keysToCheck.Count * sizeof(bool) + sizeof(Int16)];
        int index = 0;
        Array.Copy(BitConverter.GetBytes(7) , 0, inputs , index , sizeof(Int16));
        index += sizeof(Int16);

        // if there is a input save this and send the results to the server
        foreach (ButtonState key in keysToCheck)
        {
            // down input : 1
            // up input : 0
            // not optimal for string but ok for bitpacking
            if (Input.GetKeyDown(key.Key))
            {
                key.IsPressed = true;
                hasInput = true;
            }
  
            if (Input.GetKeyUp(key.Key))
            {
                key.IsPressed = false;
                hasInput = true;
            }

            if (key.IsPressed)
            {
                Array.Copy(BitConverter.GetBytes(true), 0, inputs, index, sizeof(bool));
                index += sizeof(bool);
            }
            else
            {
                Array.Copy(BitConverter.GetBytes(false), 0, inputs, index, sizeof(bool));
                index += sizeof(bool);
            }
            

        }

        // send the input if there is a input
        if (hasInput)
        {
            net.SendInput(inputs);
        }
    }
}

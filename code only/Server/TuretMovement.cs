using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TuretMovement : MonoBehaviour {

    public float speed = 10;
    public Vector3 movement;    // Use this for initialization
    public Vector3 rotation;    // Use this for initialization
    List<ButtonState> Inputs = new List<ButtonState>();
    

    void Start()
    {


      // Inputs.Add(new ButtonState(KeyCode.A, false));
      // Inputs.Add(new ButtonState(KeyCode.W, false));
      // Inputs.Add(new ButtonState(KeyCode.S, false));
      // Inputs.Add(new ButtonState(KeyCode.D, false));
      // Inputs.Add(new ButtonState(KeyCode.Q, false));
      // Inputs.Add(new ButtonState(KeyCode.E, false));
      // Inputs.Add(new ButtonState(KeyCode.Space, false));
      // Inputs.Add(new ButtonState(KeyCode.Mouse0, false));
      // Inputs.Add(new ButtonState(KeyCode.Mouse1, false));
    }

    // Update is called once per frame

    void Update()
    {
        Vector3 rot = new Vector3();
        if (Inputs.Count != 9)
        {
            Debug.Log("Not enoug inputs");
            return;
        }
            // KeyCode.A:
            if (Inputs[0].IsPressed)
            {
             // rot.y += speed * Time.deltaTime;
            }
            // KeyCode.W:
            if (Inputs[1].IsPressed)
            {

            }
            // KeyCode.S:
            if (Inputs[2].IsPressed)
            {

            }
            // KeyCode.D:
            if (Inputs[3].IsPressed)
            {
              //rot.y -= speed * Time.deltaTime;
            }
            // KeyCode.Q:
            if (Inputs[4].IsPressed)
            {

            }
            // KeyCode.E:
            if (Inputs[5].IsPressed)
            {
                
            }
            // KeyCode.Space:
            if (Inputs[6].IsPressed)
            {
                
            }
            // KeyCode.mouse0:
            if (Inputs[7].IsPressed)
            {

            }
            // KeyCode.mouse1:
            if (Inputs[8].IsPressed)
            {

            }

        gameObject.GetComponent<Rigidbody>().transform.Rotate(rot);
    }

    public void MovePlayer(List<ButtonState> inputs)
    {
        Inputs = inputs;



    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankeMovement : MonoBehaviour {
    public float MoveSpeed = 5;
    public float RotateSpeed = 50;

    int cnnId = -1;
    // Use this for initialization
    void Start () {
        cnnId = gameObject.GetComponent<NetworkObject>().cnnId;
	}
	
	// Update is called once per frame
	void Update() {

        if (NetworkInput.IsKeyPressed(KeyCode.Z, cnnId))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * MoveSpeed);
        }
        if (NetworkInput.IsKeyPressed(KeyCode.S, cnnId))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime * MoveSpeed);
        }
        if (NetworkInput.IsKeyPressed(KeyCode.Q, cnnId))
        {
            transform.Rotate(-Vector3.up * Time.deltaTime * RotateSpeed);
        }
        if (NetworkInput.IsKeyPressed(KeyCode.D, cnnId))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * RotateSpeed);
        }
        if (NetworkInput.IsKeyPressed(KeyCode.Space, cnnId))
        {
            gameObject.GetComponent<Shoot>().Fire();
        }
    }
}

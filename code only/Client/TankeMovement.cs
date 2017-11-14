using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankeMovement : MonoBehaviour {
    public float MoveSpeed = 5;
    public float RotateSpeed = 50;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update() {
        if (Input.GetKey(KeyCode.A))
        {

        }
        if (Input.GetKey(KeyCode.E))
        {

        }
        if (Input.GetKey(KeyCode.Z))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * MoveSpeed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(-Vector3.forward * Time.deltaTime * MoveSpeed);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(-Vector3.up * Time.deltaTime * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.Space))
        {

        }
    }
}

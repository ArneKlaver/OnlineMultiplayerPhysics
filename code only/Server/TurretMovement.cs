using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretMovement : MonoBehaviour {
    public bool AutoAim = false;
    public GameObject Target = null;
    private GameObject Turret;
	// Use this for initialization
	void Start () {
        Turret = gameObject.transform.Find("Turret").gameObject;
        Target = GameObject.FindGameObjectWithTag("Barrel").gameObject;

    }
	
	// Update is called once per frame
	void Update () {
        if (!Target)
        {
            Target = GameObject.FindGameObjectWithTag("Barrel").gameObject;
        }
        if (Target == null)
        {
            Debug.Log("No Target !!!");
            return;
        }
        if (AutoAim)
        {
            Vector3 targetPos = Target.transform.position + (Target.transform.forward * 1);
            targetPos.y = Turret.transform.position.y;
            Turret.transform.LookAt(targetPos);
        }


	}
}

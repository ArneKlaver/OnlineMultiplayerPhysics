using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    public bool AutoFire = false;
    public GameObject bullet;
    public Transform bulletStart;
    public float ReloadTime = 1f;
    private float FireCounter = 0;
    public float LifeTime = 5;
    public float bulletPower = 1000;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (AutoFire)
        {
            FireCounter += Time.deltaTime;
            if (FireCounter >= ReloadTime)
            {
                FireCounter = 0;
                Fire();
            }
        }
     
    }

    public void Fire()
    {
        GameObject instBullet = Instantiate<GameObject>(bullet, bulletStart.position, bulletStart.localRotation);
        instBullet.GetComponent<Rigidbody>().AddForce(bulletStart.forward * bulletPower);
        Destroy(instBullet, LifeTime);
    }
}

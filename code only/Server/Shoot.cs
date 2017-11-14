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

    private bool canFire = true;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (AutoFire)
        {
            Fire();
        }
        FireCounter += Time.deltaTime;
        if (FireCounter >= ReloadTime)
        {
            canFire = true;
        }
    }

    public void Fire()
    {
        if (canFire)
        {
            FireCounter = 0;
            canFire = false;
            GameObject instBullet = Instantiate<GameObject>(bullet, bulletStart.position, bulletStart.localRotation);
            instBullet.GetComponent<Rigidbody>().AddForce(bulletStart.forward * bulletPower);
            Destroy(instBullet, LifeTime);
        }    
    }
}

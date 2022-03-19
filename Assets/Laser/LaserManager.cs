using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserManager : MonoBehaviour
{
    public static LaserManager Instance;
    public GameObject laserPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public Laser ShotLaser(Transform spawner, Transform triggerer, Vector3 rotation, float speed, string property)
    {
        Laser laser = Instantiate(laserPrefab, spawner.position, Quaternion.Euler(spawner.forward)).GetComponent<Laser>();
        laser.transform.rotation = Quaternion.Euler(rotation);
        laser.spanwer = spawner;
        laser.source = triggerer;
        laser.speed = speed;
        laser.property = property;
        return laser;
    }
}

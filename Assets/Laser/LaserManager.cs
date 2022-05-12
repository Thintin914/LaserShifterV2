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
        SoundManagerScript.PlaySound("laser");
        Laser laser = Instantiate(laserPrefab, spawner.position, Quaternion.Euler(spawner.forward)).GetComponent<Laser>();
        laser.transform.rotation = Quaternion.Euler(rotation);

        BoxCollider box = spawner.GetComponent<BoxCollider>();
        Vector3 boxPt = box.bounds.size;
        float longest = 0;
        float centerOffset = 0;
        if (boxPt.x > longest)
        {
            longest = boxPt.x;
            centerOffset = box.center.x;
        }
        if (boxPt.y > longest)
        {
            longest = boxPt.y;
            centerOffset = box.center.y;
        }
        if (boxPt.z > longest)
        {
            longest = boxPt.z;
            centerOffset = box.center.z;
        }
        laser.transform.position = box.bounds.center + longest * laser.transform.forward * 0.5f;
        laser.transform.position += (centerOffset + 0.2f) * laser.transform.forward;

        laser.spanwer = spawner;
        laser.source = triggerer;
        laser.speed = speed;
        laser.property = property;

        laser.levelInfo = GameUI.Instance.levelInfo.text;
        return laser;
    }
}

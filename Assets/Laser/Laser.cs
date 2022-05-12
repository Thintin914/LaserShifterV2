using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public float speed;
    public Transform spanwer;
    public Transform source;
    public string property;
    public string levelInfo = null;

    private float startTime, endTime;
    private bool isReady = false;
    private void Start()
    {
        BoxCollider box = spanwer.GetComponent<BoxCollider>();
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
        if (boxPt.z > longest){ 
            longest = boxPt.z;
            centerOffset = box.center.z;
        }
        transform.position += (longest + centerOffset) * transform.forward;
        startTime = Time.timeSinceLevelLoad;
        endTime = Time.timeSinceLevelLoad + 8;
        isReady = true;
    }

    private void FixedUpdate()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
        startTime += Time.deltaTime;
        if (startTime > endTime || levelInfo != GameUI.Instance.levelInfo.text)
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isReady) return;
        if (!collision.transform.Equals(spanwer) && !collision.transform.tag.Equals("Laser"))
        {
            Destroy(gameObject);
        }
    }
}

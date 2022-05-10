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
    private void Start()
    {
        Vector3 boxPt = spanwer.GetComponent<BoxCollider>().bounds.max;
        float longest = 0;
        if (boxPt.x > longest) longest = boxPt.x;
        if (boxPt.y > longest) longest = boxPt.y;
        if (boxPt.z > longest) longest = boxPt.z;
        transform.position += longest * transform.forward * 0.5f;

        startTime = Time.timeSinceLevelLoad;
        endTime = Time.timeSinceLevelLoad + 8;
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
        if (!collision.transform.Equals(spanwer) && !collision.transform.tag.Equals("Laser"))
        {
            Destroy(gameObject);
        }
    }
}

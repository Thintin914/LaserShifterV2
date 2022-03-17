using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class wait : MonoBehaviour
{
    public VideoPlayer videoplayer;
    public string scene;
    public float wait_time;

    void Start()
    {
        videoplayer = GetComponentInParent<VideoPlayer>();
        StartCoroutine(WaitforVideoEnd());
    }

    private void Update()
    {
        if (Input.anyKey)
        {
            SceneManager.LoadScene(scene);
        }
    }

    IEnumerator WaitforVideoEnd()
    {
        yield return new WaitForSeconds(wait_time);
        SceneManager.LoadScene(scene);
    }
}
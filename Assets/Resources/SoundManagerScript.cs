using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{

    public static AudioClip jumpSound, laserSound, winSound,clickButtonSound;
    static AudioSource audioSrc;

    // Start is called before the first frame update
    void Start()
    {
        jumpSound = Resources.Load<AudioClip>("jump");
        laserSound = Resources.Load<AudioClip>("laser");
        winSound = Resources.Load<AudioClip>("win");
        clickButtonSound = Resources.Load<AudioClip>("buttonClick1");

        audioSrc = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void PlaySound(string clip)
    {
        switch (clip)
        {
            case "jump":
                audioSrc.PlayOneShot(jumpSound);
                break;
            case "laser":
                audioSrc.PlayOneShot(laserSound);
                break;
            case "win":
                audioSrc.PlayOneShot(winSound);
                break;
            case "buttonClick1":
                audioSrc.PlayOneShot(clickButtonSound);
                break;
        }
    }

}

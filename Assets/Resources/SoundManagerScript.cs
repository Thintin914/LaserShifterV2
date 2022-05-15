using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{

    public static AudioClip jumpSound, laserSound, winSound,clickButtonSound,deathSound;
    static AudioSource audioSrc;

    // Start is called before the first frame update
    void Start()
    {
        jumpSound = Resources.Load<AudioClip>("jump");
        laserSound = Resources.Load<AudioClip>("laser");
        winSound = Resources.Load<AudioClip>("win");
        clickButtonSound = Resources.Load<AudioClip>("buttonClick1");
        deathSound = Resources.Load<AudioClip>("death");

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
            case "buttonClick":
                audioSrc.PlayOneShot(clickButtonSound);
                break;
            case "death":
                audioSrc.PlayOneShot(deathSound);
                break;
        }
    }

}

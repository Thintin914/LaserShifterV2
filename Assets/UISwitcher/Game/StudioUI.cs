using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class StudioUI : MonoBehaviour
{
    public static StudioUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void GoToStudio()
    {
        Debug.Log("Go To Studio");

    }
}

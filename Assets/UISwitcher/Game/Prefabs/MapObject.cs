using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapObject: MonoBehaviour
{
    public string objectTag = "";
    public string objectName;
    public int rotationalIndex = 0;
    public float x, y, z;
    public string logic = "default";

    [System.NonSerialized] public int uuid;
}

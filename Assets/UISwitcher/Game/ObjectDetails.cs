using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDetails : MonoBehaviour
{
    public Vector3 position { get; set; }
    public string objName { get; set; }
    public Dictionary<string, string> data { get; set; }
    public int rotationIndex { get; set; }
}

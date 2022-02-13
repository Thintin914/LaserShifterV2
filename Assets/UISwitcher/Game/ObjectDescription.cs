using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapObjectData")]
public class ObjectDescription : ScriptableObject
{
    public Description[] obtainables;
    public Description[] unobtainables;

    [System.Serializable]
    public class Description
    {
        public string objName;
        public GameObject prefab;
        public DescriptionParameter[] data;
    }
    [System.Serializable]
    public class DescriptionParameter
    {
        public string name;
        public string value;
    }

    public Description GetObtainableByName(string name)
    {
        foreach (Description d in obtainables)
        {
            if (d.objName == name)
                return d;
        }
        return null;
    }

    public Description GetUnobtainableByName(string name)
    {
        foreach (Description d in unobtainables)
        {
            if (d.objName == name)
                return d;
        }
        return null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "ObjectDataHolder")]
public class ObjectData : ScriptableObject
{
    public ObjectDataDetails[] details;

    [System.Serializable]
    public class ObjectDataDetails
    {
        public string objectName;
        public float spacing;
        public GameObject model;
        [TextArea(10, 60)]
        public string logic;
    }

    public int GetSpawnIndex(string name)
    {
        for(int i = 0; i < details.Length; i++)
        {
            if (details[i].objectName == name)
            {
                return i;
            }
        }
        return -1;
    }
}

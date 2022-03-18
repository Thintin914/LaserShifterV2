using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "Color Preset")]
public class ColorPresets: ScriptableObject
{
    public Set[] colorSets;
    [System.Serializable]
    public class Set
    {
        public string key;
        public Material material;
        public Color color;
    }

    public Set GetColorSet(string name)
    {
        foreach(Set s in colorSets)
        {
            if (s.key.Equals(name))
                return s;
        }
        return null;
    }
}

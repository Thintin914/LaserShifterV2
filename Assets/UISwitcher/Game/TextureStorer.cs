using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TextureStorer : MonoBehaviour
{
    public static TextureStorer Instance;
    public List<TextureData> data = new List<TextureData>();

    private void Awake()
    {
        Instance = this;
    }


    public async Task<Texture2D> GetTexture(string url)
    {
        foreach(TextureData d in data)
        {
            if (d.url.Equals(url))
            {
                return d.texture;
            }
        }
        Texture2D tex = await GameUI.GetRemoteTexture(url);
        data.Add(new TextureData{ url = url, texture = tex });
        return tex;
    }

    [System.Serializable]
    public class TextureData
    {
        public string url;
        public Texture2D texture;
    }
}

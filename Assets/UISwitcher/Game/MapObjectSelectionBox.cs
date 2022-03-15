using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapObjectSelectionBox : MonoBehaviour
{
    public TMPro.TextMeshProUGUI description;
    public Button button;
    public string referenceKey;

    public void OnClick()
    {
        int index = EditorUI.Instance.objectData.GetSpawnIndex(referenceKey);
        Vector3 pos = EditorUI.Instance.GetGroundSpawnPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        EditorUI.Instance.SpawnMapObject(pos, index, true);
        EditorUI.Instance.LeftVerticalLayout.gameObject.SetActive(false);
    }
}

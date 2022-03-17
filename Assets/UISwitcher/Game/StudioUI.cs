using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;

public class StudioUI : MonoBehaviour
{
    public static StudioUI Instance;
    public GameObject selectBoxPrefab;
    public List<MapObjectSelectionBox> selectBoxs = new List<MapObjectSelectionBox>();
    private Button reeditButton;
    private Transform VerticalLayout;

    private bool isReeditOpeded = false;
    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        VerticalLayout = transform.GetChild(1);

        reeditButton = transform.GetChild(0).GetComponent<Button>();
        reeditButton.onClick.RemoveAllListeners();
        reeditButton.onClick.AddListener(async() =>
        {
            if (string.IsNullOrEmpty(CommonUI.Instance.username))
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                CommonUI.Instance.popupNotice.Show("You have to login to re-edit levels.", 2);
                return;
            }
            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();

            if (levelSnapShot.Exists)
            {
                if (!isReeditOpeded)
                {
                    isReeditOpeded = true;

                    foreach (MapObjectSelectionBox b in selectBoxs)
                    {
                        Destroy(b.gameObject);
                    }
                    selectBoxs.Clear();

                    List<object> levelNames = new List<object>();
                    Dictionary<string, object> levelDict = levelSnapShot.ToDictionary();
                    foreach(KeyValuePair<string, object> pair in levelDict)
                    {
                        levelNames = pair.Value as List<object>;
                    }
                    foreach(object o in levelNames)
                    {
                        string castedLevel = string.Format("{0}", o);
                        string levelName = castedLevel.Split('\n', '\r')[0];
                        MapObjectSelectionBox temp = Instantiate(selectBoxPrefab).GetComponent<MapObjectSelectionBox>();
                        temp.description.text = $"<size='14'>{levelName}</size>";
                        temp.button.onClick.RemoveAllListeners();
                        temp.button.onClick.AddListener(() =>
                        {
                            Debug.Log(castedLevel);
                            Remove();
                        });
                        temp.transform.SetParent(VerticalLayout);
                        temp.transform.localScale = Vector3.one;
                        selectBoxs.Add(temp);
                    }
                }
            }
            else
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                CommonUI.Instance.popupNotice.Show("You haven't created levels!", 2);
            }
        });
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Studio")
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void GoToStudio()
    {
        Debug.Log("Go To Studio");
        Remove();
        GameUI.Instance.ShowCommentBar();
    }

    public void Remove()
    {
        foreach(MapObjectSelectionBox b in selectBoxs)
        {
            Destroy(b.gameObject);
        }
        selectBoxs.Clear();
    }
}

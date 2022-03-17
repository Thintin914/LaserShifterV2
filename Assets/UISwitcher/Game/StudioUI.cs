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
    public GameObject selectBoxPrefab,studioModel;
    private GameObject studioModelHolder,playerHolder;
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
                            Remove();
                            EditorUI.Instance.ConstructLevel(castedLevel);
                            UISwitcher.Instance.SetUI("Testing");
                            TestingUI.Instance.SetUp(false);
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
            int rand = Random.Range(0, TestingUI.Instance.playerPrefabs.Length);
            GameObject temp = Instantiate(TestingUI.Instance.playerOuterPrefab,new Vector3(1,2.5f, 1), Quaternion.identity);
            GameObject player = Instantiate(TestingUI.Instance.playerPrefabs[rand],new Vector3(1,2.5f,1),Quaternion.identity);
            player.transform.SetParent(temp.transform);
            player.transform.position += Vector3.down * 0.5f;
            studioModelHolder = Instantiate(studioModel, new Vector3(-1.32f,0,-4.34f), Quaternion.Euler(0,53.2f,0));
            studioModelHolder.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            gameObject.SetActive(true);
            playerHolder = temp;
        }
        else
        {
            Destroy(playerHolder);
            Destroy(studioModelHolder);
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

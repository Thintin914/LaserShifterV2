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
    public GameObject selectBoxPrefab, studioModel, removeButtonPrefab;
    private GameObject studioModelHolder,playerHolder;
    public List<MapObjectSelectionBox> selectBoxs = new List<MapObjectSelectionBox>();
    private Button reeditButton;
    public Transform verticalLayout;

    public List<Button> removeButtons = new List<Button>();
    public List<object> allLevels = new List<object>();

    private bool isReeditOpeded = false;
    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        vecticalLayoutOriginalPosition = verticalLayout.localPosition;

        reeditButton = transform.GetChild(0).GetComponent<Button>();
        reeditButton.onClick.RemoveAllListeners();
        reeditButton.onClick.AddListener(async() =>
        {
            if (CommonUI.Instance.isGuest)
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                CommonUI.Instance.popupNotice.Show("You have to login to re-edit levels.", 2);
                return;
            }
            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();
            selectBoxOffset = 0;
            if (levelSnapShot.Exists)
            {
                if (!isReeditOpeded)
                {
                    isReeditOpeded = true;

                    if (selectBoxs.Count == 0)
                    {
                        foreach (MapObjectSelectionBox b in selectBoxs)
                        {
                            Destroy(b.gameObject);
                        }
                        selectBoxs.Clear();

                        List<object> levelNames = new List<object>();
                        Dictionary<string, object> levelDict = levelSnapShot.ToDictionary();
                        foreach (KeyValuePair<string, object> pair in levelDict)
                        {
                            levelNames = pair.Value as List<object>;
                        }
                        foreach (object o in levelNames)
                        {
                            allLevels.Add(o);
                            string castedLevel = string.Format("{0}", o);
                            string levelName = castedLevel.Split('\n', '\r')[0];

                            MapObjectSelectionBox temp = Instantiate(selectBoxPrefab).GetComponent<MapObjectSelectionBox>();
                            temp.description.text = $"<size='14'>{levelName}</size>";
                            selectBoxs.Add(temp);
                            temp.button.onClick.AddListener(() =>
                            {
                                Remove();
                                UISwitcher.Instance.SetUI("Editor");
                                EditorUI.Instance.SetUp(false);
                                EditorUI.Instance.ConstructLevel(castedLevel);
                                EditorUI.Instance.levelName = levelName;
                            });

                            Button removeButton = Instantiate(removeButtonPrefab).GetComponent<Button>();
                            removeButton.transform.SetParent(verticalLayout);
                            removeButton.transform.localScale = Vector3.one;
                            removeButtons.Add(removeButton);
                            removeButton.onClick.AddListener(async () =>
                            {
                                allLevels.Remove(o);
                                selectBoxs.Remove(temp);
                                removeButtons.Remove(removeButton);
                                Dictionary<string, object> update = new Dictionary<string, object>
                                {
                                    {"createdLevels", allLevels }
                                };
                                Destroy(temp.gameObject);
                                Destroy(removeButton.gameObject);
                                await levelDocRef.UpdateAsync(update);

                                DocumentReference usernameDocRef = CommonUI.db.Collection("users").Document(CommonUI.Instance.username);
                                DocumentSnapshot usernameSnapshot = await usernameDocRef.GetSnapshotAsync();
                                int createdLevelNumber = 0;
                                Dictionary<string, object> userDict = usernameSnapshot.ToDictionary();
                                foreach (KeyValuePair<string, object> pair in userDict)
                                {
                                    if (pair.Key.Equals("createdLevels"))
                                    {
                                        createdLevelNumber = int.Parse(string.Format("{0}", pair.Value));
                                        break;
                                    }
                                }

                                Dictionary<string, object> userCreatedLevelDict = new Dictionary<string, object>
                                {
                                    {"createdLevels", createdLevelNumber - 1 }
                                };
                                await usernameDocRef.UpdateAsync(userCreatedLevelDict);
                            });
                            temp.transform.SetParent(verticalLayout);
                            temp.transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        foreach (MapObjectSelectionBox b in selectBoxs)
                        {
                            b.gameObject.SetActive(true);
                        }
                        foreach (Button b in removeButtons)
                        {
                            b.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    isReeditOpeded = false;
                    foreach (MapObjectSelectionBox b in selectBoxs)
                    {
                        b.gameObject.SetActive(false);
                    }
                    foreach (Button b in removeButtons)
                    {
                        b.gameObject.SetActive(false);
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
            studioModelHolder = Instantiate(studioModel, new Vector3(-38.6f,-1.16f,7.28f), Quaternion.Euler(0,53.2f,0));
            studioModelHolder.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            playerHolder = TestingUI.Instance.SpawnTestingPlayer(new Vector3(1,2.5f,1));
            gameObject.SetActive(true);
        }
        else
        {
            if (playerHolder)
            {
                Destroy(playerHolder);
                TestingUI.Instance.testingPlayers.Clear();
            }
            if (studioModelHolder)
                Destroy(studioModelHolder);
            Remove();
            isReeditOpeded = false;
            gameObject.SetActive(false);
        }
    }

    public void GoToStudio()
    {
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
        foreach (Button b in removeButtons)
        {
            Destroy(b.gameObject);
        }
        removeButtons.Clear();
        allLevels.Clear();
    }

    private float selectBoxOffset = 0;
    [SerializeField]private Vector3 vecticalLayoutOriginalPosition;
    private void Update()
    {
        if (isReeditOpeded)
        {
            selectBoxOffset += Input.mouseScrollDelta.y * 20;
            if (selectBoxOffset < 0) selectBoxOffset = 0;
            verticalLayout.localPosition = vecticalLayoutOriginalPosition + Vector3.up * selectBoxOffset;
        }
    }
}

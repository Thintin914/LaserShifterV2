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
    public GameObject studioModelHolder,playerHolder;
    public List<MapObjectSelectionBox> selectBoxs = new List<MapObjectSelectionBox>();
    private Button reeditButton, tutorialButton;
    public Transform verticalLayout;

    public List<Button> removeButtons = new List<Button>();
    public List<object> allLevels = new List<object>();
    public List<object> allVotes = new List<object>();

    private bool isReeditOpeded = false;
    private DocumentReference previousLevelDocRef = null;
    private DocumentSnapshot previousLevelSnapshot = null;
    private DocumentReference previousVoteDocRef = null;
    private DocumentSnapshot previousVoteSnapshot = null;

    public string tutorialData = "";

    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        vecticalLayoutOriginalPosition = verticalLayout.localPosition;

        reeditButton = transform.GetChild(0).GetComponent<Button>();
        tutorialButton = transform.GetChild(1).GetComponent<Button>();
        reeditButton.onClick.RemoveAllListeners();
        reeditButton.onClick.AddListener(async() =>
        {
            if (CommonUI.Instance.isGuest)
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                CommonUI.Instance.popupNotice.Show("You have to login to re-edit levels.", 2);
                return;
            }
            Remove();
            if (previousLevelDocRef == null)
            {
                DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
                DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();
                previousLevelDocRef = levelDocRef;
                previousLevelSnapshot = levelSnapShot;

                DocumentReference voteDocRef = CommonUI.db.Collection("votes").Document(CommonUI.Instance.username);
                DocumentSnapshot voteSnapshot = await voteDocRef.GetSnapshotAsync();
                previousVoteDocRef = voteDocRef;
                previousVoteSnapshot = voteSnapshot;
            }

            selectBoxOffset = 0;
            if (previousLevelSnapshot.Exists)
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

                        // Handle Votes
                        List<object> votes = new List<object>();
                        Dictionary<string, object> voteDict = previousVoteSnapshot.ToDictionary();
                        foreach (KeyValuePair<string, object> pair in voteDict)
                        {
                            votes = pair.Value as List<object>;
                        }
                        foreach ( object o in votes)
                        {
                            allVotes.Add(o);
                        }

                        // Handle Level
                        List<object> levelNames = new List<object>();
                        Dictionary<string, object> levelDict = previousLevelSnapshot.ToDictionary();
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
                                int removeIndex = allLevels.IndexOf(o);
                                allLevels.RemoveAt(removeIndex);
                                allVotes.RemoveAt(removeIndex);
                                selectBoxs.Remove(temp);
                                removeButtons.Remove(removeButton);
                                Dictionary<string, object> update = new Dictionary<string, object>
                                {
                                    {"createdLevels", allLevels }
                                };
                                Destroy(temp.gameObject);
                                Destroy(removeButton.gameObject);
                                await previousLevelDocRef.UpdateAsync(update);

                                Dictionary<string, object> voteUpdate = new Dictionary<string, object>
                                {
                                    {"scores", allVotes }
                                };
                                await previousVoteDocRef.UpdateAsync(voteUpdate);

                                // Update Created Level Number
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

                                previousLevelDocRef = null;
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

        tutorialButton.onClick.RemoveAllListeners();
        tutorialButton.onClick.AddListener(async() =>
        {
            Debug.Log("Clicked Tutorial Button");
            if (tutorialData == "")
            {
                DocumentReference tutorialDocRef = CommonUI.db.Collection("environment").Document("tutorial");
                DocumentSnapshot tutorialSnapshot = await tutorialDocRef.GetSnapshotAsync();

                Dictionary<string, object> tutorialDict = tutorialSnapshot.ToDictionary();
                foreach (KeyValuePair<string, object> pair in tutorialDict)
                {
                    tutorialData = string.Format("{0}", pair.Value);
                }
            }
            EditorUI.Instance.ConstructLevel(tutorialData);
            GameUI.Instance.SetLevelInfo(true, "Thintin", EditorUI.Instance.levelName);
            UISwitcher.Instance.SetUI("Testing");
            TestingUI.Instance.SetUp();
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
            previousLevelDocRef = null;
            previousLevelSnapshot = null;
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
        allVotes.Clear();
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.EventSystems;

public class TestingUI : MonoBehaviour
{
    public static TestingUI Instance;
    public GameObject playerOuterPrefab;
    public GameObject[] playerPrefabs;
    public List<PlayerTriggerer> testingPlayers = new List<PlayerTriggerer>();
    private Button resultButton, passedLevelButton;
    public string result = "Fail";
    private Transform levelBar;
    private TMP_InputField levelInputField;

    private Button levelComfireButton, levelCancelButton;
    public Transform controllingPlayer;

    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        levelBar = transform.GetChild(2);
        levelInputField = levelBar.transform.GetChild(2).GetComponent<TMP_InputField>();
        levelComfireButton = levelBar.transform.GetChild(3).GetComponent<Button>();
        levelCancelButton = levelBar.transform.GetChild(4).GetComponent<Button>();

        levelComfireButton.onClick.RemoveAllListeners();
        levelComfireButton.onClick.AddListener(async() =>
        {
            if (EditorUI.Instance.levelName.StartsWith("Tutorial") && !CommonUI.Instance.username.Equals("Thintin"))
            {
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Cannot add unauthorized tutorial.", 1);
                return;
            }
            if (CommonUI.Instance.isGuest)
            {
                Remove();
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Cannot add map in guest mode.", 1);
                return;
            }
            EditorUI.Instance.levelName = levelInputField.text;
            string jsonMapObjects = levelInputField.text + "\n";
            for (int i = 0; i < EditorUI.Instance.mapObjects.Count; i++)
            {
                jsonMapObjects += JsonUtility.ToJson(EditorUI.Instance.mapObjects[i]) + "\n";
            }

            // User Level Update
            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();
            int replacedIndex = -1;

            if (!levelSnapShot.Exists)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    {"createdLevels", new string[]{jsonMapObjects} }
                };
                await levelDocRef.SetAsync(dict).ContinueWithOnMainThread(task => Debug.Log("Added Level"));
            }
            else
            {
                List<object> allLevels = new List<object>();
                Dictionary<string, object> allLevelsDict = levelSnapShot.ToDictionary();
                foreach (KeyValuePair<string, object> pair in allLevelsDict)
                {
                    allLevels = pair.Value as List<object>;
                }

                // replace level with same name
                bool isReplaced = false;
                int count = 0;
                foreach(string s in allLevels)
                {
                    if (s.Split('\n','\r')[0].Equals(levelInputField.text))
                    {
                        replacedIndex = count;
                        allLevels[count] = jsonMapObjects;
                        isReplaced = true;
                        break;
                    }
                    count++;
                }
                if (!isReplaced)
                {
                    allLevels.Add(jsonMapObjects);
                }
                Dictionary<string, object> leveldict = new Dictionary<string, object>
                {
                    {"createdLevels", allLevels}
                };
                await levelDocRef.UpdateAsync(leveldict);


/*                DocumentReference docref = CommonUI.db.Collection("environment").Document("tutorial");
                DocumentSnapshot snapshot = await docref.GetSnapshotAsync();
                Dictionary<string, object> temp = new Dictionary<string, object>
                {
                    {"data", jsonMapObjects}
                };
                await docref.SetAsync(temp).ContinueWithOnMainThread(task => Debug.Log("Added Level"));*/
            }

            // User Level Vote Update
            DocumentReference voteDocRef = CommonUI.db.Collection("votes").Document(CommonUI.Instance.username);
            DocumentSnapshot voteSnapshot = await voteDocRef.GetSnapshotAsync();

            if (!voteSnapshot.Exists)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>
                {
                    {"scores", new int[]{0} }
                };
                await voteDocRef.SetAsync(dict).ContinueWithOnMainThread(task => Debug.Log("Added Vote"));
            }
            else
            {
                List<object> allVotes = new List<object>();
                Dictionary<string, object> allVotesDict = voteSnapshot.ToDictionary();
                foreach (KeyValuePair<string, object> pair in allVotesDict)
                {
                    allVotes = pair.Value as List<object>;
                }

                // reset vote score that is replaced
                if (replacedIndex != -1)
                {
                    allVotes[replacedIndex] = 0;
                }
                else
                {
                    allVotes.Add(0);
                }


                Dictionary<string, object> voteDict = new Dictionary<string, object>
                {
                    {"scores", allVotes}
                };
                await voteDocRef.UpdateAsync(voteDict);
            }

            // User Created Level Number Update
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
                    {"createdLevels", createdLevelNumber + 1 }
                };
            await usernameDocRef.UpdateAsync(userCreatedLevelDict);

            Remove();
            UISwitcher.Instance.SetUI("Editor");
            CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
            CommonUI.Instance.popupNotice.Show("Map added to database.", 3);
        });

        levelCancelButton.onClick.RemoveAllListeners();
        levelCancelButton.onClick.AddListener(() =>
        {
            levelBar.gameObject.SetActive(false);
        });

        resultButton = transform.GetChild(0).GetComponent<Button>();
        resultButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Back";
        resultButton.onClick.RemoveAllListeners();
        resultButton.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(CommonUI.Instance.username))
            {
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Cannot add map in guest mode.", 1);
                return;
            }
            if (result.Equals("Fail"))
            {
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Test Run Failed.", 1);
            }
            else
            {
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Test Run Success.", 1);
            }
            Remove();
        });

        passedLevelButton = transform.GetChild(1).GetComponent<Button>();
        passedLevelButton.onClick.RemoveAllListeners();
        passedLevelButton.onClick.AddListener(() =>
        {
            levelBar.gameObject.SetActive(true);
        });
    }

    public async void SwitchUI(string uiName)
    {
        if (uiName == "Testing")
        {
            levelBar.gameObject.SetActive(false);
            passedLevelButton.gameObject.SetActive(false);
            gameObject.SetActive(true);
            await CommonUI.Instance.UnpixelateCamera();
        }
        else
        {
            gameObject.SetActive(false);
            if (uiName.Equals("Editor") && CommonUI.Instance.dynamicCamera.gameObject.activeSelf)
                CommonUI.Instance.EnableDynamicCamera(false, null);
        }
    }

    public void Remove()
    {
        if (testingPlayers.Count > 0)
        {
            foreach (PlayerTriggerer g in testingPlayers)
            {
                if (g != null)
                    Destroy(g.gameObject);
            }
            testingPlayers.Clear();
            GameUI.Instance.SetLevelInfo(false, "", "");
        }
    }


    public void SetUp()
    {
        bool hasSpawnPoint = false;
        bool hasGoal = false;
        foreach ( MapObject m in EditorUI.Instance.mapObjects)
        {
            if (m.objectName.Equals("Spawn Point"))
                hasSpawnPoint = true;
            if (m.objectName.Equals("Laser Receiver"))
                hasGoal = true;
        }

        CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
        if (!hasSpawnPoint)
        {
            UISwitcher.Instance.SetUI("Editor");
            CommonUI.Instance.popupNotice.Show("No Spawn Point!", 1);
            return;
        }
        if (!hasGoal)
        {
            UISwitcher.Instance.SetUI("Editor");
            CommonUI.Instance.popupNotice.Show("No Goal!", 1);
            return;
        }

        bool hasAssignedPlayer = false;
        result = "Fail";
        GameUI.Instance.OnWinEvent += LevelPassed;
        GameUI.Instance.SetLevelInfo(true, CommonUI.Instance.username, EditorUI.Instance.levelName);

        foreach(PlayerTriggerer p in testingPlayers)
        {
            if (p != null)
                Destroy(p.gameObject);
        }
        testingPlayers.Clear();

        foreach (MapObject m in EditorUI.Instance.mapObjects)
        {
            string logic = null;
            int index = EditorUI.Instance.objectData.GetSpawnIndex(m.objectName);
            string hiddenLogic = EditorUI.Instance.objectData.details[index].hiddenLogic;
            string shownLogic = EditorUI.Instance.objectData.details[index].logic;
            logic = m.logic.Equals("default") ? hiddenLogic + "\n" + shownLogic : hiddenLogic + "\n" + m.logic;
            if (logic != "\n" && logic != "\nnon-editable")
                m.GetComponent<EntityCustomAction>().onLoadLogic(logic);

            if (m.objectName.Equals("Spawn Point"))
            {
                Transform t = SpawnTestingPlayer(m.transform.position).transform;
                t.GetComponent<PlayerTriggerer>().SetPosition(m.transform.position);
                m.gameObject.SetActive(false);
                if (!hasAssignedPlayer)
                {
                    controllingPlayer = t;
                    CommonUI.Instance.EnableDynamicCamera(true, t.GetChild(1));
                }
            }
            if (m.transform.childCount > 0)
            {
                bool shouldDisableParentBox = false;
                for (int k = 0; k < m.transform.childCount; k++)
                {
                    if (!shouldDisableParentBox && m.transform.GetChild(k).GetComponent<BoxCollider>())
                    {
                        shouldDisableParentBox = true;
                    }
                }
                if (shouldDisableParentBox)
                {
                    m.GetComponent<BoxCollider>().enabled = false;
                }
            }
            if (m.objectName.Equals("Text"))
                m.GetComponent<BoxCollider>().enabled = false;
        }
    }

    public GameObject SpawnTestingPlayer(Vector3 position)
    {
        int rand = Random.Range(0, playerPrefabs.Length);
        GameObject temp = Instantiate(playerOuterPrefab, position, Quaternion.identity);
        GameObject player = Instantiate(playerPrefabs[rand], position, Quaternion.identity);
        player.transform.SetParent(temp.transform);
        player.transform.position += Vector3.down;
        testingPlayers.Add(temp.GetComponent<PlayerTriggerer>());
        return temp;
    }

    public void LevelPassed(Transform winner)
    {
        if (result.Equals("Fail"))
        {
            result = "Passed";
            passedLevelButton.gameObject.SetActive(true);
            GameUI.Instance.PauseTimer(true);
            string[] randomQoute = new string[] { ":D", ":O", ":P", "*_*", "^_^", "o_0", ">_>"};
            GameUI.Instance.timerDisplay.text = randomQoute[Random.Range(0, randomQoute.Length)];
        }
        if (winner.CompareTag("Player"))
        {
            winner.GetChild(0).GetComponent<TextMeshPro>().color = new Color32(255, 160, 0, 255);
        }
    }

    private int shiftingIndex = 0;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftingIndex = (shiftingIndex + 1) % testingPlayers.Count;
            controllingPlayer = testingPlayers[shiftingIndex].transform;
            CommonUI.Instance.EnableDynamicCamera(true, controllingPlayer.GetChild(1));
        }
    }
}

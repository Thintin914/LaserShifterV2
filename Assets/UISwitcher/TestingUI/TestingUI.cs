using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;

public class TestingUI : MonoBehaviour
{
    public static TestingUI Instance;
    public GameObject playerOuterPrefab;
    public GameObject[] playerPrefabs;
    [HideInInspector]public List<GameObject> testingPlayers = new List<GameObject>();
    private Button resultButton, tempButton;
    public string result = "Fail";
    private Transform levelBar;
    private TMP_InputField levelInputField;

    private Button levelComfireButton, levelCancelButton;
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
            if (string.IsNullOrEmpty(CommonUI.Instance.username))
            {
                UISwitcher.Instance.SetUI("Editor");
                CommonUI.Instance.popupNotice.Show("Cannot add map in guest mode.", 1);
                return;
            }
            string jsonMapObjects = levelInputField.text + "\n";
            for (int i = 0; i < EditorUI.Instance.mapObjects.Count; i++)
            {
                jsonMapObjects += JsonUtility.ToJson(EditorUI.Instance.mapObjects[i]) + "\n";
            }


            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();

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
                allLevels.Add(jsonMapObjects);

                Dictionary<string, object> leveldict = new Dictionary<string, object>
                {
                    {"createdLevels", allLevels }
                };
                await levelDocRef.UpdateAsync(leveldict);
            }

            // User Created Levels
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

            UISwitcher.Instance.SetUI("Editor");
            CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
            CommonUI.Instance.popupNotice.Show("Map added to database. Don't press again.", 3);
        });

        levelCancelButton.onClick.RemoveAllListeners();
        levelCancelButton.onClick.AddListener(() =>
        {
            levelBar.gameObject.SetActive(false);
        });

        resultButton = transform.GetChild(0).GetComponent<Button>();
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
        });

        tempButton = transform.GetChild(1).GetComponent<Button>();
        tempButton.onClick.RemoveAllListeners();
        tempButton.onClick.AddListener(() =>
        {
            levelBar.gameObject.SetActive(true);
        });
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Testing")
        {
            levelBar.gameObject.SetActive(false);
            gameObject.SetActive(true);
            resultButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Back";
        }
        else
        {
            gameObject.SetActive(false);
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

        foreach (MapObject m in EditorUI.Instance.mapObjects)
        {
            int index = EditorUI.Instance.objectData.GetSpawnIndex(m.objectName);
            string hiddenLogic = EditorUI.Instance.objectData.details[index].hiddenLogic;
            string shownLogic = EditorUI.Instance.objectData.details[index].logic;
            string logic = m.logic.Equals("default") ? hiddenLogic + "\n" + shownLogic : hiddenLogic + "\n" + m.logic;
            if (logic != "\n" && logic != "\nnon-editable")
                m.GetComponent<EntityCustomAction>().onLoadLogic(logic);

            if (m.objectName.Equals("Spawn Point"))
            {
                int rand = Random.Range(0, playerPrefabs.Length);
                GameObject temp = Instantiate(playerOuterPrefab, m.transform.position, Quaternion.identity);
                GameObject player = Instantiate(playerPrefabs[rand], m.transform.position, Quaternion.identity);
                player.transform.SetParent(temp.transform);
                player.transform.position += Vector3.down * 0.5f;
                testingPlayers.Add(temp);
            }
        }
    }
}

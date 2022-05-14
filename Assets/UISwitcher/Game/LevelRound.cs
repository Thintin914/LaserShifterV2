using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Threading;
using Photon.Pun;
using TMPro;

public class LevelRound : MonoBehaviour
{
    public static LevelRound Instance;
    public PhotonView pv;

    public bool isHost = false;
    public int totalUser = 0;

    private async void Start()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();

        while (FirebaseInitializer.Instance.isFirebaseReady == false)
            await Task.Delay(1000);
    }

    private bool isLevelPreviously = false;
    private float startTime = 0;
    private float endTime = 0;
    public bool isCreatingLevel = false;
    private void Update()
    {
        if (!UISwitcher.Instance.currentUIName.Equals("Game"))
        {
            if (isLevelPreviously)
            {
                isLevelPreviously = false;
                if (UISwitcher.Instance.currentUIName.Equals("Editor")) return;

                if (EditorUI.Instance.mapObjects.Count > 0)
                {
                    foreach (MapObject m in EditorUI.Instance.mapObjects)
                    {
                        Destroy(m.gameObject);
                    }
                    EditorUI.Instance.mapObjects.Clear();
                }
            }
            return;
        }

        if (isHost.Equals(true))
        {
            if (!isCreatingLevel)
            {
                startTime += Time.deltaTime;
                if (startTime > endTime)
                {
                    FindLevelData();
                }
            }
        }
    }

    public async void InitalizeLevel()
    {
        isCreatingLevel = false;
        if (isHost)
        {
            startTime = Time.deltaTime;
            endTime = 0;
        }
        else
        {
            DocumentReference roomDocRef = CommonUI.db.Collection("rooms").Document(CommonUI.Instance.currentRoomName);
            DocumentSnapshot roomSnapshot = await roomDocRef.GetSnapshotAsync();
            Dictionary<string, object> roomDict = roomSnapshot.ToDictionary();
            string creator = null;
            int levelName = 0;
            int userId = 0;
            foreach (KeyValuePair<string, object> pair in roomDict)
            {
                if (pair.Key.Equals("creator"))
                    creator = string.Format("{0}", pair.Value);
                else if (pair.Key.Equals("levelName"))
                    levelName = int.Parse(string.Format("{0}", pair.Value));
                else
                    userId = int.Parse(string.Format("{0}", pair.Value));
            }
            await CreateLevel(userId, levelName);
        }
    }

    public async void FindLevelData()
    {
        Debug.Log("isCreatingLevel: " + isCreatingLevel);
        if (isCreatingLevel) return;
        isCreatingLevel = true;
        GameUI.Instance.StopTimer();
        GameUI.Instance.timerDisplay.gameObject.SetActive(true);

        bool needRefetch = false;
        int levelIndex = 0;
        int id = 0;
        do
        {
            bool complete = false;
            id = 0;
            do
            {
                id = Random.Range(1, totalUser + 1);
                complete = await CommonUI.Instance.GetLevelData(id);
            } while (!complete);
            Debug.Log("isCreatingLevel: Got Level Data of: " + id);
            levelIndex = Random.Range(0, CommonUI.Instance.userLevels[id].Count);
            string levelData = CommonUI.Instance.GetLevelFromUser(id, levelIndex);
            if (levelData == null)
            {
                Debug.Log("isCreatingLevel: Refetching");
                needRefetch = true;
            }
            else
            {
                needRefetch = false;
            }
        } while (needRefetch);

        DocumentReference roomDocRef = CommonUI.db.Collection("rooms").Document(CommonUI.Instance.currentRoomName);
        Dictionary<string, object> update = new Dictionary<string, object>
        {
            {"creator", CommonUI.Instance.creatorName[id] },
            {"levelName", levelIndex },
            {"userId", id }
        };
        await roomDocRef.UpdateAsync(update);

        await CreateLevel(id, levelIndex, 0, 0);
        pv.RPC("CreateLevel", RpcTarget.Others, id, levelIndex, startTime, endTime);
    }

    [PunRPC]
    public async Task CreateLevel(int id, int levelIndex, float startTime = 0, float endTime = 0)
    {
        await CommonUI.Instance.PixelateCamera();
        isCreatingLevel = true;
        GameUI.Instance.RemoveAllListeners();
        isLevelPreviously = true;
        GameUI.Instance.StopTimer();

        if (!CommonUI.Instance.userLevels.ContainsKey(id))
        {
            await CommonUI.Instance.GetLevelData(id);
        }
        EditorUI.Instance.ConstructLevel(CommonUI.Instance.GetLevelFromUser(id, levelIndex));
        GameUI.Instance.SetLevelInfo(true, CommonUI.Instance.creatorName[id], EditorUI.Instance.levelName);

        List<Vector3> possibleSpawnPoint = new List<Vector3>();
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
                possibleSpawnPoint.Add(m.transform.position);
                m.gameObject.SetActive(false);
            }
        }

        int rand = Random.Range(0, possibleSpawnPoint.Count);
        GameUI.Instance.player.SetPosition(possibleSpawnPoint[rand]);

        if (startTime == 0)
        {
            this.startTime = Time.timeSinceLevelLoad;
            this.endTime = GameUI.Instance.timer + Time.timeSinceLevelLoad;
        }
        else
        {
            this.startTime = startTime;
            this.endTime = endTime;
        }

        if (GameUI.Instance.isWin)
            pv.RPC("UpdateNameColor", RpcTarget.All, GameUI.Instance.player.pv.ViewID, "w");

        isCreatingLevel = false;
        GameUI.Instance.isWin = false;
        GameUI.Instance.finishedPlayers.Clear();
        await CommonUI.Instance.UnpixelateCamera();
    }

    [PunRPC]
    public void UpdateNameColor(int viewId, string color)
    {
        Transform t = PhotonView.Find(viewId).transform;
        TextMeshPro username = t.GetChild(0).GetComponent<TextMeshPro>();
        if (color.Equals("w"))
        {
            username.color = new Color32(255, 255, 255, 255);
        }
        else if (color.Equals("y"))
        {
            username.color = new Color32(255, 160, 0, 255);
        }
    }
}

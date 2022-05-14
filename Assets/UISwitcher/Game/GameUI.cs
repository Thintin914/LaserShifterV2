using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;
using XLua;
using Photon.Pun;
using UnityEngine.EventSystems;
using Photon.Realtime;

public class GameUI : MonoBehaviour
{
    public TMP_InputField commentBar;
    public static GameUI Instance;
    public float timer;
    public TextMeshProUGUI timerDisplay;
    public ColorPresets colorPresets;
    public TextMeshProUGUI levelInfo;

    public PlayerTriggerer player;
    private PhotonView pv;
    public static string luaLibrary = @"
local Unity = CS.UnityEngine
local Vector3 = Unity.Vector3
local Quaternion = Unity.Quaternion
";

    private void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();

        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        commentBar.onEndEdit.RemoveAllListeners();
        commentBar.onEndEdit.AddListener(async (value) =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                commentBar.text = "";
                if (value.Length > 1 && value[0] == '/')
                {
                    commentBar.gameObject.SetActive(false);

                    string[] command = value.Replace("/", null).Split(' ');
                    Debug.Log("Command Detected: " + command[0]);

                    if (command[0] == "room" && command.Length > 1)
                    {
                        if (command[1].Equals(CommonUI.Instance.currentRoomName))
                        {
                            ShowCommentBar();
                            return;
                        }

                        UISwitcher.Instance.SetUI("Game");
                        if (player != null)
                        {
                            StopTimer();
                            CommonUI.Instance.EnableDynamicCamera(false, null);
                            PhotonNetwork.Destroy(player.gameObject);
                            player = null;
                        }
                        // Spawn Player
                        LevelRound.Instance.isHost = false;
                        await CommonUI.Instance.GoToRoom(command[1], true);
                        while (player == null)
                            await Task.Delay(500);

                        CommonUI.Instance.EnableDynamicCamera(true, player.transform);
                        finishedPlayers.Clear();
                        if (PhotonNetwork.IsMasterClient)
                        {
                            LevelRound.Instance.isHost = true;
                        }

                        if (LevelRound.Instance.isHost)
                            LevelRound.Instance.InitalizeLevel();
                        else
                        {
                            timerDisplay.gameObject.SetActive(true);
                            timerDisplay.text = "<size='12'>Waiting For Round's End.</size>";
                            CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                            CommonUI.Instance.popupNotice.Show($"Game will start until this round's end.", 2);
                        }

                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nRoom {command[1]}", 2);
                        Debug.Log("Go Room Success");
                    }
                    else if (command[0] == "editor")
                    {
                        await CommonUI.Instance.LeaveRoom();
                        CommonUI.Instance.currentRoomName = "";
                        UISwitcher.Instance.SetUI("Editor");
                        EditorUI.Instance.SetUp();
                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nMap Editor", 2);
                    }
                    else if (command[0] == "studio")
                    {
                        if (!UISwitcher.Instance.currentUIName.Equals("Studio"))
                        {
                            await CommonUI.Instance.LeaveRoom();
                            CommonUI.Instance.currentRoomName = "";
                            UISwitcher.Instance.SetUI("Studio");
                            StudioUI.Instance.GoToStudio();
                            CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                            CommonUI.Instance.popupNotice.Show($"Change To\nStudio", 2);
                        }
                    }

                    ShowCommentBar();
                }
                else
                {
                    Talk(value);
                }

                commentBar.DeactivateInputField();
                EventSystem.current.SetSelectedGameObject(null);
            }
        });
    }

    public void SwitchUI(string uiName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        if (uiName == "Game")
        {
            gameObject.SetActive(true);
        }
        else 
        {
            gameObject.SetActive(false);
            if (player != null)
            {
                LevelRound.Instance.isHost = false;
                CommonUI.Instance.EnableDynamicCamera(false, null);
                PhotonNetwork.Destroy(player.gameObject);
            }
        }
    }

    public async void Talk(string content)
    {
        if (!player) return;

        player.talk.text = content;
        await Task.Delay((int)((content.Length * 0.5f) * 1000));
        player.talk.text = null;
    }

    public GameObject SpawnServerPlayer(Vector3 position)
    {
        GameObject temp = PhotonNetwork.Instantiate("PlayerOuter", position, Quaternion.identity);
        int rand = UnityEngine.Random.Range(0, TestingUI.Instance.playerPrefabs.Length);
        pv.RPC("AttachModelToPlayer", RpcTarget.AllBufferedViaServer, temp.GetComponent<PhotonView>().ViewID, rand, CommonUI.Instance.username);
        temp.GetComponent<PlayerTriggerer>().controller.enabled = false;
        return temp;
    }

    [PunRPC]
    public void AttachModelToPlayer(int viewId, int modelId, string name)
    {
        Transform t = PhotonView.Find(viewId).transform;
        GameObject player = Instantiate(TestingUI.Instance.playerPrefabs[modelId], t.position, Quaternion.identity);
        player.transform.SetParent(t);
        player.transform.localPosition = Vector3.zero;
        player.transform.position += Vector3.down;
        PlayerTriggerer triggerer = t.GetComponent<PlayerTriggerer>();
        triggerer.username.text = name;
        t.gameObject.SetActive(false);
        finishedPlayers.Add(name);
    }

    public void ShowCommentBar()
    {
        gameObject.SetActive(true);
        commentBar.gameObject.SetActive(true);
    }

    private bool pauseTimer = false;
    private CancellationTokenSource timerCancelSource = null;
    public void StopTimer()
    {
        if (timerCancelSource != null && !timerCancelSource.IsCancellationRequested)
        {
            timerCancelSource.Cancel();
            timerCancelSource.Dispose();
        }
        if (timerDisplay)
            timerDisplay.gameObject.SetActive(false);
    }

    public void PauseTimer(bool isPause)
    {
        if (isPause)
        {
            pauseTimer = true;
        }
        else
        {
            pauseTimer = false;
        }
    }

    public float startTime = 0;
    public float endTime = 0;
    public async void StartTimer(float countdownTimer, Action callback = null)
    {
        pauseTimer = false;
        timer = countdownTimer;
        Debug.Log("Start Counting");
        StopTimer();
        timerCancelSource = new CancellationTokenSource();

        gameObject.SetActive(true);
        timerDisplay.gameObject.SetActive(true);

        startTime = Time.timeSinceLevelLoad;
        endTime = Time.timeSinceLevelLoad + countdownTimer;

        try
        {
            double leftTime = 0;
            while (endTime > startTime)
            {
                if (!pauseTimer)
                {
                    startTime += Time.deltaTime;
                    leftTime = endTime - startTime;
                    TimeSpan t = TimeSpan.FromSeconds(leftTime);
                    timerDisplay.text = $"{t.Minutes}:{t.Seconds}";
                }
                await Task.Yield();
                if (timerCancelSource == null || timerCancelSource.IsCancellationRequested)
                    return;
            }
        }
        catch (OperationCanceledException) when (timerCancelSource.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            Debug.Log("Timer Complete");
            callback?.Invoke();
        }
    }

    public static async Task<Texture2D> GetRemoteTexture(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            var asyncOp = www.SendWebRequest();

            while (asyncOp.isDone == false)
                await Task.Delay(1000 / 30);//30 hertz

            if (www.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(www);
            }
            return null;
        }
    }

    public async void SetTexture(string url, Transform trans)
    {
        Texture2D tex = await GetRemoteTexture(url);

        Material defaultMaterial = colorPresets.GetColorSet("White").material;

        if (!trans) return;
        MeshRenderer rend = trans.GetComponent<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (rend.material != null)
        {
            rend.material = defaultMaterial;
        }
        rend.GetPropertyBlock(block);
        block.SetTexture("_MainTex", tex);
        rend.SetPropertyBlock(block);

        if (trans.childCount < 0) return;

        for (int i = 0; i < trans.childCount; i++)
        {
            if (trans.GetChild(i).GetComponent<MeshRenderer>())
            {
                MeshRenderer childRend = trans.GetChild(i).GetComponent<MeshRenderer>();
                if (rend.material != null)
                {
                    rend.material = defaultMaterial;
                }
                MaterialPropertyBlock childBlock = new MaterialPropertyBlock();
                childRend.GetPropertyBlock(childBlock);
                childBlock.SetTexture("_MainTex", tex);
                childRend.SetPropertyBlock(childBlock);
            }
        }
    }

    public MapObject FindObjectWithTag(string tag)
    {
        foreach(MapObject m in EditorUI.Instance.mapObjects)
        {
            if (m.objectTag == tag)
                return m;
        }
        return null;
    }

    public void RemoteTrigger(MapObject m, string methodName = "onTrigger")
    {
        if (m.GetComponent<EntityCustomAction>().cycle == null) return;

        if (UISwitcher.Instance.currentUIName.Equals("Testing"))
        {
            m.GetComponent<EntityCustomAction>().cycle.Trigger(methodName, TestingUI.Instance.controllingPlayer);
        }
        else
        {
            pv.RPC("PunRemoteTrigger", RpcTarget.All, player.pv.ViewID, m.objectTag, methodName);
        }
    }

    [PunRPC]
    public void PunRemoteTrigger(int triggererViewId, string tag, string methodName)
    {
        MapObject m = FindObjectWithTag(tag);
        if (!m) return;
        m.GetComponent<EntityCustomAction>().cycle.Trigger(methodName, PhotonView.Find(triggererViewId).transform);
    }

    public Laser ShotLaser(Transform t, Transform source, Vector3 rotation, float speed, string property)
    {
        return LaserManager.Instance.ShotLaser(t, source, rotation, speed, property);
    }

    [CSharpCallLua]
    public delegate void OnWinDelegate(Transform winner);
    public event OnWinDelegate OnWinEvent;

    public bool isWin = false;
    public void TriggerWinEvent(Transform winner)
    {
        isWin = true;
        if (UISwitcher.Instance.currentUIName.Equals("Testing"))
        {
            OnWinEvent?.Invoke(winner);
        }
        else
        {
            pv.RPC("RemotePlayerWin", RpcTarget.All, winner.GetComponent<PhotonView>().ViewID);
        }
    }

    public void RemoveAllListeners()
    {
        if (OnWinEvent != null)
        {
            foreach (Delegate d in OnWinEvent.GetInvocationList())
            {
                OnWinEvent -= (OnWinDelegate)d;
            }
        }
    }

    public void SetLevelInfo(bool isDisplay, string creator, string levelName)
    {
        if (isDisplay)
        {
            gameObject.SetActive(true);
            levelInfo.gameObject.SetActive(true);
            levelInfo.text = $"Creator - {creator}\nLevel Name - {levelName}";
        }
        else
        {
            levelInfo.gameObject.SetActive(false);
        }
    }

    public int totalPlayers = 0;
    public List<string> finishedPlayers = new List<string>();
    [PunRPC]
    public void RemotePlayerWin(int viewId)
    {
        Transform t = PhotonView.Find(viewId).transform;
        TextMeshPro username = t.GetChild(0).GetComponent<TextMeshPro>();
        username.color = new Color32(255, 160, 0, 255);
        OnWinEvent?.Invoke(t);

        if (!finishedPlayers.Contains(username.text))
        {
            finishedPlayers.Add(username.text);
            CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
            CommonUI.Instance.popupNotice.Show($"{username.text} completed the level!", 1);
        }

        if (LevelRound.Instance.isHost)
        {
            if (finishedPlayers.Count >= totalPlayers)
            {
                LevelRound.Instance.FindLevelData();
            }
        }
    }
/*
    [PunRPC]
    public void voteLevel()
    {

    }*/

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;

public class GameUI : MonoBehaviour
{
    public TMP_InputField commentBar;
    public static GameUI Instance;
    public float timer;
    public TextMeshProUGUI timerDisplay;
    private void Awake()
    {
        Instance = this;

        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        commentBar = transform.GetChild(0).GetComponent<TMP_InputField>();
        timerDisplay = transform.GetChild(1).GetComponent<TextMeshProUGUI>();

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
                        UISwitcher.Instance.SetUI("Game");
                        await CommonUI.Instance.GoToRoom(command[1]);
                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nRoom {command[1]}", 2);
                    }
                    else if (command[0] == "editor")
                    {
                        CommonUI.Instance.LeaveRoom();
                        UISwitcher.Instance.SetUI("Editor");
                        EditorUI.Instance.SetUp();
                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nMap Editor", 2);
                    }
                    else if (command[0] == "studio")
                    {
                        CommonUI.Instance.LeaveRoom();
                        UISwitcher.Instance.SetUI("Studio");
                        StudioUI.Instance.GoToStudio();
                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nStudio", 2);
                    }

                    commentBar.gameObject.SetActive(true);
                }

                commentBar.ActivateInputField();
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
        }
    }

    public void ShowCommentBar()
    {
        gameObject.SetActive(true);
        commentBar.gameObject.SetActive(true);
    }

    private CancellationTokenSource timerCancelSource = null;
    public void StopTimer()
    {
        if (timerCancelSource != null && !timerCancelSource.IsCancellationRequested)
        {
            timerCancelSource.Cancel();
            timerCancelSource.Dispose();
        }
    }
    public async void StartTimer(float countdownTimer, Action callback = null)
    {
        timer = countdownTimer;
        Debug.Log("Start Counting");
        StopTimer();
        timerCancelSource = new CancellationTokenSource();

        gameObject.SetActive(true);
        timerDisplay.gameObject.SetActive(true);
        float startTime = Time.timeSinceLevelLoad;
        float endTime = Time.timeSinceLevelLoad + countdownTimer;
        try
        {
            double leftTime = 0;
            while (endTime > startTime)
            {
                startTime += Time.deltaTime;
                leftTime = endTime - startTime;
                TimeSpan t = TimeSpan.FromSeconds(leftTime);
                timerDisplay.text = $"{t.Minutes}:{t.Seconds}";
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
            timerDisplay.gameObject.SetActive(false);
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
        Debug.Log(trans.name);
        Texture2D tex = await GetRemoteTexture(url);

        MeshRenderer rend = trans.GetComponent<MeshRenderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetTexture("_MainTex", tex);
        rend.SetPropertyBlock(block);

        if (trans.childCount < 0) return;

        for (int i = 0; i < trans.childCount; i++)
        {
            if (trans.GetChild(i).GetComponent<MeshRenderer>())
            {
                MeshRenderer childRend = trans.GetChild(i).GetComponent<MeshRenderer>();
                MaterialPropertyBlock childBlock = new MaterialPropertyBlock();
                childRend.GetPropertyBlock(childBlock);
                childBlock.SetTexture("_MainTex", tex);
                childRend.SetPropertyBlock(childBlock);
            }
        }
    }
}

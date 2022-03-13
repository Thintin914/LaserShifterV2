using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using TMPro;
using System.Threading.Tasks;
using System.Threading;
using Firebase.Firestore;
using Photon.Pun;
using Photon.Realtime;

public class CommonUI : MonoBehaviourPunCallbacks, ILobbyCallbacks
{
    public static CommonUI Instance;
    public static FirebaseFirestore db;

    private Button closeButton;
    public PopupNotice popupNotice;
    public string username;
    public string currentRoomName;

    private void Awake()
    {
        Instance = this;
        db = FirebaseFirestore.DefaultInstance;

        closeButton = transform.GetChild(0).GetComponent<Button>();
        closeButton.onClick.AddListener(() =>
        {
            Debug.Log("Quit Game");
            Application.Quit();
        });

        popupNotice = new PopupNotice
        {
            background = transform.GetChild(1).GetComponent<MPImage>(),
            text = transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>()
        };
        popupNotice.SetColor(16, 23, 34, 0);
    }

    private bool isConnectedToMaster = false;
    private bool canChangeRoom = false;
    public async Task GoToRoom(string roomName)
    {
        if (isConnectedToMaster == false)
        {
            PhotonNetwork.ConnectUsingSettings();
            while (isConnectedToMaster == false) { await Task.Delay(50); };
        }
        if (PhotonNetwork.InRoom)
        {
            canChangeRoom = false;
            PhotonNetwork.LeaveRoom();
            while (canChangeRoom == false) { await Task.Delay(50); };
        }

        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 30
        };

        currentRoomName = roomName;
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connect To Master Server.");
        if (isConnectedToMaster == false)
            isConnectedToMaster = true;
        else
            canChangeRoom = true;
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Leave Room.");
            canChangeRoom = false;
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Join Room " + currentRoomName);
    }

    public class PopupNotice
    {
        public MPImage background { get; set; }
        public TextMeshProUGUI text { get; set; }

        public void SetColor(byte r, byte g, byte b, byte a)
        {
            background.color = new Color32(r, g, b, a);
            text.color = new Color32(255, 255, 255, a);
        }

        private CancellationTokenSource transitionCancelSource = null;
        private async Task FadeOut()
        {
            if (transitionCancelSource != null)
            {
                transitionCancelSource.Cancel();
                transitionCancelSource.Dispose();
                transitionCancelSource = null;
            }
            transitionCancelSource = new CancellationTokenSource();

            Color32 m_Color = background.color;
            SetColor(m_Color.r, m_Color.g, m_Color.b, 255);

            try
            {
                for (int i = 255; i >= 0; i = i - 5)
                {
                    SetColor(m_Color.r, m_Color.g, m_Color.b, (byte)i);
                    await Task.Yield();
                    if (transitionCancelSource.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (System.Exception) when (transitionCancelSource.IsCancellationRequested)
            {
                return;
            }
        }

        private async Task FadeIn()
        {
            if (transitionCancelSource != null)
            {
                transitionCancelSource.Cancel();
                transitionCancelSource.Dispose();
                transitionCancelSource = null;
            }
            transitionCancelSource = new CancellationTokenSource();

            Color32 m_Color = background.color;
            SetColor(m_Color.r, m_Color.g, m_Color.b, 0);

            try
            {
                for (int i = 0; i <= 255; i = i + 5)
                {
                    SetColor(m_Color.r, m_Color.g, m_Color.b, (byte)i);
                    await Task.Yield();
                    if (transitionCancelSource.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (System.Exception) when (transitionCancelSource.IsCancellationRequested)
            {
                return;
            }
        }

        private CancellationTokenSource showCancelSource = null;
        public async void Show(string text, float second)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (showCancelSource != null)
            {
                showCancelSource.Cancel();
                showCancelSource.Dispose();
                showCancelSource = null;
            }
            if (transitionCancelSource != null)
            {
                transitionCancelSource.Cancel();
                transitionCancelSource.Dispose();
                transitionCancelSource = null;
            }
            showCancelSource = new CancellationTokenSource();

            this.text.text = text;
            try
            {
                await FadeIn();
                await Task.Delay((int)(second * 1000), showCancelSource.Token);
                await FadeOut();
            }
            catch (System.Exception) when (showCancelSource.IsCancellationRequested)
            {
                return;
            }
        }
    }
}

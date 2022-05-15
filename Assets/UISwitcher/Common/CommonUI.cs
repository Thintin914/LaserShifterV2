using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using TMPro;
using System.Threading.Tasks;
using System.Threading;
using Firebase.Firestore;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;

public class CommonUI : MonoBehaviourPunCallbacks, ILobbyCallbacks, IInRoomCallbacks
{
    public static CommonUI Instance;
    public static FirebaseFirestore db;

    private Button closeButton;
    public PopupNotice popupNotice;
    public string username;
    public bool isGuest = false;
    public string currentRoomName;
    public Camera dynamicCamera, mainCamera;
    [HideInInspector]public Camera currentCamera;
    public Transform lookAt;

    private void Awake()
    {
        Instance = this;
        db = FirebaseFirestore.DefaultInstance;

        currentCamera = mainCamera;
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
    private bool canSpawnPlayer = false;
    public async Task GoToRoom(string roomName, bool spawnPlayer = true)
    {
        if (isConnectedToMaster == false)
        {
            PhotonNetwork.ConnectUsingSettings();
            while (isConnectedToMaster == false) { await Task.Delay(50); };
        }
        if (PhotonNetwork.InRoom)
        {
            await LeaveRoom();
            while (canChangeRoom == false) { await Task.Delay(50); };
        }

        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 30
        };

        canSpawnPlayer = spawnPlayer;
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

    public async Task LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Leave Room.");
            await RemoveRoom();
            PhotonNetwork.LeaveRoom();
        }
        canChangeRoom = false;
    }

    public override async void OnJoinedRoomAsync()
    {
        Debug.Log("Join Room " + currentRoomName);

        //Add room to firebase
        DocumentReference roomDocRef = db.Collection("rooms").Document(currentRoomName);
        DocumentSnapshot roomSnapshot = await roomDocRef.GetSnapshotAsync();
        if (!roomSnapshot.Exists)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>
            {
                {"creator", "Thintin" },
                {"levelName", "the start of everything" },
                {"userId", 1 }
            };
            await roomDocRef.SetAsync(dict).ContinueWithOnMainThread(task => Debug.Log("Added Room"));
        }

        if (canSpawnPlayer)
        {
            GameUI.Instance.player = GameUI.Instance.SpawnServerPlayer(Vector3.zero).GetComponent<PlayerTriggerer>();
        }
    }

    private async void OnApplicationQuit()
    {
        await RemoveRoom();
    }

    public async Task RemoveRoom()
    {
        if (UISwitcher.Instance.currentUIName.Equals("Game"))
        {
            if (PhotonNetwork.PlayerList.Length == 1)
            {
                DocumentReference roomDocRef = db.Collection("rooms").Document(currentRoomName);
                DocumentSnapshot roomSnapshot = await roomDocRef.GetSnapshotAsync();
                if (roomSnapshot.Exists)
                {
                    await roomDocRef.DeleteAsync();
                    Debug.Log("Deleted Room: " + currentRoomName);
                }
            }
        }
    }

    void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("Host switched: " + newMasterClient);
        if (PhotonNetwork.MasterClient == newMasterClient)
        {
            LevelRound.Instance.isHost = true;
            if (GameUI.Instance.isVoting)
            {
                LevelRound.Instance.FindLevelData();
            }
        }
    }
    public void EnableDynamicCamera(bool isEnable, Transform target)
    {
        dynamicCamera.gameObject.SetActive(isEnable);

        if (isEnable)
        {
            currentCamera = dynamicCamera;
            mainCamera.gameObject.SetActive(false);
            lookAt.SetParent(target);
            lookAt.localPosition = Vector3.zero;
        }
        else
        {
            currentCamera = mainCamera;
            mainCamera.gameObject.SetActive(true);
            mainCamera.transform.position = new Vector3(0, 100, -100);
            lookAt.SetParent(null);
        }
    }

    public Dictionary<int, List<object>> userLevels = new Dictionary<int, List<object>>();
    public Dictionary<int, string> creatorName = new Dictionary<int, string>();
    public async Task<bool> GetLevelData(int id)
    {
        if (userLevels.ContainsKey(id))
        {
            return true;
        }
        else
        {
            if (string.IsNullOrEmpty(currentRoomName)) return false;

            Query userQuery = db.Collection("users").WhereEqualTo("id", id);
            QuerySnapshot userQuerySnapshot = await userQuery.GetSnapshotAsync();
            string username = null;
            foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
            {
                username = documentSnapshot.Id;
            }

            DocumentReference levelDocRef = db.Collection("levels").Document(username);
            DocumentSnapshot levelSnapshot = await levelDocRef.GetSnapshotAsync();
            Dictionary<string, object> levelDict = levelSnapshot.ToDictionary();
            if (levelDict != null)
            {
                foreach (KeyValuePair<string, object> pair in levelDict)
                {
                    userLevels.Add(id, pair.Value as List<object>);
                    break;
                }
            }
            else
            {
                return false;
            }
            creatorName.Add(id, username);
            return true;
        }
    }

    public string GetLevelFromUser(int id , int index)
    {
        if (userLevels.ContainsKey(id))
        {
            if (id >= userLevels[id].Count) return null;
            return string.Format("{0}", userLevels[id][index]);
        }
        return null;
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

    private CancellationTokenSource cameraPixelateCancelSource = null;
    private Codexus.PixelEffect pixelEffect = null;
    public async Task PixelateCamera()
    {
        if (pixelEffect == null)
        {
            pixelEffect = dynamicCamera.GetComponent<Codexus.PixelEffect>();
        }

        if (cameraPixelateCancelSource != null)
        {
            cameraPixelateCancelSource.Cancel();
            cameraPixelateCancelSource.Dispose();
            cameraPixelateCancelSource = null;
        }
        pixelEffect.pixelHeight = 1;
        pixelEffect.pixelWidth = 1;
        cameraPixelateCancelSource = new CancellationTokenSource();

        try
        {
            for (int i = 0; i < 40; i++)
            {
                pixelEffect.pixelHeight += 0.25f;
                pixelEffect.pixelWidth += 0.25f;
                await Task.Yield();
                if (cameraPixelateCancelSource.IsCancellationRequested)
                {
                    return;
                }
            }
            pixelEffect.pixelHeight = 20;
            pixelEffect.pixelWidth = 20;
        }
        catch (System.Exception) when (cameraPixelateCancelSource.IsCancellationRequested)
        {
            return;
        }
    }
    public async Task UnpixelateCamera()
    {
        if (pixelEffect == null)
        {
            pixelEffect = dynamicCamera.GetComponent<Codexus.PixelEffect>();
        }

        if (cameraPixelateCancelSource != null)
        {
            cameraPixelateCancelSource.Cancel();
            cameraPixelateCancelSource.Dispose();
            cameraPixelateCancelSource = null;
        }
        pixelEffect.pixelHeight = 20;
        pixelEffect.pixelWidth = 20;
        cameraPixelateCancelSource = new CancellationTokenSource();

        try
        {
            for (int i = 0; i < 35; i++)
            {
                pixelEffect.pixelHeight -= 0.25f;
                pixelEffect.pixelWidth -= 0.25f;
                await Task.Yield();
                if (cameraPixelateCancelSource.IsCancellationRequested)
                {
                    return;
                }
            }
            pixelEffect.pixelHeight = 1;
            pixelEffect.pixelWidth = 1;
        }
        catch (System.Exception) when (cameraPixelateCancelSource.IsCancellationRequested)
        {
            return;
        }
    }

    public async void updateVote(string username, int index, int score)
    {
        DocumentReference voteDocRef = db.Collection("votes").Document(username);
        DocumentSnapshot voteSnapshot = await voteDocRef.GetSnapshotAsync();

        List<object> votes = new List<object>();
        Dictionary<string, object> voteDict = voteSnapshot.ToDictionary();
        foreach (KeyValuePair<string, object> pair in voteDict)
        {
            votes = pair.Value as List<object>;
        }

        int finalScore = int.Parse(string.Format("{0}", votes[index])) + score;
        votes[index] = finalScore;

        if (finalScore < -100)
        {
            votes.RemoveAt(index);
            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();

            List<object> levelNames = new List<object>();
            Dictionary<string, object> levelDict = levelSnapShot.ToDictionary();
            foreach (KeyValuePair<string, object> pair in levelDict)
            {
                levelNames = pair.Value as List<object>;
            }
            levelNames.RemoveAt(index);
            Dictionary<string, object> levelUpdate = new Dictionary<string, object>
            {
            {"creadtedLevels", levelNames }
            };
            await voteDocRef.UpdateAsync(levelUpdate);

            // Update Created Level Number
            DocumentReference usernameDocRef = db.Collection("users").Document(username);
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
        }

        Dictionary<string, object> voteUpdate = new Dictionary<string, object>
        {
            {"scores", votes }
        };
        await voteDocRef.UpdateAsync(voteUpdate);
    }
}

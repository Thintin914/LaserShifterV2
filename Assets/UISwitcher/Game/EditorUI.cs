using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;
using UnityEngine.EventSystems;

public class EditorUI : MonoBehaviour
{
    public static EditorUI Instance;
    public ObjectData objectData;
    private Camera cam;
    public List<MapObject> mapObjects = new List<MapObject>();
    public List<MapObjectSelectionBox> mapObjectSelectionBoxs = new List<MapObjectSelectionBox>();
    public List<SelectObjectDescription> selectObjectDescriptions = new List<SelectObjectDescription>();
    public List<SelectObjectDescription> objectParameterDescriptions = new List<SelectObjectDescription>();
    public List<ObjectParameter> objectParameterBoxs = new List<ObjectParameter>();
    private CurrentMapObject currentMapObject;
    private Button selectObjectButton, toTestingButton;
    public Transform LeftVerticalLayout;
    public GameObject selectObjectBoxPrefab, objectParameterBoxPrefab, largeObjectParameterBoxPrefab;
    public string levelName = "undefined";
    public GameObject background;

    public int currentUuid = 0;

    [HideInInspector]public Quaternion[] allRotations = new Quaternion[]
    {
        Quaternion.Euler(0,0,0), Quaternion.Euler(45,0,0), Quaternion.Euler(90,0,0), Quaternion.Euler(135,0,0), Quaternion.Euler(180,0,0),
        Quaternion.Euler(225,0,0), Quaternion.Euler(270,0,0), Quaternion.Euler(315,0,0),
        Quaternion.Euler(0,45,0), Quaternion.Euler(0,90,0), Quaternion.Euler(0,135,0), Quaternion.Euler(0,180,0),
        Quaternion.Euler(0,225,0), Quaternion.Euler(0,270,0), Quaternion.Euler(0,315,0), Quaternion.Euler(0,360,0),
        Quaternion.Euler(0,0,45), Quaternion.Euler(0,0,90), Quaternion.Euler(0,0,135), Quaternion.Euler(0,0,180),
        Quaternion.Euler(0,0,225), Quaternion.Euler(0,0,270), Quaternion.Euler(0,0,315), Quaternion.Euler(0,0,360)
    };

    [System.Serializable]
    public class SelectObjectDescription
    {
        public string title;
        public string description;
        public string referenceKey = ""; // If reference key is empty, then it finds the title instead.
    }

    [System.Serializable]
    public class ObjectParameter
    {
        public string title;
        public TMPro.TMP_InputField content;
    }

    private bool isSelectBoxOpened = false;
    private void Awake()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;

        selectBoxOrginalPosition = LeftVerticalLayout.position;
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Ground", description = "Floor of the level." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Spawn Point", description = "Player spawn position. Multiple spawn points will randomise the spawn position." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Goal", description = "Pass the level when is triggered.", referenceKey = "Laser Receiver" });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Laser Sender", description = "Fire a laser in a direction." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Mirror", description = "Reflect the laser in its facing direction." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Small Plane", description = "A small plane" });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Rotating Laser Sender", description = "Laser Send, but constantly rotating." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Lever", description = "Can remotely trigger other objects." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Elevator", description = "Move between multiple positions." });

        selectObjectButton = transform.GetChild(0).GetComponent<Button>();
        selectObjectButton.onClick.RemoveAllListeners();
        selectObjectButton.onClick.AddListener(() =>
        {
            if (!isObjectEditorOpened)
            {
                if (!isSelectBoxOpened)
                {
                    isSelectBoxOpened = true;
                    if (mapObjectSelectionBoxs.Count > 0) mapObjectSelectionBoxs.Clear();
                    foreach (SelectObjectDescription s in selectObjectDescriptions)
                    {
                        MapObjectSelectionBox box = Instantiate(selectObjectBoxPrefab).GetComponent<MapObjectSelectionBox>();
                        box.description.text = $"<size='12'>{s.title}</size>\n<size='8'>{s.description}</size>";
                        box.referenceKey = s.referenceKey.Equals("") ? s.title : s.referenceKey;
                        box.button.onClick.RemoveAllListeners();
                        box.button.onClick.AddListener(() =>
                        {
                            int index = objectData.GetSpawnIndex(box.referenceKey);
                            Vector3 pos = GetGroundSpawnPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                            SpawnMapObject(pos, index, true);
                            LeftVerticalLayout.gameObject.SetActive(false);
                        });
                        mapObjectSelectionBoxs.Add(box);
                        box.transform.SetParent(LeftVerticalLayout);
                        box.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    isSelectBoxOpened = false;
                    selectBoxOffset = 0;
                    foreach (MapObjectSelectionBox s in mapObjectSelectionBoxs)
                    {
                        Destroy(s.gameObject);
                    }
                    mapObjectSelectionBoxs.Clear();
                }
            }
            else
            {
                isObjectEditorOpened = false;
                SaveLogic();
            }
        });

        toTestingButton = transform.GetChild(2).GetComponent<Button>();
        toTestingButton.onClick.RemoveAllListeners();
        toTestingButton.onClick.AddListener(() =>
        {
            if (isObjectEditorOpened)
            {
                SaveLogic();
            }
            UISwitcher.Instance.SetUI("Testing");
            TestingUI.Instance.SetUp();
        });

        Instance = this;
        cam = Camera.main;
        speed = 10 * Time.deltaTime;
        groundMask = (1 << LayerMask.NameToLayer("Base")) | (1 << LayerMask.NameToLayer("Built"));
        objectMask = 1 << LayerMask.NameToLayer("Base");
        ignoreBaseMask = ~(1 << LayerMask.NameToLayer("Base"));
        currentMapObject = new CurrentMapObject();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Editor")
        {
            gameObject.SetActive(true);
            background.SetActive(true);
            GameUI.Instance.ShowCommentBar();
            GameUI.Instance.StopTimer();
            if (mapObjects.Count > 0)
            {
                foreach(MapObject m in mapObjects)
                {
                    m.GetComponent<EntityCustomAction>().CollectGarbage();
                    if (m.objectName.Equals("Spawn Point"))
                        m.gameObject.SetActive(true);
                    m.transform.position = new Vector3(m.x, m.y, m.z);
                    m.transform.rotation = allRotations[m.rotationalIndex];
                }
                GameUI.Instance.RemoveAllListeners();
            }
        }
        else
        {
            gameObject.SetActive(false);
            if (isEditorSetUp)
            {
                Remove();
            }
           if (uiName == "Studio")
            {
                background.SetActive(true);
            }
            else
            {
                background.SetActive(false);
            }
        }
    }

    private bool isEditorSetUp = false;
    public void SetUp(bool spawnDefaultMap = true)
    {
        isEditorSetUp = true;
        Remove();
        cam.transform.position = new Vector3(0, 100, -100);
        currentMapObject.mapObject = new MapObject() { objectName = "" };

        if (!spawnDefaultMap) return;
        Vector3 midPoint = (GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane))) + GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane)))) / 2;
        SpawnMapObject(midPoint, 0).gameObject.layer = LayerMask.NameToLayer("Built");
        currentMapObject.mapObject.transform.position += Vector3.up * 0.1f;
        SpawnMapObject(midPoint, 1).gameObject.layer = LayerMask.NameToLayer("Built");
    }

    public void SaveLogic()
    {
        string logic = null;
        foreach (ObjectParameter o in objectParameterBoxs)
        {
            if (o.title.Equals("Tag"))
            {
                currentMapObject.mapObject.objectTag = o.content.text;
                continue;
            }
            if (o.title.Equals("Logic"))
            {
                logic = o.content.text;
                continue;
            }
        }
        int index = objectData.GetSpawnIndex(currentMapObject.mapObject.objectName);
        if (logic.Equals(objectData.details[index].logic))
        {
            currentMapObject.mapObject.logic = "default";
        }
        else
        {
            currentMapObject.mapObject.logic = logic;
        }

        foreach (ObjectParameter o in objectParameterBoxs)
        {
            Destroy(o.content.transform.parent.gameObject);
        }
        objectParameterBoxs.Clear();
    }

    public void Remove()
    {
        if (!UISwitcher.Instance.currentUIName.Equals("Testing"))
        {
            foreach (MapObject m in mapObjects)
            {
                Destroy(m.gameObject);
            }
            mapObjects.Clear();

            currentUuid = 0;
            levelName = "undefined";
        }

        isSelectBoxOpened = false;
        foreach (MapObjectSelectionBox s in mapObjectSelectionBoxs)
        {
            Destroy(s.gameObject);
        }
        mapObjectSelectionBoxs.Clear();

        selectObjectButton.gameObject.SetActive(true);
        isSelecting = false;
        isObjectEditorOpened = false;
        foreach (ObjectParameter o in objectParameterBoxs)
        {
            Destroy(o.content.transform.parent.gameObject);
        }
        objectParameterBoxs.Clear();
    }

    private float speed = 0;
    public void ControlCamera()
    {
        if (GameUI.Instance.commentBar.isFocused) return;

        if (Input.GetKey(KeyCode.W))
        {
            cam.transform.position += Vector3.forward * speed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            cam.transform.position += Vector3.back * speed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            cam.transform.position += Vector3.left * speed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            cam.transform.position += Vector3.right * speed;
        }
    }

    LayerMask groundMask, objectMask, ignoreBaseMask;
    public Vector3 GetGroundSpawnPoint(Vector3 position)
    {
        var ray = new Ray(position, cam.transform.forward);
        RaycastHit hit;
        if (currentMapObject.mapObject.objectName.Equals("Ground"))
        {
            if (Physics.Raycast(ray, out hit, 2500, objectMask))
            {
                return hit.point;
            }
            return Vector3.zero;
        }

        if (Physics.Raycast(ray, out hit, 2500, groundMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    public MapObject SpawnMapObject(Vector3 position, int spawnIndex, bool keepFollow = false)
    {
        MapObject temp = Instantiate(objectData.details[spawnIndex].model, position, Quaternion.identity).AddComponent<MapObject>();
        temp.uuid = currentUuid;
        currentUuid++;

        temp.gameObject.AddComponent<EntityCustomAction>();
        temp.gameObject.layer = LayerMask.NameToLayer("Building");

        if (temp.GetComponent<MeshRenderer>())
        {
            currentMapObject.renderer = temp.GetComponent<MeshRenderer>();
        }
        else
        {
            for(int i = 0; i < temp.transform.childCount; i++)
            {
                if (temp.transform.GetChild(i).GetComponent<MeshRenderer>())
                {
                    currentMapObject.renderer = temp.transform.GetChild(i).GetComponent<MeshRenderer>();
                    break;
                }
            }
        }
        currentMapObject.mapObject = temp;
        temp.transform.position += Vector3.up * (currentMapObject.renderer.bounds.size.y * 0.5f);

        temp.objectName = objectData.details[spawnIndex].objectName;
        temp.x = position.x;
        temp.y = position.y;
        temp.z = position.z;
        mapObjects.Add(temp);

        this.spawnIndex = spawnIndex;
        if (keepFollow) isSelecting = true;
        return temp;
    }

    public void ConstructLevel(string json)
    {
        foreach(MapObject m in mapObjects)
        {
            Destroy(m.gameObject);
        }
        mapObjects.Clear();

        string[] seperatedJson = json.Split('\n', '\r');
        levelName = seperatedJson[0];
        for(int i = 1; i < seperatedJson.Length - 1; i++)
        {
            SerializedMapObject serializedMapObject = JsonUtility.FromJson<SerializedMapObject>(seperatedJson[i]);
            MapObject mapObject = SpawnMapObject(new Vector3(serializedMapObject.x, serializedMapObject.y, serializedMapObject.z), objectData.GetSpawnIndex(serializedMapObject.objectName));
            mapObject.logic = serializedMapObject.logic;
            mapObject.objectTag = serializedMapObject.objectTag;
            mapObject.rotationalIndex = serializedMapObject.rotationalIndex;
            mapObject.transform.rotation = allRotations[serializedMapObject.rotationalIndex];
            mapObject.gameObject.layer = LayerMask.NameToLayer("Built");
        }
    }

    public class CurrentMapObject
    {
        public MeshRenderer renderer { get; set; }
        public MapObject mapObject { get; set; }
    }

    private bool isSelecting = false;
    private bool isObjectEditorOpened = false;
    private int spawnIndex = 0;
    private Vector3 selectBoxOrginalPosition;
    private float selectBoxOffset = 0;
    private float selectMapObjectHeightOffset = 0;
    private void Update()
    {
        if (isSelectBoxOpened)
        {
            selectBoxOffset += Input.mouseScrollDelta.y * 20;
            if (selectBoxOffset < 0) selectBoxOffset = 0;
            LeftVerticalLayout.transform.position = selectBoxOrginalPosition + Vector3.up * selectBoxOffset;
        }

        if (isObjectEditorOpened) return;
        ControlCamera();
        if (!isSelecting)
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2500, ignoreBaseMask))
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                    {
                        currentMapObject.mapObject = hit.transform.GetComponent<MapObject>();
                        currentMapObject.renderer = currentMapObject.mapObject.GetComponent<MeshRenderer>();
                        currentMapObject.mapObject.gameObject.layer = LayerMask.NameToLayer("Building");
                        spawnIndex = objectData.GetSpawnIndex(currentMapObject.mapObject.objectName);
                        isSelecting = true;
                        selectMapObjectHeightOffset = 0;
                        if (isSelectBoxOpened) LeftVerticalLayout.gameObject.SetActive(false);
                    }
                }
            }
            if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2500, ignoreBaseMask) && hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                    {
                        selectBoxOffset = 0;
                        LeftVerticalLayout.transform.position = selectBoxOrginalPosition;

                        isObjectEditorOpened = true;
                        if (isSelectBoxOpened)
                        {
                            isSelectBoxOpened = false;
                            foreach (MapObjectSelectionBox s in mapObjectSelectionBoxs)
                            {
                                Destroy(s.gameObject);
                            }
                            mapObjectSelectionBoxs.Clear();
                        }

                        currentMapObject.mapObject = hit.transform.GetComponent<MapObject>();
                        spawnIndex = objectData.GetSpawnIndex(currentMapObject.mapObject.objectName);
                        LeftVerticalLayout.gameObject.SetActive(true);
                        objectParameterDescriptions.Clear();
                        objectParameterDescriptions.Add(new SelectObjectDescription() { title = $"Tag\nx: {currentMapObject.mapObject.x.ToString("F1")}, y: {currentMapObject.mapObject.y.ToString("F1")}, z: {currentMapObject.mapObject.z.ToString("F1")}", description = "", referenceKey = "small" });
                        objectParameterDescriptions.Add(new SelectObjectDescription() { title = "Logic", description = objectData.details[spawnIndex].logic, referenceKey = "large" });

                        foreach (SelectObjectDescription s in objectParameterDescriptions)
                        {
                            TMPro.TMP_InputField tempTxt = null;
                            GameObject tempObj = null;
                            if (s.referenceKey.Equals("small"))
                            {
                                tempObj = Instantiate(objectParameterBoxPrefab);
                                tempTxt = tempObj.transform.GetChild(1).GetComponent<TMPro.TMP_InputField>();
                            }
                            else
                            {
                                tempObj = Instantiate(largeObjectParameterBoxPrefab);
                                tempTxt = tempObj.transform.GetChild(1).GetComponent<TMPro.TMP_InputField>();
                            }
                            tempObj.transform.SetParent(LeftVerticalLayout);
                            tempObj.transform.localScale = Vector3.one;
                            tempObj.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = s.title;
                            if (s.title.Equals("Tag"))
                                tempTxt.text = currentMapObject.mapObject.objectTag.Equals("") ? s.description : currentMapObject.mapObject.objectTag;
                            if (s.title.Equals("Logic"))
                                tempTxt.text = currentMapObject.mapObject.logic.Equals("default") ? s.description : currentMapObject.mapObject.logic;
                            if (s.description.Equals("non-editable"))
                                tempTxt.enabled = false;
                            if (s.referenceKey.Equals("large"))
                            {
                                if (currentMapObject.mapObject.objectName.Equals("Setting Cube"))
                                {
                                    tempObj.transform.GetChild(2).gameObject.SetActive(false);
                                    tempObj.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() =>
                                    {
                                        isObjectEditorOpened = false;
                                        SaveLogic();
                                    });
                                }
                                else
                                {
                                    tempObj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
                                    {
                                        mapObjects.Remove(currentMapObject.mapObject);
                                        Destroy(currentMapObject.mapObject.gameObject);
                                        isSelecting = false;
                                        isObjectEditorOpened = false;
                                        foreach (ObjectParameter o in objectParameterBoxs)
                                        {
                                            Destroy(o.content.transform.parent.gameObject);
                                        }
                                        objectParameterBoxs.Clear();

                                    });

                                    tempObj.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() =>
                                    {
                                        isSelecting = false;
                                        isObjectEditorOpened = false;
                                        SaveLogic();
                                    });
                                }
                            }
                            objectParameterBoxs.Add(new ObjectParameter() { title = s.title, content = tempTxt });
                        }
                    }
                }
            }
        }
        else
        {
            selectMapObjectHeightOffset += Input.mouseScrollDelta.y * 0.25f;
            Vector3 raycastPosition = GetGroundSpawnPoint(cam.ScreenToWorldPoint(Input.mousePosition));
            Vector3 offsetPosition = Vector3Int.FloorToInt(raycastPosition / objectData.details[spawnIndex].spacing);
            offsetPosition = offsetPosition * objectData.details[spawnIndex].spacing;
            currentMapObject.mapObject.transform.position = new Vector3(offsetPosition.x, raycastPosition.y, offsetPosition.z) + Vector3.up * (currentMapObject.renderer.bounds.size.y * 0.5f) + Vector3.up * selectMapObjectHeightOffset;
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                currentMapObject.mapObject.gameObject.layer = LayerMask.NameToLayer("Built");
                if (currentMapObject.mapObject.objectName.Equals("Ground")) currentMapObject.mapObject.transform.position += Vector3.up * 0.1f;
                currentMapObject.mapObject.x = currentMapObject.mapObject.transform.position.x;
                currentMapObject.mapObject.y = currentMapObject.mapObject.transform.position.y;
                currentMapObject.mapObject.z = currentMapObject.mapObject.transform.position.z;
                isSelecting = false;
                if (isSelectBoxOpened) LeftVerticalLayout.gameObject.SetActive(true);
            }
            if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
            {
                currentMapObject.mapObject.rotationalIndex = (currentMapObject.mapObject.rotationalIndex + 1) % allRotations.Length;
                currentMapObject.mapObject.transform.rotation = allRotations[currentMapObject.mapObject.rotationalIndex];
            }
        }
    }
}

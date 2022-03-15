using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MPUIKIT;

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
    private Button selectObjectButton;
    public Transform LeftVerticalLayout;
    public GameObject selectObjectBoxPrefab, objectParameterBoxPrefab, largeObjectParameterBoxPrefab;

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
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Goal", description = "Pass the level when is triggered." });
        selectObjectDescriptions.Add(new SelectObjectDescription() { title = "Laser Sender", description = "Fire a laser in a direction." });

        selectObjectButton = transform.GetChild(1).GetComponent<Button>();
        selectObjectButton.onClick.RemoveAllListeners();
        selectObjectButton.onClick.AddListener(() =>
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
                    mapObjectSelectionBoxs.Add(box);
                    box.transform.SetParent(LeftVerticalLayout);
                    box.transform.localScale = Vector3.one;
                }
            }
            else
            {
                isSelectBoxOpened = false;
                selectBoxOffset = 0;
                foreach(MapObjectSelectionBox s in mapObjectSelectionBoxs)
                {
                    Destroy(s.gameObject);
                }
                mapObjectSelectionBoxs.Clear();
            }
        });

        Instance = this;
        cam = Camera.main;
        speed = 20 * Time.deltaTime;
        groundMask = (1 << LayerMask.NameToLayer("Base")) | (1 << LayerMask.NameToLayer("Built"));
        objectMask = 1 << LayerMask.NameToLayer("Base");
        currentMapObject = new CurrentMapObject();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Editor")
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
            if (isEditorSetUp)
            {
                Remove();
            }
           
        }
    }

    private bool isEditorSetUp = false;
    public void SetUp()
    {
        isEditorSetUp = true;
        Remove();

        GameUI.Instance.ShowCommentBar();
        cam.transform.position = new Vector3(0, 100, -100);
        currentMapObject.mapObject = new MapObject() { objectName = "" };
        Vector3 midPoint = (GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane))) + GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane)))) / 2;
        SpawnMapObject(midPoint, 0).gameObject.layer = LayerMask.NameToLayer("Built");
        if (currentMapObject.mapObject.objectName.Equals("Ground")) currentMapObject.mapObject.transform.position += Vector3.up * 0.1f;
        SpawnMapObject(midPoint, 1).gameObject.layer = LayerMask.NameToLayer("Built");
    }

    public void Remove()
    {
        foreach (MapObject m in mapObjects)
        {
            Destroy(m.gameObject);
        }
        mapObjects.Clear();

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

    LayerMask groundMask, objectMask;
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
        temp.gameObject.layer = LayerMask.NameToLayer("Building");
        currentMapObject.renderer = temp.GetComponent<MeshRenderer>();
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
    private void Update()
    {
        ControlCamera();
        if (isSelectBoxOpened)
        {
            selectBoxOffset += Input.mouseScrollDelta.y * 20;
            if (selectBoxOffset < 0) selectBoxOffset = 0;
            LeftVerticalLayout.transform.position = selectBoxOrginalPosition + Vector3.up * selectBoxOffset;
        }

        if (isObjectEditorOpened) return;
        if (!isSelecting)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2500, groundMask))
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                    {
                        currentMapObject.mapObject = hit.transform.GetComponent<MapObject>();
                        currentMapObject.renderer = currentMapObject.mapObject.GetComponent<MeshRenderer>();
                        currentMapObject.mapObject.gameObject.layer = LayerMask.NameToLayer("Building");
                        spawnIndex = objectData.GetSpawnIndex(currentMapObject.mapObject.objectName);
                        isSelecting = true;
                        if (isSelectBoxOpened) LeftVerticalLayout.gameObject.SetActive(false);
                    }
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 2500, groundMask) && hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                {
                    if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                    {
                        isSelecting = true;
                        isObjectEditorOpened = true;
                        selectObjectButton.gameObject.SetActive(false);
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
                        objectParameterDescriptions.Add(new SelectObjectDescription() { title = "Tag", description = "", referenceKey = "small" });
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
                            tempTxt.text = s.description;
                            objectParameterBoxs.Add(new ObjectParameter() { title = s.title, content = tempTxt });
                        }
                    }
                }
            }
        }
        else
        {
            Vector3 raycastPosition = GetGroundSpawnPoint(cam.ScreenToWorldPoint(Input.mousePosition));
            Vector3 offsetPosition = Vector3Int.FloorToInt(raycastPosition / objectData.details[spawnIndex].spacing);
            offsetPosition = offsetPosition * objectData.details[spawnIndex].spacing;
            currentMapObject.mapObject.transform.position = new Vector3(offsetPosition.x, raycastPosition.y, offsetPosition.z) + Vector3.up * (currentMapObject.renderer.bounds.size.y * 0.5f);
            if (Input.GetMouseButtonDown(0))
            {
                currentMapObject.mapObject.gameObject.layer = LayerMask.NameToLayer("Built");
                if (currentMapObject.mapObject.objectName.Equals("Ground")) currentMapObject.mapObject.transform.position += Vector3.up * 0.1f;
                currentMapObject.mapObject.x = currentMapObject.mapObject.transform.position.x;
                currentMapObject.mapObject.y = currentMapObject.mapObject.transform.position.y;
                currentMapObject.mapObject.z = currentMapObject.mapObject.transform.position.z;
                isSelecting = false;
                if (isSelectBoxOpened) LeftVerticalLayout.gameObject.SetActive(true);
            }
        }
    }
}

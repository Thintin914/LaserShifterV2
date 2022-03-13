using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorUI : MonoBehaviour
{
    public static EditorUI Instance;
    public ObjectData objectData;
    private Camera cam;
    public List<MapObject> mapObjects = new List<MapObject>();
    private CurrentMapObject currentMapObject;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        speed = 20 * Time.deltaTime;
        groundMask = (1 << LayerMask.NameToLayer("Base")) | (1 << LayerMask.NameToLayer("Built"));
        currentMapObject = new CurrentMapObject();
    }

    public void SetUp()
    {
        cam.transform.position = new Vector3(0, 100, -100);
        Vector3 midPoint = (GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane))) + GetGroundSpawnPoint(cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane)))) / 2;
        SpawnMapObject(midPoint, 0).gameObject.layer = LayerMask.NameToLayer("Built");
        if (currentMapObject.mapObject.objectName.Equals("Ground")) currentMapObject.mapObject.transform.position += Vector3.up * 0.1f;
        SpawnMapObject(midPoint, 1).gameObject.layer = LayerMask.NameToLayer("Built");
    }

    private float speed = 0;
    public void ControlCamera()
    {
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

    LayerMask groundMask;
    private Vector3 GetGroundSpawnPoint(Vector3 position)
    {
        var ray = new Ray(position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, 2500, groundMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    public MapObject SpawnMapObject(Vector3 position, int spawnIndex)
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
        return temp;
    }

    public class CurrentMapObject
    {
        public MeshRenderer renderer { get; set; }
        public MapObject mapObject { get; set; }
    }

    private bool isSelecting = false;
    private int spawnIndex = 0;
    private void Update()
    {
        ControlCamera();
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
                isSelecting = false;
            }
        }
    }
}

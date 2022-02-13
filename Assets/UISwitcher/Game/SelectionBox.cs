using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;

public class SelectionBox : MonoBehaviour
{
    public GameObject imagePrefab;
    public int gridSize;
    private int page = 0;
    private int ignoreMask;

    public List<ObjectDetails> objectDetails;
    public class CurrentHoldingObject
    {
        public GameObject mapObject { get; set; }
        public int rotationIndex { get; set; }
        public float height { get; set; }
    }
    public CurrentHoldingObject currentHoldingObject;

    public string[] settingCubeKeywords = new string[]
    {
        "SmallMap", "MediumMap", "LargeMap"
    };

    public void CreateGrid()
    {
        page = 0;
        ignoreMask = ~(1 << LayerMask.NameToLayer("Building"));
        currentHoldingObject = new CurrentHoldingObject();
        ObjectDescription.Description description = CommonUI.Instance.mapObjectData.GetUnobtainableByName("SettingCube");
        objectDetails.Add(Instantiate(description.prefab, Vector3.zero + Vector3.up * 0.5f, Quaternion.identity).AddComponent<ObjectDetails>());
        objectDetails[0].data = SetDescriptionParametersToDictionary(description.data);
        objectDetails[objectDetails.Count - 1].gameObject.layer = LayerMask.NameToLayer("Built");

        SetSettingCube(objectDetails[0]);
        for (int i = 0; i < gridSize; i++)
        {
            GameObject temp = Instantiate(imagePrefab, transform);
            temp.GetComponent<Button>().onClick.AddListener(() =>
            {

            });
        }

        NotDraggingObject();
    }

    public void RemoveGrid()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        foreach (ObjectDetails d in objectDetails)
        {
            Destroy(d.gameObject);
        }
        objectDetails.Clear();
        if (currentHoldingObject != null)
            Destroy(currentHoldingObject.mapObject);

        if (dragHoldingObjectCancelSource != null)
        {
            dragHoldingObjectCancelSource.Cancel();
            dragHoldingObjectCancelSource.Dispose();
            dragHoldingObjectCancelSource = null;
        }
        if (notDraggingObjectCancelSource != null)
        {
            notDraggingObjectCancelSource.Cancel();
            notDraggingObjectCancelSource.Dispose();
            notDraggingObjectCancelSource = null;
        }
    }

    public void SetSettingCube(ObjectDetails setting)
    {
        List<ObjectDetails> temp = new List<ObjectDetails>();
        foreach (ObjectDetails d in objectDetails)
        {
            if (System.Array.IndexOf(settingCubeKeywords, d.objName) != -1)
            {
                temp.Add(d);
            }
        }
        foreach(ObjectDetails d in temp)
        {
            objectDetails.Remove(d);
            Destroy(d.gameObject);
        }
        temp.Clear();

        foreach (KeyValuePair<string, string> pairs in setting.data)
        {
            if (pairs.Key == "Map Size")
            {
                ObjectDescription.Description description = CommonUI.Instance.mapObjectData.GetUnobtainableByName(pairs.Value);
                objectDetails.Add(Instantiate(description.prefab, Vector3.zero, Quaternion.identity).AddComponent<ObjectDetails>());
            }
        }
    }

    public Dictionary<string, string> SetDescriptionParametersToDictionary(ObjectDescription.DescriptionParameter[] parameters)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        for(int i = 0; i < parameters.Length; i++)
        {
            dict.Add(parameters[i].name, parameters[i].value);
        }
        return dict;
    }

    public void SpawnMapObject(ObjectDescription.Description description)
    {
        if (currentHoldingObject.mapObject != null) return;

        currentHoldingObject.mapObject = Instantiate(description.prefab);
        currentHoldingObject.rotationIndex = 0;
        DragCurrentHoldingObject();
    }

    CancellationTokenSource dragHoldingObjectCancelSource = null;
    public async void DragCurrentHoldingObject()
    {
        if (dragHoldingObjectCancelSource != null)
        {
            dragHoldingObjectCancelSource.Cancel();
            dragHoldingObjectCancelSource.Dispose();
            dragHoldingObjectCancelSource = null;
        }
        dragHoldingObjectCancelSource = new CancellationTokenSource();

        currentHoldingObject.height = currentHoldingObject.mapObject.GetComponent<Renderer>().bounds.size.y;
        try
        {
            while (true)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000, ignoreMask))
                {
                    currentHoldingObject.mapObject.transform.position = hit.point + Vector3.up * currentHoldingObject.height;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    currentHoldingObject.mapObject.layer = LayerMask.NameToLayer("Built");
                    currentHoldingObject.mapObject.GetComponent<ObjectDetails>().rotationIndex = currentHoldingObject.rotationIndex;
                    currentHoldingObject.mapObject = null;

                    if (dragHoldingObjectCancelSource != null)
                    {
                        dragHoldingObjectCancelSource.Cancel();
                        dragHoldingObjectCancelSource.Dispose();
                        dragHoldingObjectCancelSource = null;
                    }

                    await Task.Yield();
                    NotDraggingObject();
                }
                await Task.Yield();
            }
        }
        catch (System.OperationCanceledException) when (dragHoldingObjectCancelSource.IsCancellationRequested)
        {
            return;
        }
    }

    CancellationTokenSource notDraggingObjectCancelSource = null;
    public async void NotDraggingObject()
    {
        if (notDraggingObjectCancelSource != null)
        {
            notDraggingObjectCancelSource.Cancel();
            notDraggingObjectCancelSource.Dispose();
            notDraggingObjectCancelSource = null;
        }
        notDraggingObjectCancelSource = new CancellationTokenSource();
        try
        {
            while (true)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000))
                    {
                        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Built"))
                        {
                            currentHoldingObject.mapObject = hit.transform.gameObject;
                            currentHoldingObject.rotationIndex = hit.transform.GetComponent<ObjectDetails>().rotationIndex;
                            currentHoldingObject.mapObject.layer = LayerMask.NameToLayer("Building");

                            if (notDraggingObjectCancelSource != null)
                            {
                                notDraggingObjectCancelSource.Cancel();
                                notDraggingObjectCancelSource.Dispose();
                                notDraggingObjectCancelSource = null;
                            }

                            DragCurrentHoldingObject();
                        }
                    }
                }
                await Task.Yield();
            }
        }
        catch (System.OperationCanceledException) when (notDraggingObjectCancelSource.IsCancellationRequested)
        {
            return;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestingUI : MonoBehaviour
{
    public static TestingUI Instance;
    public GameObject playerOuterPrefab;
    public GameObject[] playerPrefabs;
    [HideInInspector]public List<GameObject> testingPlayers = new List<GameObject>();
    private Button resultButton;
    public string result = "Fail";
    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        resultButton = transform.GetChild(0).GetComponent<Button>();
        resultButton.onClick.RemoveAllListeners();
        resultButton.onClick.AddListener(() =>
        {
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
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Testing")
        {
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

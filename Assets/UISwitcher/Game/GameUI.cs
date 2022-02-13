using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GameUI : MonoBehaviour
{
    public TMP_InputField commentBar;
    public SelectionBox selectionBox;

    private void Awake()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        commentBar = transform.GetChild(0).GetComponent<TMP_InputField>();
        selectionBox = transform.GetChild(1).GetComponent<SelectionBox>();

        commentBar.onEndEdit.RemoveAllListeners();
        commentBar.onEndEdit.AddListener(async (value) =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                commentBar.text = "";
                if (value[0] == '/')
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
                        UISwitcher.Instance.SetUI("Editor");
                        CommonUI.Instance.LeaveRoom();
                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show($"Change To\nMap Editor", 2);
                    }

                    commentBar.gameObject.SetActive(true);
                }

                commentBar.ActivateInputField();
            }
        });
    }

    public void SwitchUI(string uiName)
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        commentBar.gameObject.SetActive(true);
        selectionBox.RemoveGrid();

        if (uiName == "Game")
        {
            gameObject.SetActive(true);
        }
        else if (uiName == "Editor")
        {
            gameObject.SetActive(true);
            selectionBox.gameObject.SetActive(true);
            selectionBox.CreateGrid();
        }
        else 
        {
            gameObject.SetActive(false);
        }
    }
}

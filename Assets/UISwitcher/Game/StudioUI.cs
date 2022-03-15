using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class StudioUI : MonoBehaviour
{
    public static StudioUI Instance;

    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);
    }

    public void SwitchUI(string uiName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        if (uiName == "Studio")
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void GoToStudio()
    {
        Debug.Log("Go To Studio");
        GameUI.Instance.ShowCommentBar();
    }
}

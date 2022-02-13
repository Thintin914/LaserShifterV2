using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplashScreenUI : MonoBehaviour
{
    private Button PlayBtn;
    private Button QuitBtn;
    private Button SettingBtn;


    private void Awake()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        PlayBtn = transform.GetChild(2).GetComponent<Button>();
        QuitBtn = transform.GetChild(3).GetComponent<Button>();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "SplashScreen")
        {
            gameObject.SetActive(true);
            PlayBtn.onClick.RemoveAllListeners();
            PlayBtn.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("Main");
            });

            QuitBtn.onClick.RemoveAllListeners();
            QuitBtn.onClick.AddListener(() =>
            {
                Application.Quit();
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
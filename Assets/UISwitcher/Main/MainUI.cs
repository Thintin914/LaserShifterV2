using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{
    private Button registerButton;
    private Button loginButton;
    private Button backButton;

    private void Awake()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        registerButton = transform.GetChild(1).GetComponent<Button>();
        loginButton = transform.GetChild(2).GetComponent<Button>();
        backButton = transform.GetChild(3).GetComponent<Button>();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Main")
        {
            gameObject.SetActive(true);
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("Register");
            });

            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("Login");
            });

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("SplashScreen");
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

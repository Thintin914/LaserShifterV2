using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using Firebase.Firestore;

public class LoginUI : MonoBehaviour
{
    private TMP_InputField usernameField;
    private TMP_InputField passwordField;
    private Button comfirmButton;
    private Button closeButton;
    private Button guestButton;

    private void Awake()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        usernameField = transform.GetChild(2).GetComponent<TMP_InputField>();
        passwordField = transform.GetChild(4).GetComponent<TMP_InputField>();
        comfirmButton = transform.GetChild(5).GetComponent<Button>();
        closeButton = transform.GetChild(6).GetComponent<Button>();
        guestButton = transform.GetChild(7).GetComponent<Button>();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Login")
        {
            gameObject.SetActive(true);

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("Main");
            });

            usernameField.onEndEdit.RemoveAllListeners();
            usernameField.onEndEdit.AddListener((value) =>
            {
                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                {
                    passwordField.ActivateInputField();
                }
            });

            passwordField.onEndEdit.RemoveAllListeners();
            passwordField.onEndEdit.AddListener((value) =>
            {
                if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
                {
                    comfirmButton.onClick.Invoke();
                }
            });

            guestButton.onClick.RemoveAllListeners();
            guestButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);

                CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                CommonUI.Instance.popupNotice.Show($"Welcome,\nGuest#{Random.Range(1000, 9999)}", 2);
                CommonUI.Instance.username = usernameField.text;

                UISwitcher.Instance.SetUI("Studio");
                StudioUI.Instance.GoToStudio();
            });

            comfirmButton.onClick.RemoveAllListeners();
            comfirmButton.onClick.AddListener(async () =>
            {
                if (string.IsNullOrEmpty(usernameField.text) || string.IsNullOrEmpty(passwordField.text) || usernameField.text[0] == ' ')
                {
                    CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                    CommonUI.Instance.popupNotice.Show("Invalid Input", 2);
                    return;
                }

                comfirmButton.gameObject.SetActive(false);

                DocumentReference usernameDocRef = CommonUI.db.Collection("users").Document(usernameField.text);
                await usernameDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.Result.Exists)
                    {
                        Dictionary<string, object> dict = task.Result.ToDictionary();
                        foreach(KeyValuePair<string, object> pair in dict)
                        {
                            if (pair.Key == "pw" && passwordField.text == string.Format("{0}", pair.Value))
                            {
                                gameObject.SetActive(false);
                                Debug.Log("Login Successfully");

                                CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                                CommonUI.Instance.popupNotice.Show($"Welcome,\n{usernameField.text}", 2);
                                CommonUI.Instance.username = usernameField.text;

                                UISwitcher.Instance.SetUI("Studio");
                                StudioUI.Instance.GoToStudio();
                                comfirmButton.gameObject.SetActive(true);
                                return;
                            }
                        }
                        CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                        CommonUI.Instance.popupNotice.Show("Wrong Password", 2);
                    }
                    else
                    {
                        CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                        CommonUI.Instance.popupNotice.Show("User Not Found", 2);
                    }
                });

                comfirmButton.gameObject.SetActive(true);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

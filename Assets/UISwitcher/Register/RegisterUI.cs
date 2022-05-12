using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Extensions;
using Firebase.Firestore;

public class RegisterUI : MonoBehaviour
{
    private TMP_InputField usernameField;
    private TMP_InputField passwordField;
    private Button comfirmButton;
    private Button closeButton;
    
    void Start()
    {
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        usernameField = transform.GetChild(3).GetComponent<TMP_InputField>();
        passwordField = transform.GetChild(5).GetComponent<TMP_InputField>();
        comfirmButton = transform.GetChild(6).GetComponent<Button>();
        closeButton = transform.GetChild(7).GetComponent<Button>();
    }

    public void SwitchUI(string uiName)
    {
        if (uiName == "Register")
        {
            gameObject.SetActive(true);
            usernameField.text = "";
            passwordField.text = "";

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                UISwitcher.Instance.SetUI("Main");
            });

            usernameField.onValueChanged.RemoveAllListeners();
            usernameField.onValueChanged.AddListener((value) =>
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                if (string.IsNullOrEmpty(value) || value[0] == ' ')
                {
                    CommonUI.Instance.popupNotice.Show($"Invalid Name\n'{value}'", 2);
                }
                if (value.Length > 17)
                {
                    CommonUI.Instance.popupNotice.Show($"Exceed Length Limit\n'{value}'", 2);
                }
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

            passwordField.onValueChanged.RemoveAllListeners();
            passwordField.onValueChanged.AddListener((value) =>
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                if (string.IsNullOrEmpty(value) || value[0] == ' ')
                {
                    CommonUI.Instance.popupNotice.Show($"Invalid Password\n'{value}'", 2);
                }
                if (value.Length > 17)
                {
                    CommonUI.Instance.popupNotice.Show($"Exceed Length Limit\n'{value}'", 2);
                }
            });

            comfirmButton.onClick.RemoveAllListeners();
            comfirmButton.onClick.AddListener(async() =>
            {
                comfirmButton.gameObject.SetActive(false);

                bool isNameOK = true;
                bool isPasswordOK = true;
                if (string.IsNullOrEmpty(usernameField.text) || usernameField.text[0] == ' ' || usernameField.text.Length > 17)
                {
                    isNameOK = false;
                }
                if (string.IsNullOrEmpty(passwordField.text) || passwordField.text[0] == ' ' || passwordField.text.Length > 17)
                {
                    isPasswordOK = false;
                }

                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                if (isNameOK && isPasswordOK)
                {
                    DocumentReference usernameDocRef = CommonUI.db.Collection("users").Document(usernameField.text);
                    DocumentSnapshot usernameSnapshot = await usernameDocRef.GetSnapshotAsync();

                    if (!usernameSnapshot.Exists)
                    {
                        DocumentReference environmentDocRef = CommonUI.db.Collection("environment").Document("totalUser");
                        DocumentSnapshot environmentSnapshot = await environmentDocRef.GetSnapshotAsync();
                        int totalUser = 0;
                        Dictionary<string, object> totalUserDict = environmentSnapshot.ToDictionary();
                        foreach (KeyValuePair<string, object> pair in totalUserDict)
                        {
                            totalUser = int.Parse(string.Format("{0}", pair.Value));
                        }
                        Dictionary<string, object> environmentDict = new Dictionary<string, object>
                        {
                            {"total", totalUser + 1 }
                        };
                        await environmentDocRef.SetAsync(environmentDict).ContinueWithOnMainThread(task => Debug.Log("Updated Total User"));

                        Dictionary<string, object> userDict = new Dictionary<string, object>
                        {
                            {"name", usernameField.text },
                            {"pw", passwordField.text },
                            {"passedLevel", 0 },
                            {"winAsFirst", 0 },
                            {"badge", new string[0] },
                            {"createdLevels", 0 },
                            {"id", totalUser + 1}
                        };
                        await usernameDocRef.SetAsync(userDict).ContinueWithOnMainThread(task => Debug.Log("Added User"));

                        CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                        CommonUI.Instance.popupNotice.Show("Registered", 2);
                        UISwitcher.Instance.SetUI("Main");
                    }
                    else
                    {
                        CommonUI.Instance.popupNotice.Show($"The Name\n'{usernameField.text}'\nHas Existed", 2);
                    }
                }
                else
                {
                    string nameSentance = isNameOK ? "Valid Name" : "Invalid Name";
                    string passwordSentance = isPasswordOK ? "Valid Password" : "Invalid Password";
                    CommonUI.Instance.popupNotice.Show($"{nameSentance}\n{passwordSentance}", 2);
                }

                comfirmButton.gameObject.SetActive(true);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}

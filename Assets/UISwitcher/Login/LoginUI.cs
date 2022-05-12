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

        usernameField = transform.GetChild(3).GetComponent<TMP_InputField>();
        passwordField = transform.GetChild(5).GetComponent<TMP_InputField>();
        comfirmButton = transform.GetChild(6).GetComponent<Button>();
        closeButton = transform.GetChild(7).GetComponent<Button>();
        guestButton = transform.GetChild(8).GetComponent<Button>();
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
            guestButton.onClick.AddListener(async () =>
            {
                gameObject.SetActive(false);

                CommonUI.Instance.popupNotice.SetColor(16, 23, 34, 0);
                int rand = Random.Range(1000, 9999);
                CommonUI.Instance.popupNotice.Show($"Welcome,\nGuest#{rand}", 2);
                CommonUI.Instance.username = "Guest#" + rand;
                CommonUI.Instance.isGuest = true;    

                UISwitcher.Instance.SetUI("Studio");
                StudioUI.Instance.GoToStudio();

                DocumentReference totalUserDocRef = CommonUI.db.Collection("environment").Document("totalUser");
                DocumentSnapshot totalUserSnapshot = await totalUserDocRef.GetSnapshotAsync();
                Dictionary<string, object> totalUserDict = totalUserSnapshot.ToDictionary();
                foreach (KeyValuePair<string, object> pair in totalUserDict)
                {
                    LevelRound.Instance.totalUser = int.Parse(string.Format("{0}", pair.Value));
                }
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
                await usernameDocRef.GetSnapshotAsync().ContinueWithOnMainThread(async task =>
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

                                DocumentReference totalUserDocRef = CommonUI.db.Collection("environment").Document("totalUser");
                                DocumentSnapshot totalUserSnapshot = await totalUserDocRef.GetSnapshotAsync();
                                Dictionary<string, object> totalUserDict = totalUserSnapshot.ToDictionary();
                                foreach (KeyValuePair<string, object> pair2 in totalUserDict)
                                {
                                    LevelRound.Instance.totalUser = int.Parse(string.Format("{0}", pair2.Value));
                                }
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

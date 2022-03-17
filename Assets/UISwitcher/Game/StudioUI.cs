using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;

public class StudioUI : MonoBehaviour
{
    public static StudioUI Instance;

    private Button reeditButton;

    private void Awake()
    {
        Instance = this;
        UISwitcher.Instance.SwitchUIEvent += SwitchUI;
        gameObject.SetActive(false);

        reeditButton = transform.GetChild(0).GetComponent<Button>();
        reeditButton.onClick.RemoveAllListeners();
        reeditButton.onClick.AddListener(async() =>
        {
            DocumentReference levelDocRef = CommonUI.db.Collection("levels").Document(CommonUI.Instance.username);
            DocumentSnapshot levelSnapShot = await levelDocRef.GetSnapshotAsync();

            if (levelSnapShot.Exists)
            {

            }
            else
            {
                CommonUI.Instance.popupNotice.SetColor(205, 46, 83, 0);
                CommonUI.Instance.popupNotice.Show("Map added to database. Don't press again.", 3);
            }
        });
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

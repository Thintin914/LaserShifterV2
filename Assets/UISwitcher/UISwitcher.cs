using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder (-1)]
public class UISwitcher : MonoBehaviour
{
    public delegate void SwitchUIDelegate(string uiName);
    public event SwitchUIDelegate SwitchUIEvent;

    public static UISwitcher Instance;

    private void Awake()
    {
        Instance = this;
    }

    public string currentUIName;
    public void SetUI(string uiName)
    {
        Debug.Log("Set UI To " + uiName);
        currentUIName = uiName;
        SwitchUIEvent?.Invoke(uiName);
    }
}

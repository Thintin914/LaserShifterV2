using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FirebaseInitializer : MonoBehaviour
{
    public UnityEvent onFirebaseInitialized;

    private void Start()
    {
        StartCoroutine(CheckAndFixDependenciesCoroutine());
    }

    private IEnumerator CheckAndFixDependenciesCoroutine()
    {
        var checkDependenciesTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => checkDependenciesTask.IsCompleted);
        var denpendencyStatus = checkDependenciesTask.Result;
        if (denpendencyStatus == Firebase.DependencyStatus.Available)
        {
            Debug.Log("Firebase Ready");
            onFirebaseInitialized.Invoke();
        }
        else
        {
            Debug.Log("Firebase Failed");
        }
    }
}

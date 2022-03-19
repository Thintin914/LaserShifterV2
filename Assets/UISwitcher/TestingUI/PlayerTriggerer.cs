using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerTriggerer : MonoBehaviour
{
    public TMPro.TextMeshPro username;
    public PhotonView pv;


    private void Awake()
    {
        username.text = CommonUI.Instance.username;
        pv = GetComponent<PhotonView>();
    }

    private void Update()
    {
        username.transform.forward = CommonUI.Instance.currentCamera.transform.forward;
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Trigger(2.5f);
        }
    }

    public void Trigger(float range)
    {
        if (UISwitcher.Instance.currentUIName.Equals("Testing"))
        {
            foreach(MapObject m in EditorUI.Instance.mapObjects)
            {
                if (Vector3.Distance(m.transform.position, transform.position) <= range)
                {   
                    if (m.GetComponent<EntityCustomAction>().cycle != null)
                    m.GetComponent<EntityCustomAction>().cycle.Trigger("onTrigger", transform);
                }
            }
        }
    }
}

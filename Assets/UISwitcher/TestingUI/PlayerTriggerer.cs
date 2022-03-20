using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;

public class PlayerTriggerer : MonoBehaviour
{
    public TMPro.TextMeshPro username;
    public PhotonView pv;
    public Controller controller;

    private void Awake()
    {
        username.text = CommonUI.Instance.username;
        pv = GetComponent<PhotonView>();
        controller = GetComponent<Controller>();
    }

    private void Update()
    {
        username.transform.forward = CommonUI.Instance.currentCamera.transform.forward;
        if (pv.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (UISwitcher.Instance.currentUIName.Equals("Testing"))
                {
                    Trigger(2.5f);
                }
                else
                {
                    pv.RPC("RemoteTrigger", RpcTarget.All, pv.ViewID);
                }
            }
        }
    }

    public void Trigger(float range)
    {
        foreach (MapObject m in EditorUI.Instance.mapObjects)
        {
            if (Vector3.Distance(m.transform.position, transform.position) <= range)
            {
                if (m.GetComponent<EntityCustomAction>().cycle != null)
                    m.GetComponent<EntityCustomAction>().cycle.Trigger("onTrigger", transform);
            }
        }
    }

    public void SetPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }

    [PunRPC]
    public void RemoteTrigger(int viewId)
    {
        PhotonView.Find(viewId).GetComponent<PlayerTriggerer>().Trigger(2.5f);
    }

    [PunRPC]
    public void ChangeUsername(string name)
    {
        username.text = name;
    }
}

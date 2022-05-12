using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;

public class PlayerTriggerer : MonoBehaviour
{
    public TMPro.TextMeshPro username;
    public TMPro.TextMeshPro talk;
    public PhotonView pv;
    public Controller controller;

    private void Awake()
    {
        username.text = CommonUI.Instance.username;
        talk.text = null;
        pv = GetComponent<PhotonView>();
        controller = GetComponent<Controller>();
    }

    private void Update()
    {
        username.transform.forward = CommonUI.Instance.currentCamera.transform.forward;
        talk.transform.forward = CommonUI.Instance.currentCamera.transform.forward;
        if (UISwitcher.Instance.currentUIName.Equals("Testing"))
        {
            if (Input.GetKeyDown(KeyCode.Z))
                Trigger(2.5f);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Z) && pv.IsMine)
                pv.RPC("RemoteTrigger", RpcTarget.All, pv.ViewID);
        }
        if (transform.position.y <= -30)
        {
            SetPosition(spawnPoint);
            DeathEffect();
        }
    }

    private async void DeathEffect()
    {
        await CommonUI.Instance.PixelateCamera();
        await CommonUI.Instance.UnpixelateCamera();
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

    public Vector3 spawnPoint = Vector3.zero;
    public void SetPosition(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        spawnPoint = position;
    }

    [PunRPC]
    public void RemoteTrigger(int viewId)
    {
        PhotonView.Find(viewId).GetComponent<PlayerTriggerer>().Trigger(2.5f);
    }
}

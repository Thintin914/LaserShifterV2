using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTriggerer : MonoBehaviour
{
    private void Update()
    {
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
                    m.GetComponent<EntityCustomAction>().cycle.Trigger("onTrigger");
                }
            }
        }
    }
}

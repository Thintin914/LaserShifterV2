using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using System.Threading.Tasks;
using System.Threading;
using System;
using Photon.Pun;

public class EntityCustomAction : MonoBehaviour
{
    internal LuaEnv luaEnv = new LuaEnv();
    private LuaTable scriptEnv;
    public MapObject mapObject;
    public LifeCycle cycle;

    public List<string> allFunctions = new List<string>();

    public void onLoadLogic(string logic)
    {
        mapObject = GetComponent<MapObject>();

        luaEnv = new LuaEnv();
        scriptEnv = luaEnv.NewTable();
        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", mapObject);
        scriptEnv.Set("this", this);
        scriptEnv.Set("Game", GameUI.Instance);
        scriptEnv.Set("UISwitcher", UISwitcher.Instance);

        string[] allLines = logic.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int k = 0; k < allLines.Length; k++)
        {
            allLines[k] = allLines[k].Replace("\t", string.Empty).Trim(); ;
            if (allLines[k].Length >= 8)
            {
                if (allLines[k].Substring(0, 8).Equals("function"))
                {
                    string keyword = allLines[k].Replace("function", string.Empty);
                    int bracketIndex = keyword.IndexOf('(');
                    keyword = keyword.Substring(0, bracketIndex).Trim();

                    allFunctions.Add(keyword);
                }
            }
        }

        luaEnv.DoString(GameUI.luaLibrary + "\n" + logic, "chunk", scriptEnv);

        cycle = new LifeCycle()
        {
            className = mapObject.objectTag,
        };

        for (int k = 0; k < allFunctions.Count; k++)
        {
            LifeCycle.CustomAction action = null;
            scriptEnv.Get(allFunctions[k], out action);
            cycle.functions.Add(allFunctions[k], action);
        }
        cycle.Trigger("start");
    }

    private void OnDestroy()
    {
        CollectGarbage();
    }

    private void OnDisable()
    {
        CollectGarbage();
    }

    public void CollectGarbage()
    {
        for(int i = 0; i < allFunctions.Count; i++)
        {
            if (cycle.loopingFunction.ContainsKey(allFunctions[i]))
                cycle.loopingFunction[allFunctions[i]].Cancel();
        }
        if (cycle != null)
        cycle.loopingFunction.Clear();
        allFunctions.Clear();

        if (scriptEnv != null)
        scriptEnv?.Dispose();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Laser"))
        {
            Laser laser = collision.transform.GetComponent<Laser>();
            cycle.Trigger("onLaserHit", laser.spanwer, laser.source);
        }
    }
}

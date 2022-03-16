using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using System.Threading.Tasks;
using System.Threading;
using System;

public class EntityCustomAction : MonoBehaviour
{
    internal LuaEnv luaEnv = new LuaEnv();
    private LuaTable scriptEnv;
    public MapObject mapObject;
    public LifeCycle cycle;

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

        List<string> allFunctions = new List<string>();
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

        luaEnv.DoString(logic, "chunk", scriptEnv);

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
}

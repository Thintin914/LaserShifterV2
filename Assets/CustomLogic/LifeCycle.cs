using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

[System.Serializable]
public class LifeCycle
{
    public string className;
    public delegate object CustomAction(params object[] parameters);
    public Dictionary<string, UpdateCycle> loopingFunction = new Dictionary<string, UpdateCycle>();

    public void Trigger(string functionName)
    {
        if (functions.ContainsKey(functionName))
        {
            functions[functionName]?.Invoke();
        }
    }

    public Dictionary<string, CustomAction> functions = new Dictionary<string, CustomAction>();

    public void Trigger(string functionName, params object[] parameters)
    {
        if (functions.ContainsKey(functionName))
        {
            functions[functionName]?.Invoke(parameters);
        }
    }

    public void Loop(string functionName, int loopCount, float waitTime, params object[] parameters)
    {
        if (functions.ContainsKey(functionName))
        {
            if (!loopingFunction.ContainsKey(functionName))
            {
                UpdateCycle updateCycle = new UpdateCycle(functionName, this, loopCount, waitTime, parameters);
                loopingFunction.Add(functionName, updateCycle);
                loopingFunction[functionName].Loop();
            }
        }
    }

    public UpdateCycle GetUpdateCycleByMethodName(string methodName)
    {
        if (loopingFunction.ContainsKey(methodName))
        {
            return loopingFunction[methodName];
        }
        return null;
    }

    public class UpdateCycle
    {
        string functionName;
        public LifeCycle lifeCycle;
        int loopCount = 0;
        float waitTime = 0;
        object[] parameters;
        private CancellationTokenSource cancelSource;

        public UpdateCycle(string functionName, LifeCycle lifeCycle, int loopCount, float waitTime, params object[] parameters)
        {
            UnityEngine.Debug.Log($"Start Looping Function {functionName}, {loopCount}, {waitTime}");
            this.functionName = functionName;
            this.lifeCycle = lifeCycle;
            this.loopCount = loopCount;
            this.waitTime = waitTime;
            this.parameters = parameters;
        }

        public void Cancel()
        {
            if (cancelSource != null && !cancelSource.IsCancellationRequested)
            {
                cancelSource.Cancel();
                cancelSource.Dispose();
            }
        }

        public async void Loop()
        {
            if (cancelSource != null && !cancelSource.IsCancellationRequested)
            {
                return;
            }
            cancelSource = new CancellationTokenSource();

            try
            {
                for (int i = 0; i != loopCount; i++)
                {
                    lifeCycle.functions[functionName]?.Invoke(parameters);

                    if (waitTime <= 0.05f)
                    {
                        await Task.Yield();
                    }
                    else
                    {
                        await Task.Delay((int)(waitTime * 1000), cancelSource.Token);
                    }
                    if (cancelSource.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (System.OperationCanceledException) when (cancelSource.IsCancellationRequested)
            {
                return;
            }
        }
    }
}

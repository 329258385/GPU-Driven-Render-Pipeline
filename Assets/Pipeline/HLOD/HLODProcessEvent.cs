using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public abstract class HLODProcessEvent : MonoBehaviour
{
    public static List<HLODProcessEvent> allEvents = new List<HLODProcessEvent>();
    private int localIndex;
    private void OnEnable()
    {
        localIndex = allEvents.Count;
        allEvents.Add(this);
        OnEnableFunction();
    }

    private void OnDisable()
    {
        allEvents[localIndex] = allEvents[allEvents.Count - 1];
        allEvents[localIndex].localIndex = localIndex;
        allEvents.RemoveAt(allEvents.Count - 1);
        localIndex = -1;
        OnDisableFunction();
    }
    public abstract void PrepareJob();
    public abstract void FinishJob();
    protected virtual void OnEnableFunction() { }
    protected virtual void OnDisableFunction() { }
}
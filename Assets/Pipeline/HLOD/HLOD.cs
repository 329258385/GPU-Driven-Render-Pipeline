using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;




public class HLOD : HLODProcessEvent
{
    public struct LoadCommand
    {
        public enum Operator
        {
            Disable, Enable, Combine, Separate
        }
        public Operator     ope;
        public int          parent;
        public int          leftDownSon;
        public int          leftUpSon;
        public int          rightDownSon;
        public int          rightUpSon;
    }

    public int                          allLevel;
    public List<SceneStreaming>         allGPURPScene;
    private NativeList<int>             levelOffset;
    public double3                      center;
    public double3                      extent;
    private NativeQueue<LoadCommand>    allLoadingCommand;
    private List<SceneStreaming>        childrenList = new List<SceneStreaming>(4);


    protected override void OnEnableFunction()
    {
        levelOffset             = new NativeList<int>(allLevel, Allocator.Persistent);
        levelOffset[0]          = 0;
        allLoadingCommand       = new NativeQueue<LoadCommand>(20, Allocator.Persistent);
        for (int i = 1; i < allLevel; ++i)
        {
            int v               = (int)(pow(2.0, i - 1));
            levelOffset[i]      = levelOffset[i - 1] + v * v;
        }
    }

    protected override void OnDisableFunction()
    {
        levelOffset.Dispose();
        allLoadingCommand.Dispose();
    }


    private IEnumerator Loader()
    {
        while (enabled)
        {
            LoadCommand cmd;
            if (allLoadingCommand.TryDequeue(out cmd))
            {
                switch (cmd.ope)
                {
                    case LoadCommand.Operator.Combine:
                        childrenList.Clear();
                        if (allGPURPScene[cmd.leftDownSon]) childrenList.Add(allGPURPScene[cmd.leftDownSon]);
                        if (allGPURPScene[cmd.leftUpSon]) childrenList.Add(allGPURPScene[cmd.leftUpSon]);
                        if (allGPURPScene[cmd.rightDownSon]) childrenList.Add(allGPURPScene[cmd.rightDownSon]);
                        if (allGPURPScene[cmd.rightUpSon]) childrenList.Add(allGPURPScene[cmd.rightUpSon]);
                        yield return SceneStreaming.Combine(allGPURPScene[cmd.parent], childrenList);
                        break;
                    case LoadCommand.Operator.Disable:
                        yield return allGPURPScene[cmd.parent].Delete();
                        break;
                    case LoadCommand.Operator.Enable:
                        yield return allGPURPScene[cmd.parent].Generate();
                        break;
                    case LoadCommand.Operator.Separate:
                        childrenList.Clear();
                        if (allGPURPScene[cmd.leftDownSon]) childrenList.Add(allGPURPScene[cmd.leftDownSon]);
                        if (allGPURPScene[cmd.leftUpSon]) childrenList.Add(allGPURPScene[cmd.leftUpSon]);
                        if (allGPURPScene[cmd.rightDownSon]) childrenList.Add(allGPURPScene[cmd.rightDownSon]);
                        if (allGPURPScene[cmd.rightUpSon]) childrenList.Add(allGPURPScene[cmd.rightUpSon]);
                        yield return SceneStreaming.Separate(allGPURPScene[cmd.parent], childrenList);
                        break;
                }
            }
            else yield return null;
        }
    }

    
    public override void PrepareJob()
    {

    }

    public override void FinishJob()
    {

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;



public unsafe struct HLODQuadTree
{
    public struct Data
    {
        public NativeList_Float                 lodDistance;
        public NativeList_Int                   positionOffsets;
        public NativeQueue<HLOD.LoadCommand>    commands;
    }

    private HLODQuadTree*   leftDown;
    private HLODQuadTree*   leftUp;
    private HLODQuadTree*   rightDown;
    private HLODQuadTree*   rightUp;
    private int2            localPos;
    private int             currentLevel;
    private double3         center;
    private bool            separate;
    private bool            isRendering;

    private static int GetIndex(int2 position, int level, ref Data data)
    {
        return data.positionOffsets[level] + position.x + position.y * (int)(0.1 + pow(2.0, level));
    }

    public void SetIsRendering(bool value, ref Data data)
    {
        if (isRendering == value) 
            return;


        isRendering = value;
        if (value)
        {
            data.commands.Add(new HLOD.LoadCommand
            {
                ope = HLOD.LoadCommand.Operator.Enable,
                parent = GetIndex(localPos, currentLevel, ref data)
            });
        }
        else
        {
            data.commands.Add(new HLOD.LoadCommand
            {
                ope = HLOD.LoadCommand.Operator.Disable,
                parent = GetIndex(localPos, currentLevel, ref data)
            });
        }
    }

    private double3         extent;
    private double          dist;
    public HLODQuadTree(int currentLevel, int2 localPos, ref Data data, double3 extent, double3 center)
    {
        separate            = false;
        isRendering         = false;
        dist                = 0;
        this.currentLevel   = currentLevel;
        this.center         = center;
        this.extent         = extent;
        leftDown            = null;
        leftUp              = null;
        rightDown           = null;
        rightUp             = null;
        this.localPos       = localPos;
    }

    private void GenerateChildren(ref Data data)
    {
        HLODQuadTree* ptr   = MUnsafeUtility.Malloc<HLODQuadTree>(sizeof(HLODQuadTree) * 4, Allocator.Persistent);
        leftDown            = ptr;
        leftUp              = ptr + 1;
        rightDown           = ptr + 2;
        rightUp             = ptr + 3;
        int subLevel        = currentLevel + 1;
        double3 subExtent   = extent * 0.5;
        *leftDown           = new HLODQuadTree(subLevel, localPos * 2, ref data, subExtent, center - double3(subExtent.x, 0, subExtent.z));
        *leftUp             = new HLODQuadTree(subLevel, localPos * 2 + int2(0, 1), ref data, subExtent, center + double3(-subExtent.x, 0, subExtent.z));
        *rightDown          = new HLODQuadTree(subLevel, localPos * 2 + int2(1, 0), ref data, subExtent, center + double3(subExtent.x, 0, -subExtent.z));
        *rightUp            = new HLODQuadTree(subLevel, localPos * 2 + 1, ref data, subExtent, center + double3(subExtent.x, 0, subExtent.z));
    }

    private void DisposeChildren()
    {
        if (leftDown != null)
        {
            leftDown->Dispose();
            leftUp->Dispose();
            rightDown->Dispose();
            rightUp->Dispose();
            UnsafeUtility.Free(leftDown, Allocator.Persistent);
            leftDown = null;
        }
    }

    public void Dispose()
    {
        DisposeChildren();
    }

    private void Separate(ref Data data)
    {
        if (currentLevel >= data.lodDistance.Length - 1)
        {
            SetIsRendering(true, ref data);
        }
        else if (leftDown == null)
        {
            GenerateChildren(ref data);
            leftDown->isRendering   = true;
            leftUp->isRendering     = true;
            rightDown->isRendering  = true;
            rightUp->isRendering    = true;
            data.commands.Add(new HLOD.LoadCommand
            {
                leftDownSon         = GetIndex(leftDown->localPos, leftDown->currentLevel, ref data),
                leftUpSon           = GetIndex(leftUp->localPos, leftUp->currentLevel, ref data),
                rightDownSon        = GetIndex(rightDown->localPos, rightDown->currentLevel, ref data),
                rightUpSon          = GetIndex(rightUp->localPos, rightUp->currentLevel, ref data),
                ope                 = HLOD.LoadCommand.Operator.Separate,
                parent              = GetIndex(localPos, currentLevel, ref data)
            });
        }
    }

    private void Combine(bool willRender, ref Data data)
    {
        if (leftDown != null)
        {
            leftDown->isRendering   = false;
            leftUp->isRendering     = false;
            rightDown->isRendering  = false;
            rightUp->isRendering    = false;
            data.commands.Add(new HLOD.LoadCommand
            {
                leftDownSon         = GetIndex(leftDown->localPos, leftDown->currentLevel, ref data),
                leftUpSon           = GetIndex(leftUp->localPos, leftUp->currentLevel, ref data),
                rightDownSon        = GetIndex(rightDown->localPos, rightDown->currentLevel, ref data),
                rightUpSon          = GetIndex(rightUp->localPos, rightUp->currentLevel, ref data),
                ope                 = HLOD.LoadCommand.Operator.Combine,
                parent              = GetIndex(localPos, currentLevel, ref data)
            });
            DisposeChildren();
        }
        SetIsRendering(willRender, ref data);
    }

    public void FirstUpdate(double3 cameraPos, ref Data data)
    {
        if (leftDown != null)
        {
            leftDown->FirstUpdate(cameraPos, ref data);
            leftUp->FirstUpdate(cameraPos, ref data);
            rightDown->FirstUpdate(cameraPos, ref data);
            rightUp->FirstUpdate(cameraPos, ref data);
        }
        double3 boxToCamera = cameraPos - center;
        dist                = MathLib.DistanceToCube(extent, boxToCamera);
        if (dist > data.lodDistance[currentLevel])
        {
            separate = false;
            Combine(currentLevel > 0, ref data);
        }
        else if (dist > data.lodDistance[currentLevel + 1])
        {
            separate = false;
            Combine(false, ref data);
        }
        else
        {
            separate = true;
        }  
    }

    public void SecondUpdate(ref Data data)
    {
        if (separate)
        {
            Separate(ref data);
            if (leftDown != null)
            {
                leftDown->SecondUpdate(ref data);
                leftUp->SecondUpdate(ref data);
                rightDown->SecondUpdate(ref data);
                rightUp->SecondUpdate(ref data);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ClusterBasedLighting
{
    public  static int      numClusterX = 16;
    public  static int      numClusterY = 16;
    public  static int      numClusterZ = 16;
    public  static int      maxNumLights = 1024;
    public  static int      maxNumLightsPerCluster = 128;
    private static int      SIZE_OF_LIGHT = 32;
    private static int      SIZE_OF_CLUSTERTBOX = 8 * 3 * 4;


    /// <summary>
    /// ComputeShader GPU上运行单元
    /// </summary>
    public ComputeShader    ComputeclusterShader;
    public ComputeShader    lightAssignShader;

    /// <summary>
    /// ComputeBuffer 是GPU 与CPU数据交换API
    /// </summary>
    public ComputeBuffer    clusterBuffer;              // 簇列表
    public ComputeBuffer    lightBuffer;                // 光源列表
    public ComputeBuffer    lightAssignBuffer;          // 光源分配结果表
    public ComputeBuffer    assignTable;                // 光源分配索引表

    struct ClusterBox
    {
        public Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
    };


    struct PointLight
    {
        public Vector3      color;
        public float        intensity;
        public Vector3      position;
        public float        radius;
    };


    struct LightIndex
    {
        public int          count;
        public int          start;
    };


    ComputeShader FindComputeShader( string shaderName )
    {
        ComputeShader[] css = Resources.FindObjectsOfTypeAll(typeof(ComputeShader)) as ComputeShader[];
        for( int i = 0; i < css.Length; i++ )
        {
            if (css[i].name == shaderName)
                return css[i];
        }

        return null;
    }


    public ClusterBasedLighting( )
    {
        int numClusters         = numClusterX * numClusterY * numClusterZ;
        clusterBuffer           = new ComputeBuffer( numClusters, SIZE_OF_CLUSTERTBOX);
        lightBuffer             = new ComputeBuffer( maxNumLights, SIZE_OF_LIGHT );
        lightAssignBuffer       = new ComputeBuffer( numClusters * maxNumLightsPerCluster, sizeof(uint));
        assignTable             = new ComputeBuffer( numClusters, SIZE_OF_LIGHT );
        ComputeclusterShader    = Resources.Load<ComputeShader>("Shaders/ClusterBasedLighting/ClusterBasedLighting");
        lightAssignShader       = Resources.Load<ComputeShader>("Shaders/ClusterBasedLighting/ClusterAssignLighting");
    }

    public void OnDestroy()
    {
        clusterBuffer.Release();
        lightBuffer.Release();
        lightAssignBuffer.Release();
        assignTable.Release();
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 根据相机参数生成 cluster
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void ClusterGenerate( Camera mainCamera )
    {
        // 设置参数
        Matrix4x4 viewMatrix        = mainCamera.worldToCameraMatrix;
        Matrix4x4 viewMatrixInv     = viewMatrix.inverse;
        Matrix4x4 projMatrix        = GL.GetGPUProjectionMatrix( mainCamera.projectionMatrix, false );
        Matrix4x4 vpMatrix          = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv       = vpMatrix.inverse;

        ComputeclusterShader.SetMatrix("_viewMatrix",       viewMatrix);
        ComputeclusterShader.SetMatrix("_viewMatrixInv",    viewMatrixInv);
        ComputeclusterShader.SetMatrix("_vpMatrix",         vpMatrix);
        ComputeclusterShader.SetMatrix("_vpMatrixInv",      vpMatrixInv);
        ComputeclusterShader.SetFloat("_near",              mainCamera.nearClipPlane);
        ComputeclusterShader.SetFloat("_far",               mainCamera.farClipPlane);
        ComputeclusterShader.SetFloat("_fovh",              mainCamera.fieldOfView);
        ComputeclusterShader.SetFloat("_numClusterX",       numClusterX);
        ComputeclusterShader.SetFloat("_numClusterY",       numClusterY);
        ComputeclusterShader.SetFloat("_numClusterZ",       numClusterZ);

        var Kernel                  = ComputeclusterShader.FindKernel("ClusterGenerate");
        ComputeclusterShader.SetBuffer(Kernel, "_clusterBuffer",  clusterBuffer);
        ComputeclusterShader.Dispatch(Kernel, numClusterZ, 1, 1);
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 更新光源信息
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void ClusterUpdateLightBuffer( Light[] lights )
    {
        PointLight[] plights            = new PointLight[maxNumLights];
        int cnt = 0;

        for( int i = 0; i < lights.Length; i++ )
        {
            if (lights[i].type != LightType.Point)
                continue;

            PointLight pl;
            pl.color                    = new Vector3( lights[i].color.r, lights[i].color.g, lights[i].color.b );
            pl.intensity                = lights[i].intensity;
            pl.position                 = lights[i].transform.position;
            pl.radius                   = lights[i].range;
            plights[cnt++]              = pl;
        }
        lightBuffer.SetData(plights);
        lightAssignShader.SetInt("_numLights", cnt);
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 更新光源信息
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void ClusterUpdateLightBuffer( VisibleLight[] lights )
    {
        PointLight[] plights            = new PointLight[maxNumLights];
        int cnt = 0;

        for( int i = 0; i < lights.Length; i++ )
        {
            var light                   = lights[i].light;
            if (light.type != LightType.Point)
                continue;

            PointLight pl;
            pl.color                    = new Vector3(light.color.r, light.color.g, light.color.b );
            pl.intensity                = light.intensity;
            pl.position                 = light.transform.position;
            pl.radius                   = light.range;
            plights[cnt++]              = pl;
        }
        lightBuffer.SetData(plights);
        lightAssignShader.SetInt("_numLights", cnt);
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 为每一个 cluster 分配光源
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void ClusterAssignLight()
    {
        lightAssignShader.SetInt("_maxNumLightsPerCluster", maxNumLightsPerCluster);
        lightAssignShader.SetInt("_numClusterX", numClusterX);
        lightAssignShader.SetInt("_numClusterY", numClusterY);
        lightAssignShader.SetInt("_numClusterZ", numClusterZ);

        var kenal           = lightAssignShader.FindKernel("ClusterAssignLight");
        lightAssignShader.SetBuffer(kenal, "_clusterBuffer",        clusterBuffer);
        lightAssignShader.SetBuffer(kenal, "_lightBuffer",          lightBuffer);
        lightAssignShader.SetBuffer(kenal, "_lightAssignBuffer",    lightAssignBuffer);
        lightAssignShader.SetBuffer(kenal, "_assignTable",          assignTable);
        lightAssignShader.Dispatch(kenal, numClusterZ, 1, 1);
    }
    

    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 向光照 shader 传递变量
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void SetShaderParameters()
    {
        Shader.SetGlobalFloat("_numClusterX",           numClusterX);
        Shader.SetGlobalFloat("_numClusterY",           numClusterY);
        Shader.SetGlobalFloat("_numClusterZ",           numClusterZ);

        Shader.SetGlobalBuffer("_lightBuffer",          lightBuffer);
        Shader.SetGlobalBuffer("_lightAssignBuffer",    lightAssignBuffer);
        Shader.SetGlobalBuffer("_assignTable",          assignTable);
    }


    void DrawBox(ClusterBox box, Color color)
    {
        Debug.DrawLine(box.p0, box.p1, color);
        Debug.DrawLine(box.p0, box.p2, color);
        Debug.DrawLine(box.p0, box.p4, color);

        Debug.DrawLine(box.p6, box.p2, color);
        Debug.DrawLine(box.p6, box.p7, color);
        Debug.DrawLine(box.p6, box.p4, color);

        Debug.DrawLine(box.p5, box.p1, color);
        Debug.DrawLine(box.p5, box.p7, color);
        Debug.DrawLine(box.p5, box.p4, color);

        Debug.DrawLine(box.p3, box.p1, color);
        Debug.DrawLine(box.p3, box.p2, color);
        Debug.DrawLine(box.p3, box.p7, color);
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 调试接口，显示clusters
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void DebugCluster()
    {
        ClusterBox[] boxes = new ClusterBox[numClusterX * numClusterY * numClusterZ];
        clusterBuffer.GetData(boxes, 0, 0, numClusterX * numClusterY * numClusterZ);

        foreach (var box in boxes)
            DrawBox(box, Color.gray);
    }


    /// ------------------------------------------------------------------------------------------------------
    /// <summary>
    // 调试接口，显示分配的灯光
    /// </summary>
    /// ------------------------------------------------------------------------------------------------------
    public void DebugLight()
    {
        /// CS 根据相机计算得到的所有cluster
        int numClusters         = numClusterX * numClusterY * numClusterZ;
        ClusterBox[] boxes      = new ClusterBox[numClusters];
        clusterBuffer.GetData(boxes, 0, 0, numClusters);

        /// 每个cluster 分配的灯光索引
        LightIndex[] indices    = new LightIndex[numClusters];
        assignTable.GetData(indices, 0, 0, numClusters);

        /// 每个cluster 分配的灯光
        uint[] assignBuf        = new uint[numClusters * maxNumLightsPerCluster];
        lightAssignBuffer.GetData(assignBuf, 0, 0, numClusters * maxNumLightsPerCluster);

        Color[] colors          = { Color.red, Color.green, Color.blue, Color.yellow };
        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i].count > 0)
            {
                uint firstLightId = assignBuf[indices[i].start];
                DrawBox(boxes[i], colors[firstLightId % 4]);
            }
        }
    }
}



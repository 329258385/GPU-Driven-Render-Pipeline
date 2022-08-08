using UnityEngine;





public class ClusterDebug : MonoBehaviour
{

    private ClusterBasedLighting    clusterLighting;
    private void Update()
    {
        if( clusterLighting == null )
        {
            clusterLighting     = new ClusterBasedLighting();
        }

        /// 更新所有光源
        var lights              = FindObjectsOfType(typeof(Light)) as Light[];
        clusterLighting.ClusterUpdateLightBuffer(lights);

        /// 划分相机
        Camera mainCamera       = Camera.main;
        clusterLighting.ClusterGenerate(mainCamera);

        // 分配光源
        clusterLighting.ClusterAssignLight();

        clusterLighting.DebugCluster();
        clusterLighting.DebugLight();
    }

    private void OnDestroy()
    {
        clusterLighting.OnDestroy();
    }
}

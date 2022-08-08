using System;
using System.Collections.Generic;
using UnityEngine;




public class ShadowCameraDebug : MonoBehaviour
{
    private CSM                 csm;
    void Update()
    {
        Camera mainCam          = Camera.main;

        // 获取光源信息
        Light light             = RenderSettings.sun;
        Vector3 lightDir        = light.transform.rotation * Vector3.forward;

        // 更新 shadowmap
        if (csm == null) csm    = new CSM();
        csm.Update(mainCam, lightDir);
        csm.DebugDraw();
    }
}

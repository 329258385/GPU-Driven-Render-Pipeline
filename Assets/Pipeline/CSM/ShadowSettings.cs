﻿using System;
using System.Collections.Generic;




[Serializable]
public class ShadowSettings
{
    public float            shadingPointNormalBias = 0.1f;
    public float            depthNormalBias = 0.005f;
    public float            pcssSearchRadius = 1.0f;
    public float            pcssFilterRadius = 7.0f;

    public void Set()
    {

    }
}

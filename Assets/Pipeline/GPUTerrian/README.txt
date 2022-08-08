////////////////
1.0 近处 GPU 地形
	
      1.0 把地形拆分成 4x4的小个子，考虑lod，得到一个全量的金字塔形的NodeList。

      2.0 将NodeList传入computeshader计算lod

      3.0 继续将处理后的NodeList传入computeshader作视椎体和Hiz剔除，得到可视Node的Id列表

      4.0 使用Node的Id列表DrawMeshInstancedIndirect，通过Id获取node的信息，还原地形的相关信息，渲染出来。
	  
	  5.0 视锥裁剪、Hiz 裁剪、背面裁剪、小三角裁剪。
	  
	  6.0 渲染地形
			
		   把可视的Node传入ps 调用 DrawMeshInstanceIndirect 就可以画地形了， 大致通过InstanceLd去除索引，
		   在全量表里通过索引获取地块信息，然后转换顶点。
		   
		   {
		   
				float4 rect 		= _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]].rect;
				float2 posXZ 		= rect.zw * 0.25 * v.position.xz + rect.xy; //we pre-transform to posWS in C# now
				VaryingsLean o 		= (VaryingsLean) 0;
				
				float3 positionWS 	= TransformObjectToWorld(posXZ.xyy);
				float height 		= UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(positionWS.xz, 0)));
				positionWS.y 		= height * terrainParam.y * 2;
				
				float3 normalWS 	= _TerrainNormalmapTexture.Load(int3(positionWS.xz, 0)).rgb * 2 - 1;
		   }
	   
	  7.0 接缝处理

2.0 远景四叉树
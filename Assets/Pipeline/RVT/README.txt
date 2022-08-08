/////////////////////////// 虚拟纹理技术文档说明 /////////////////////////////////////////////////
大概的过程就是FeedBack取出当前所需的各位置下的mipmap等级贴图，然后填入pagetable这张表，
pagetable取出信息加载对应等级贴图（RVT实时烘焙，SVT从预处理贴图提取），
然后烘焙到TileTexture上。加载完后会去更新lookup贴图，把当前显示的信息存入到里边供VT渲染地形使用。
在渲染地形时，通过uv去lookup贴图找出当前格子在TileTexture上的格子坐标，以及uv偏移，取出TileTexture上的Diffuse，法线，Mask等参与光照计算。


1.0 格子
我们会根据可视范围把范围内的所有地块划分成格子


2.0 FeedBack
feedback有点类似于遮挡剔除，会预先烘一个低分辨率的贴图信息，rgb分别表示格子坐标，mipmap等级。

// 把世界坐标映射到 PageTile 范围内 --- _LookupTex 和 计算mipmap
feed_v2f VTVertFeedBack( feed_attr v )
{
	feed_v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	
	#if defined(UNITY_INSTANCING_ENABLED)
	
	
	#endif
	VertexPositionInputs Attributes = GetVertexPositionInputs( v.vertex.xyz );
	o.pos 				= attributes.positionCS;
	float2 posWS		= Attributes.positionWS.xz;
	o.uv 				= ( posWS - _VTWorldRect.xy ) / _VTWorldRect.zw;
	return 0;
}

float4 VTFragFeedback( feed_v2f i ) : SV_Target
{
	float2 page 		= floor( i.uv * _VTFeedbackParam.x );
	
	float2 uv 			= i.uv * _VTFeedbackParam.y;
	float2 dx 			= ddx( uv );
	float2 dy 			= ddy( uv );
	
	int mip 			= clamp(int(0.5 * log2(max(dot(dx, dx), dot(dy, dy))) + 0.5 + _VTFeedbackParam.w), 0, _VTFeedbackParam.z);
	return float4( page / 255.0, mip / 255.0, 1 );
}


3.0 PageTable 页表是一个mipmap层级结构的表
public class TableNodeCell
{
        public RectInt Rect { get; set; }

        public PagePayload Payload { get; set; }

        public int MipLevel { get; }

        public TableNodeCell(int x, int y, int width, int height,int mip)
        {
            Rect = new RectInt(x, y, width, height);
            MipLevel = mip;
            Payload = new PagePayload();
        }
}


4.0 地形显示
half4 RVT( Varyings IN )
{
	float2 uv 			= ( IN.positionWS.xy - _VTWorldRect.xy ) / _VTWorldRect.zw;
	float2 uvInt		= uv - frac( uv * _VTPageParam.x ) * _VTPageParam.y;
	float4 page			= tex2D( _VTLookupTex, uvInt ) * 255;
	
	float2 inPageOffset	= frac( uv * exp2( _VTPageParam.z - page.b ));
	uv 					= (page.rg * (_VTTileParam.y + _VTTileParam.x * 2) + inPageOffset * _VTTileParam.y + _VTTileParam.x) / _VTTileParam.zw;
	
	half3 albedo 		= tex2D(_VTDiffuse, uv);
	half3 normalTS 		= UnpackNormalScale(tex2D(_VTNormal, uv), 1);
}
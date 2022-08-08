///----------------------------------------------------------------------------------------------
// unity引擎默认情况下,会很容易产生各种模糊. 
///  1.0 比如 贴图尺寸
///	 2.0 贴图格式压缩
///  3.0 画面解析度缩放
///  4.0 各项异性参数设置
///  5.0 TAA 抗锯齿并发症

/// ---------------------------------------------------------------------------------------------
///  mipmaps模糊是 最最常见的一种模糊，离相机几米远就开始显现，对于第一人称项目 几乎不可避免。
///  这是因为默认的 mipmaps算法是简单的box 均值。比较简易的做法有 改选kaiser 过滤算法，
///  或直接修改 mipmaps bias偏移量。但是 kaiser 算法可控度依然不足，而 mipmaps bias，并没修改mipmaps各级清晰度仅仅是 放缓mipmaps切换是条件。
///  同时 他影响了性能，更多区域采样精度比原来高了。特别是对于texturestreaming的功能，他更快的占用显存了。
///  6.0 Mipmaps 模糊优化



/// ---------------------------------------------------------------------------------------------
/// 针对skybox 优化模糊的方案
/// 使用两套 FOV
/// 核心算法是，先转换到视图空间，修改完投影再转到投影空间
/// 
/// o.vertex = mul( UINTY_MATRIX_MV, v.vertex );
/// float4x4 p = UNITY_MATRIX_P;
/// p.m22 = -ScaleFOV;
//// ----- _Screenparams.x == camera.pixelsw, _Screenparams.y == camera.pixelsheight
/// p.m11 = -p.m22 * _ScreenParams.y / _ScreenParams.x;


//float3 rotate( float3 v, float degrees )
//{
//	float angle = degrees * UNITY_PI / 180.0f
//	float sina, cosa;
//	sincos( angle, sina, cosa );
//	float2x2 m = float2x2( cosa, -sina, sina, cosa );
//	return float3( mul( m, v.xz ), v.y ).xyz;
//}
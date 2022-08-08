using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;






namespace VT
{
    /// <summary>
	/// 预渲染器类.
	/// 预渲染器使用特定的着色器渲染场景，获取当前场景用到的的虚拟贴图相关信息(页表/mipmap等级等)
	/// </summary>
    public class FeedbackRenderer : MonoBehaviour
    {
        /// <summary>
		/// 渲染目标缩放比例
		/// </summary>
        [SerializeField]
        private ScaleFactor         mScaleFactor = default;
        
        /// <summary>
        /// mipmap层级偏移
        /// </summary>
        [SerializeField]
        public  int                 mMipmapBias  = default;
        
        /// <summary>
        /// 预渲染摄像机
        /// </summary>
        public Camera               FeedbackCamera { get; set; }
        
        /// <summary>
		/// 获取预渲染的贴图
		/// </summary>
        public RenderTexture        TargetTexture { get; private set; }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            var mainCamera               = Camera.main;
            if (mainCamera == null)
                return;

            FeedbackCamera               = GetComponent<Camera>();
            if( FeedbackCamera == null )
            {
                FeedbackCamera           = gameObject.AddComponent<Camera>();
            }
            FeedbackCamera.enabled       = false;


            // 处理屏幕尺寸变换
            float scale                  = mScaleFactor.ToFloat();
            int   width                  = (int)(mainCamera.pixelWidth  * scale );
            int   height                 = (int)(mainCamera.pixelHeight * scale );
            if(TargetTexture == null || TargetTexture.width != width || TargetTexture.height != height )
            {
                TargetTexture            = new RenderTexture( width, height, 0 );
                TargetTexture.useMipMap  = false;
                TargetTexture.wrapMode   = TextureWrapMode.Clamp;
                TargetTexture.filterMode = FilterMode.Point;
                FeedbackCamera.targetTexture = TargetTexture;

                // 设置预渲染着色器参数
                // x: 页表大小(单位: 页)
                // y: 虚拟贴图大小(单位: 像素)
                // z: 最大mipmap等级
                var tileTexture          = GetComponent(typeof(TiledTexture)) as TiledTexture;
                var virtualTable         = GetComponent(typeof(PageTable)) as PageTable;
                Shader.SetGlobalVector( "_VTFeedbackParam",new Vector4(virtualTable.TableSize,
                                        virtualTable.TableSize * tileTexture.TileSize * scale,
                                        virtualTable.MaxMipLevel - 1,mMipmapBias));
            }

            // 渲染前先拷贝主摄像机的相关参数
            CopyCamera(mainCamera);
        }

        /// <summary>
		/// 拷贝摄像机参数
		/// </summary>
		private void CopyCamera(Camera camera)
        {
            if (camera == null)
                return;

            FeedbackCamera.fieldOfView      = camera.fieldOfView;
            FeedbackCamera.nearClipPlane    = camera.nearClipPlane;
            FeedbackCamera.farClipPlane     = camera.farClipPlane;
        }
    }
}

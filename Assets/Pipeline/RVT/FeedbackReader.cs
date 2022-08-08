using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;





namespace VT
{
    /// <summary>
    /// 预渲染贴图回读类.
    /// 负责将预渲染RT从GPU端回读到CPU端
    public class FeedbackReader : MonoBehaviour
    {
        /// <summary>
		/// 回读完成的事件回调.
		/// </summary>
		public event Action<Texture2D>          OnFeedbackReadComplete;

        /// <summary>
		/// 回读目标缩放比例
		/// </summary>
		[SerializeField]
        private ScaleFactor                     mReadbackScale = default;

		/// <summary>
		/// 缩放着色器.
		/// Feedback目标有特定的缩放逻辑，必须要通过自定义着色器来实现.
		/// 具体逻辑为:找到区域中mipmap等级最小的像素作为最终像素，其余像素抛弃.
		/// </summary>
		[SerializeField]
		private Shader							mDownScaleShader = default;
		private Material						mDownScaleMaterial;
		private int								mDownScaleMaterialPass;
		private RenderTexture					mDownScaleTexture;

		/// <summary>
		/// 调试着色器.
		/// 用于在编辑器中显示贴图mipmap等级
		/// </summary>
		[SerializeField]
		private Shader							mDebugShader = default;
		private Material						mDebugMaterial;
		public RenderTexture					DebugTexture { get; private set; }

		/// <summary>
		/// 处理中的回读请求
		/// </summary>
		private AsyncGPUReadbackRequest			mReadbackRequest;

		/// <summary>
		/// 回读到cpu端的贴图
		/// </summary>
		private Texture2D						mReadbackTexture;
		

		public bool CanRead
		{
			get
			{
				return mReadbackRequest.done || mReadbackRequest.hasError;
			}
		}

        private void Start()
        {
            if( mReadbackScale != ScaleFactor.One )
            {
				mDownScaleMaterial				= new Material(mDownScaleShader);
				switch( mReadbackScale )
                {
					case ScaleFactor.Half:
						mDownScaleMaterialPass = 0;
						break;
					case ScaleFactor.Quarter:
						mDownScaleMaterialPass = 1;
						break;
					case ScaleFactor.Eighth:
						mDownScaleMaterialPass = 2;
						break;
				}
            }
        }


		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// 发起回读请求
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public void NewRequest( RenderTexture texture, bool forceRequestAndWaitComplete = false )
        {
			if (!mReadbackRequest.done && mReadbackRequest.hasError)
				return;

			// 缩放后的尺寸
			var width		= (int)(texture.width  * mReadbackScale.ToFloat());
			var height		= (int)(texture.height * mReadbackScale.ToFloat());
			
			// 先进行缩放
			if (mReadbackScale != ScaleFactor.One)
			{
				if (mDownScaleTexture == null || mDownScaleTexture.width != width || mDownScaleTexture.height != height)
				{
					mDownScaleTexture = new RenderTexture(width, height, 0);
				}

				mDownScaleTexture.DiscardContents();
				Graphics.Blit(texture, mDownScaleTexture, mDownScaleMaterial, mDownScaleMaterialPass);
				texture = mDownScaleTexture;
			}

			// 贴图尺寸检测
			if (mReadbackTexture == null || mReadbackTexture.width != width || mReadbackTexture.height != height)
			{
				mReadbackTexture			= new Texture2D(width, height, TextureFormat.RGBA32, false);
				mReadbackTexture.filterMode = FilterMode.Point;
				mReadbackTexture.wrapMode	= TextureWrapMode.Clamp;
				InitDebugTexture(width, height);
			}

			// 发起异步回读请求
			mReadbackRequest = AsyncGPUReadback.Request(texture);
			if (forceRequestAndWaitComplete)
			{
				mReadbackRequest.WaitForCompletion();
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// 检测回读请求状态
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public void UpdateRequest()
        {
			if( mReadbackRequest.done && !mReadbackRequest.hasError )
            {
				mReadbackTexture.GetRawTextureData<Color32>().CopyFrom( mReadbackRequest.GetData<Color32>() );
				OnFeedbackReadComplete?.Invoke(mReadbackTexture);
				UpdateDebugTexture();
			}
        }

		[Conditional("ENABLE_DEBUG_TEXTURE")]
		private void InitDebugTexture(int width, int height)
		{
			#if UNITY_EDITOR
			DebugTexture			= new RenderTexture(width, height, 0);
			DebugTexture.filterMode = FilterMode.Point;
			DebugTexture.wrapMode	= TextureWrapMode.Clamp;
			#endif
		}

		[Conditional("ENABLE_DEBUG_TEXTURE")]
		protected void UpdateDebugTexture()
		{
			#if UNITY_EDITOR
			if (mReadbackTexture == null || mDebugShader == null)
				return;

			if (mDebugMaterial == null)
				mDebugMaterial = new Material(mDebugShader);

			mReadbackTexture.Apply(false);

			DebugTexture.DiscardContents();
			Graphics.Blit(mReadbackTexture, DebugTexture, mDebugMaterial);
			#endif
		}
	}
}

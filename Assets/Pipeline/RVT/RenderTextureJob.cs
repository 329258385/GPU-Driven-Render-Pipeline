using System;
using System.Collections.Generic;
using UnityEngine;





namespace VT
{
    /// <summary>
	/// 渲染器类.
	/// </summary>
    public class RenderTextureJob
    {
        /// <summary>
        /// 渲染完成的事件回调.
        /// </summary>
        public event Action<RenderTextureRequest>       StartRenderJob;

        /// <summary>
        /// 渲染取消的事件回调.
        /// </summary>
        public event Action<RenderTextureRequest>       CancelRenderJob;

        /// <summary>
        /// 一帧最多处理几个
        /// </summary>
        [SerializeField]
        private int                                     mLimit = 2;

        /// <summary>
		/// 等待处理的请求.
		/// </summary>
		private List<RenderTextureRequest>              mPendingRequests = new List<RenderTextureRequest>();

        public void Update()
        {
            if ( mPendingRequests.Count <= 0 )
                return;

            mPendingRequests.Sort((x, y) => { return x.MipLevel.CompareTo(y.MipLevel); });
            int count = mLimit;
            while( count > 0 && mPendingRequests.Count > 0 )
            {
                count--;
                var req = mPendingRequests[mPendingRequests.Count - 1];
                mPendingRequests.RemoveAt(mPendingRequests.Count - 1);

                // 开始渲染
                StartRenderJob?.Invoke(req);
            }
        }

        public RenderTextureRequest Request( int x, int y, int mip )
        {
            foreach( var r in mPendingRequests )
            {
                if (r.PageX == x && r.PageY == y && r.MipLevel == mip)
                    return null;
            }

            /// 加入待处理队列
            var request = new RenderTextureRequest(x, y, mip);
            mPendingRequests.Add(request);
            return request;
        }

        public void ClearJob()
        {
            foreach (var r in mPendingRequests)
            {
                CancelRenderJob?.Invoke(r);
            }

            mPendingRequests.Clear();
        }
    }
}

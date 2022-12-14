namespace VT
{
    /// <summary>
    /// 渲染请求类.
    /// </summary>
    public class RenderTextureRequest
    {
        /// <summary>
		/// 页表X坐标
		/// </summary>
        public int          PageX { get; }

        /// <summary>
        /// 页表Y坐标
        /// </summary>
        public int          PageY { get; }

        /// <summary>
        /// mipmap等级
        /// </summary>
        public int          MipLevel { get; }

        public RenderTextureRequest(int x, int y, int mip)
        {
            PageX           = x;
            PageY           = y;
            MipLevel        = mip;
        }
    }
}

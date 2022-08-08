using System;
using UnityEngine;






namespace VT
{
    public class TiledTexture : MonoBehaviour
    {
        /// <summary>
        /// Tile缓存池.
        /// </summary>
        private LruCache                       mTilePool = new LruCache();

        /// <summary>
        /// Tile更新完成的事件回调
        /// </summary>
        public event Action<Vector2Int>        OnTileUpdateComplete;

        /// <summary>
		/// 画Tile的事件.
		/// </summary>
        public event Action<RectInt, RenderTextureRequest>  OnDrawTexture;

        /// <summary>
        /// 区域尺寸.
        /// </summary>
        [SerializeField]
        private Vector2Int                      mRegionSize = default;

        /// <summary>
		/// 单个Tile的尺寸.
		/// </summary>
		[SerializeField]
        private int                             mTileSize = 256;

        /// <summary>
		/// 填充尺寸
		/// </summary>
		[SerializeField]
        private int                            mPaddingSize = 4;

        /// <summary>
		/// Tile Target
		/// </summary>
        public RenderTexture[]                VTRTs { get; private set; }

        /// <summary>
		/// 区域尺寸.
		/// 区域尺寸表示横竖两个方向上Tile的数量.
		/// </summary>
		public Vector2Int                       RegionSize { get { return mRegionSize; } }

        /// <summary>
		/// 单个Tile的尺寸.
		/// Tile是宽高相等的正方形.
		/// </summary>
		public int                              TileSize { get { return mTileSize; } }

        /// <summary>
		/// 填充尺寸
		/// 每个Tile上下左右四个方向都要进行填充，用来支持硬件纹理过滤.
		/// 所以Tile有效尺寸为(TileSize - PaddingSize * 2)
		/// </summary>
		public int                              PaddingSize { get { return mPaddingSize; } }

        public int                              TileSizeWithPadding { get { return TileSize + PaddingSize * 2; } }

        public void InitTileTexture()
        {
            mTilePool.Init( RegionSize.x * RegionSize.y );
            VTRTs                       = new RenderTexture[2];
            VTRTs[0]                    = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
            VTRTs[0].useMipMap          = false;
            VTRTs[0].wrapMode           = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_VTDiffuse", VTRTs[0]);

            VTRTs[1]                    = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
            VTRTs[1].useMipMap          = false;
            VTRTs[1].wrapMode           = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_VTNormal", VTRTs[1]);

            // 设置Shader参数
            // x: padding偏移量 4
            // y: tile有效区域的尺寸 15
            // zw: 1/区域尺寸 = 2960
            Shader.SetGlobalVector( "_VTTileParam",new Vector4((float)PaddingSize,(float)TileSize, RegionSize.x * TileSizeWithPadding,RegionSize.y * TileSizeWithPadding));
        }

        public void Reset()
        {
            mTilePool.Init(RegionSize.x * RegionSize.y);

            VTRTs                       = new RenderTexture[2];
            VTRTs[0]                    = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
            VTRTs[0].useMipMap          = false;
            VTRTs[0].wrapMode           = TextureWrapMode.Clamp;
            VTRTs[0].filterMode         = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_VTDiffuse", VTRTs[0]);

            VTRTs[1]                    = new RenderTexture(RegionSize.x * TileSizeWithPadding, RegionSize.y * TileSizeWithPadding, 0);
            VTRTs[1].useMipMap          = false;
            VTRTs[1].wrapMode           = TextureWrapMode.Clamp;
            VTRTs[1].filterMode         = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_VTNormal", VTRTs[1]);
        }

        /// <summary>
        /// Tile ID 到位置的转换
        /// </summary>
        private Vector2Int ID2Pos( int id )
        {
            return new Vector2Int(id % RegionSize.x, id / RegionSize.x);
        }

        /// <summary>
        /// 位置 到 Tile ID 的转换
        /// </summary>
        private int Pos2ID( Vector2Int tile )
        {
            return tile.y * RegionSize.x + tile.x;
        }

        public Vector2Int RequestTile()
        {
            return ID2Pos(mTilePool.First);
        }


        /// -------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 激活超级纹理上某一个快 tile 
        /// </summary>
        /// -------------------------------------------------------------------------------------------------------------------
        public bool SetActive( Vector2Int tile )
        {
            bool success    = mTilePool.SetActive(Pos2ID(tile));
            return success;
        }

        public void UpdateTile( Vector2Int tile, RenderTextureRequest request )
        {
            if (!SetActive(tile))
                return;

            OnDrawTexture?.Invoke(new RectInt(tile.x * TileSizeWithPadding, tile.y * TileSizeWithPadding, TileSizeWithPadding, TileSizeWithPadding),request);
            OnTileUpdateComplete?.Invoke(tile);
        }
    }
}

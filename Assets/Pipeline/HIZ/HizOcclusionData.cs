using UnityEngine;





public class HizOcclusionData : IPerCameraData
{
    public RenderTexture        historyDepth { get; private set; }
    public int                  targetWidth { get; private set; }
    public int                  mip { get; private set; }


    private int GetWidthFromScreen(int screenWidth)
    {
        int targetWidth;
        if (screenWidth >= 2048)
        {
            targetWidth = 1024;
            mip = 9;
        }
        else if (screenWidth >= 1024)
        {
            targetWidth = 512;
            mip = 8;
        }
        else
        {
            targetWidth = 256;
            mip = 7;
        }
        return targetWidth;
    }

    public HizOcclusionData(int screenWidth)
    {
        targetWidth                     = GetWidthFromScreen(screenWidth);
        historyDepth                    = new RenderTexture(targetWidth, targetWidth / 2, 0, RenderTextureFormat.RHalf, 9);
        historyDepth.useMipMap          = true;
        historyDepth.autoGenerateMips   = false;
        historyDepth.enableRandomWrite  = true;
        historyDepth.wrapMode           = TextureWrapMode.Clamp;
        historyDepth.filterMode         = FilterMode.Point;
        historyDepth.Create();
    }

    public void UpdateWidth(int screenWidth)
    {
        int tar                         = GetWidthFromScreen(screenWidth);
        if (tar != targetWidth)
        {
            targetWidth                 = tar;
            historyDepth.Release();
            historyDepth.width          = tar;
            historyDepth.height         = tar / 2;
            historyDepth.Create();
        }
    }
    public override void DisposeProperty()
    {
        Object.DestroyImmediate(historyDepth);
    }

    public struct GetHizOcclusionData : IGetCameraData
    {
        public int                      screenWidth;
        public IPerCameraData Run()
        {
            return new HizOcclusionData(screenWidth);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;




public class BOCTree : MonoBehaviour
{
    public struct int3
    {
        public int x, y, z;
        public override string ToString()
        {
            return x + "," + y + "," + z;
        }
    }


    class Node
    {
        public int          level;
        public int          x;
        public int          y;
        public int          z;
        public int          size;
        public int          flag;
        public Node         parent;
        public Node[]       children;
        public int          Index;

        static int3[]       OffsetNodeList =
        {
            new int3() { x = 0, y = 0, z = 0}, new int3() { x = 1, y = 0, z = 0 }, new int3() { x = 0, y = 1, z = 0 }, new int3() { x = 1, y = 1, z = 0},
            new int3() { x = 0, y = 0, z = 1}, new int3() { x = 1, y = 0, z = 1 }, new int3() { x = 0, y = 1, z = 1 }, new int3() { x = 1, y = 1, z = 1},
        };

        internal void insert( int vx, int vy, int vz )
        {
            if( size <= 1 ) { flag = 1; return; }
            if( children == null )
            {
                children    = new Node[8];
                for( int i = 0; i < 8; i++ )
                {
                    children[i] = new Node() { x = x + OffsetNodeList[i].x * size / 2, y = y + OffsetNodeList[i].y * size / 2, z = z + OffsetNodeList[i].z * size / 2, size = size / 2, level = level + 1, parent = this };
                }
            }

            int offset = 0;
            if (vx > x + size / 2) offset++;
            if (vy > y + size / 2) offset += 2;
            if (vz > z + size / 2) offset += 4;
            children[offset].insert(vx, vy, vz);
        }

        public bool find( int fx, int fy, int fz )
        {
            if (children == null) return flag == 1;

            int offset = 0;
            if (fx > x + size / 2) offset++;
            if (fy > y + size / 2) offset += 2;
            if (fz > z + size / 2) offset += 4;
            return children[offset].find(fx, fy, fz);
        }

        public List<Node> getAllNodes()
        {
            List<Node> nodes    = new List<Node>();
            nodes.Add(this);

            int startIndex      = 0;
            int endIndex        = nodes.Count;
            while( startIndex != endIndex )
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (nodes[i].children != null)
                    {
                        nodes.AddRange(nodes[i].children);
                    }
                }
                startIndex  = endIndex;
                endIndex    = nodes.Count;
            }
            return nodes;
        }

        private void calCount(ref int count0, ref int count1)
        {
            if (flag == 0)
                count0++;
            else
                count1++;
            if (children != null)
            {
                foreach (var item in children)
                {
                    item.calCount(ref count0, ref count1);
                }
            }
        }

        public void ClipNode( bool recursion = true )
        {
            if (children == null) return;
            int dataCount = 0;
            foreach (var item in children)
            {
                if (item.flag == 1)
                {
                    dataCount++;
                }
            }

            if (dataCount > 7)
            {
                if (flag != 1)
                {
                    flag = 1;
                    children = null;
                    if (parent != null) parent.ClipNode(false);
                }
            }
            else if (recursion)
            {
                foreach (var item in children)
                {
                    item.ClipNode();
                }
            }
        }

        public void draw(float drawScale, Vector3 wposOffset)
        {

            if (children == null)
            {
                Gizmos.color = flag == 1 ? Color.red : Color.green;
                if (flag == 1)
                    Gizmos.DrawWireCube(new Vector3(x, y, z) * drawScale + wposOffset + new Vector3(size, size, size) * 0.5f * drawScale,
                        new Vector3(size, size, size) * drawScale);
            }
            
            if (children != null)
            {
                foreach (var item in children)
                {
                    item.draw(drawScale, wposOffset);
                }
            }
        }

        public void printCount()
        {
            int count0 = 0;
            int count1 = 0;
            calCount(ref count0, ref count1);
            print(count0);
            print(count1);
        }
    }


    public Texture2D     tex;
    public Light         light;
    public bool          clipMode;
    public bool          debugCellMode = false;
    private List<int3>   shadows;
    public OCTShadowGenerate shadowGenerate;
    public Vector3       wposOffset;
    public int           unitsPerMeter = 1;
    private Node         root;



    [ContextMenu("initShadowData")]
    private void InitShadowData()
    {
        shadows         = shadowGenerate.GenerateShadow(1000 * unitsPerMeter, 20 * unitsPerMeter, 1000 * unitsPerMeter, wposOffset, unitsPerMeter);
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;

        root        = new Node();
        root.x      = 0;
        root.z      = 0;
        root.size   = 1024 * unitsPerMeter;
        foreach (var item in shadows)
        {
            root.insert(item.x, item.y, item.z);
        }
        root.ClipNode();
        root.printCount();

        var nodes   = root.getAllNodes();
        int width   = Mathf.CeilToInt(Mathf.Sqrt(nodes.Count));
        tex         = new Texture2D( width, width, TextureFormat.RGBAFloat, false, true );
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var colors   = tex.GetPixels();
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Index = i;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            colors[i].r = nodes[i].x;
            colors[i].g = nodes[i].y;
            colors[i].b = nodes[i].z;
            colors[i].a = nodes[i].children != null ? nodes[i].children[0].Index : 0;
            colors[i].a = colors[i].a * 10 + nodes[i].flag;
        }
        tex.SetPixels(colors);
        tex.Apply();
        Shader.SetGlobalTexture("_OTreeTex", tex);
        Shader.SetGlobalInt("_OTreeWidth", width);
        Shader.SetGlobalInt("_unitsPerMeter", unitsPerMeter);
        Shader.SetGlobalVector("_wposOffset", wposOffset);
    }


    void OnDrawGizmos()
    {

        if (debugCellMode == false) return;
        float drawScale = 1f / unitsPerMeter;
        root.draw(drawScale, wposOffset);
    }
}


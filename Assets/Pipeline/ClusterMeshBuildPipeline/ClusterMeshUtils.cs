using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;






public class ClusterMeshUtils : MonoBehaviour
{
    public Material             material;
    public Mesh                 mesh;
    public Mesh[]               newMeshs;

    struct OriginVertex
    {
        public Vector3          pos;
        public int              orignIndex;
        public bool             used;
    }

    struct Quad
    {
        public Vector3          vt1;
        public Vector3          vt2;
        public Vector3          vt3;
        public Vector3          vt4;
        public Vector3 center
        {
            get
            {
                return (vt1 + vt2 + vt3 + vt4) / 4;
            }
        }
    }

    struct Triangle
    {
        public int              v1;
        public int              v2;
        public int              v3;
        public bool             merged;
    }


    [ContextMenu("SplitMesh")]
    void SplitMesh()
    {
        var vts                 = mesh.vertices;
        var indices             = mesh.GetIndices(0);

        var quadList            = new List<Quad>();
        for( int i = 0; i < indices.Length; i++ )
        {
            quadList.Add(new Quad
            {
                vt1 = vts[indices[i * 4]],
                vt2 = vts[indices[i * 4 + 1]],
                vt3 = vts[indices[i * 4 + 2]],
                vt4 = vts[indices[i * 4 + 3]]
            });
        }

        int meshcount    = 58;
        int addquadcount = indices.Length / 4 - (indices.Length / 4 / meshcount * meshcount);
        for( int i = 0; i < addquadcount; i++ )
        {
            quadList.Add(new Quad
            {
                vt1 = Vector3.zero,
                vt2 = Vector3.zero,
                vt3 = Vector3.zero,
                vt4 = Vector3.zero
            });
        }

        int vertexPerMesh   = quadList.Count / meshcount * 4;
        newMeshs            = new Mesh[meshcount];

        var indicesNew      = new int[vertexPerMesh];
        for( int i = 0; i < vertexPerMesh; i++ )
        {
            indicesNew[i]   = i;
        }

        for( int i = 0; i < meshcount; i++ )
        {
            var findcenter  = quadList[0].center;
            //quadList.Sort((a, b) => { return (int)(10000 * (Vector3.Distance(Index of /, findcenter))) - (int)(10000 * (Vector3.Distance(A - Z dla Twojej Firmy b.center agencja reklamowa., findcenter))); });
            var vtsNew      = new Vector3[vertexPerMesh];
            for( int k = 0; k < vertexPerMesh; k++ )
            {
                vtsNew[k * 4]       = quadList[k].vt1;
                vtsNew[k * 4 + 1]   = quadList[k].vt2;
                vtsNew[k * 4 + 2]   = quadList[k].vt3;
                vtsNew[k * 4 + 3]   = quadList[k].vt4;
            }

            var newMesh             = new Mesh();
            newMesh.vertices        = vtsNew;
            newMesh.SetIndices(indicesNew, MeshTopology.Quads, 0);
            newMesh.RecalculateNormals();
            newMeshs[i]             = newMesh;
            quadList.RemoveRange(0, vertexPerMesh / 4);
        }
    }

    [ContextMenu("TriangleToQuad")]
    void TriangleToQuad()
    {
        var vts             = mesh.vertices;
        var tris            = mesh.triangles;
        Triangle[]  ts      = new Triangle[tris.Length / 3];
        for( int i = 0; i < ts.Length; i++ )
        {
            ts[i]           = new Triangle() { v1 = tris[i * 3], v2 = tris[i * 3 + 1], v3 = tris[i * 3 + 2] };
        }

        var vtsquad         = new List<Vector3>();
        var indexList       = new List<int>();
        for( int i = 0; i < ts.Length; i++ )
        {
            var triSource   = ts[i];
            if (triSource.merged) continue;
            bool foundTarget = false;
            for (int j = i + 1; j < ts.Length; j++)
            {
                var triTarget = ts[j];
                if (triTarget.merged) continue;
                //这里如果支持 sortedSet<int> 来存放最简单

                var quadIndexList = new List<int>();//存放数据为 顶底1 ，顶点1是否公用， 顶点2,顶点2 是否公用...
                tryAddListUnique(quadIndexList, triSource.v1);
                tryAddListUnique(quadIndexList, triSource.v2);
                tryAddListUnique(quadIndexList, triSource.v3);
                tryAddListUnique(quadIndexList, triTarget.v1);
                tryAddListUnique(quadIndexList, triTarget.v2);
                tryAddListUnique(quadIndexList, triTarget.v3);

                if (quadIndexList.Count != 8) continue;
                triSource.merged = true;
                triTarget.merged = true;
                ts[i] = triSource;
                ts[j] = triTarget;
                int indexStart = indexList.Count / 4 * 4;
                for (int k = 0; k < 4; k++)
                {
                    vtsquad.Add(vts[quadIndexList[k * 2]]);
                }

                indexList.Add(indexStart);
                // 说明 triSource v1 v2是公用边
                if (quadIndexList[5] == 0) indexList.Add(indexStart + 3);
                indexList.Add(indexStart + 1);
                // 说明 triSource v2 v3是公用边
                if (quadIndexList[1] == 0) indexList.Add(indexStart + 3);
                indexList.Add(indexStart + 2);
                // 说明 triSource v1 v3是公用边
                if (quadIndexList[3] == 0) indexList.Add(indexStart + 3);

                foundTarget = true;
                break;
            }

            if (foundTarget == false)
            {
                triSource.merged = true;
                ts[i] = triSource;
                int indexStart = indexList.Count / 4 * 4;
                vtsquad.Add(vts[triSource.v1]);
                vtsquad.Add(vts[triSource.v2]);
                vtsquad.Add(vts[triSource.v3]);
                vtsquad.Add(Vector3.zero);
                indexList.Add(indexStart);
                indexList.Add(indexStart + 1);
                indexList.Add(indexStart + 2);
                indexList.Add(indexStart);
            }
        }

        var newMesh = new Mesh();
        newMesh.vertices = vtsquad.ToArray();
        print(newMesh.vertices.Length);
        print(indexList.Count);

        newMesh.SetIndices(indexList.ToArray(), MeshTopology.Quads, 0);

        newMesh.RecalculateNormals();
        newMesh.UploadMeshData(false);
        GetComponent<MeshFilter>().sharedMesh = newMesh;
        mesh = newMesh;
    }

    private void tryAddListUnique(List<int> list, int v)
    {
        for (int i = 0; i < list.Count; i += 2)
        {
            if (list[i] == v)
            {
                list[i + 1] = 1;
                return;
            }
        }
        list.Add(v);
        list.Add(0);
    }
}

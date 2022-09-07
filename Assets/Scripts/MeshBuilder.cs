using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;

namespace Sketch {

// Modeler array -> Single combined mesh
static class MeshBuilder
{
    public static void Build(Span<Modeler> modelers, Mesh mesh)
    {
        // Total vertex / index count
        var (vcount, icount) = (0, 0);
        foreach (var m in modelers)
        {
            vcount += m.VertexCount;
            icount += m.IndexCount;
        }

        // Native arrays for vertex / color / index
        using var vbuf = Util.NewNativeArray<float3>(vcount);
        using var cbuf = Util.NewNativeArray<float4>(vcount);
        using var ibuf = Util.NewNativeArray<uint>(icount);

        // Span<T> representation
        var vspan = vbuf.GetSpan();
        var cspan = cbuf.GetSpan();
        var ispan = ibuf.GetSpan();

        // Mesh building and combining
        var (voffs, ioffs) = (0, 0);
        foreach (var m in modelers)
        {
            var (vc, ic) = (m.VertexCount, m.IndexCount);

            var vslice = vspan.Slice(voffs, vc);
            var cslice = cspan.Slice(voffs, vc);
            var islice = ispan.Slice(ioffs, ic);

            m.BuildGeometry(vslice, cslice, islice, (uint)voffs);

            voffs += vc;
            ioffs += ic;
        }

        // Mesh (re)initialization
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vbuf);
        mesh.SetUVs(0, cbuf);
        mesh.SetIndices(ibuf, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

} // namespace Sketch

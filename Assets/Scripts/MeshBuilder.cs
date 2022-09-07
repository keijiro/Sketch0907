using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System;

namespace Sketch {

static class MeshBuilder
{
    unsafe public static void Build(Span<Modeler> modelers, Mesh mesh)
    {
        var (vcount, icount) = (0, 0);
        foreach (var m in modelers)
        {
            vcount += m.VertexCount;
            icount += m.IndexCount;
        }

        using var vbuf = Util.NewNativeArray<float3>(vcount);
        using var cbuf = Util.NewNativeArray<float4>(vcount);
        using var ibuf = Util.NewNativeArray<uint>(icount);

        var vspan = vbuf.GetSpan();
        var cspan = cbuf.GetSpan();
        var ispan = ibuf.GetSpan();

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

        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vbuf);
        mesh.SetUVs(0, cbuf);
        mesh.SetIndices(ibuf, MeshTopology.Triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

} // namespace Sketch

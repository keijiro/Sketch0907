using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Mathematics;
using System;

namespace Sketch {

// Modeler array -> Single combined mesh
[BurstCompile]
static class MeshBuilder
{
    // Public method
    public static void Build(Span<Modeler> modelers, Mesh mesh)
    {
        // Total vertex / index count
        var (vcount, icount) = (0, 0);
        foreach (var m in modelers)
        {
            vcount += m.VertexCount;
            icount += m.IndexCount;
        }

        // Native arrays for vertex / color / index data
        using var vbuf = Util.NewNativeArray<float3>(vcount);
        using var cbuf = Util.NewNativeArray<float4>(vcount);
        using var ibuf = Util.NewNativeArray<uint>(icount);

        // Data construction
        BuildDataBursted(modelers.GetUntyped(),
                         vbuf.GetUntypedSpan(),
                         cbuf.GetUntypedSpan(),
                         ibuf.GetUntypedSpan());

        // Mesh object construction
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vbuf);
        mesh.SetUVs(0, cbuf);
        mesh.SetIndices(ibuf, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // Burst accelerated vertex data construction
    [BurstCompile]
    static void BuildDataBursted(in UntypedSpan u_modelers,
                                 in UntypedSpan u_vspan,
                                 in UntypedSpan u_cspan,
                                 in UntypedSpan u_ispan)
    {
        var modelers = u_modelers.GetTyped<Modeler>();

        // Warning: Not sure but this "1" extension is needed.
        var vspan = u_vspan.GetTyped<float3>(1);
        var cspan = u_cspan.GetTyped<float4>(1);
        var ispan = u_ispan.GetTyped<uint>(1);

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
    }
}

} // namespace Sketch

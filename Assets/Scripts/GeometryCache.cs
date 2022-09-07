using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Sketch {

// Mesh read cache with NativeArrray
sealed class GeometryCache : System.IDisposable
{
    public NativeArray<float3> Vertices;
    public NativeArray<uint> Indices;

    public GeometryCache(Mesh mesh)
    {
        var v = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent);
        var i = new NativeArray<int>(mesh.triangles, Allocator.Persistent);
        Vertices = v.Reinterpret<float3>();
        Indices = i.Reinterpret<uint>();
    }

    public void Dispose()
    {
        if (Vertices.IsCreated) Vertices.Dispose();
        if (Indices.IsCreated) Indices.Dispose();
    }
}

// Weak reference to GeometryCache contents
readonly struct GeometryCacheRef
{
    public readonly NativeSlice<float3> Vertices;
    public readonly NativeSlice<uint> Indices;

    public GeometryCacheRef(GeometryCache geo)
    {
        Vertices = new NativeSlice<float3>(geo.Vertices);
        Indices = new NativeSlice<uint>(geo.Indices);
    }

    // Implicit conversion operator
    public static implicit operator GeometryCacheRef(GeometryCache cache)
      => new GeometryCacheRef(cache);
}

} // namespace Sketch

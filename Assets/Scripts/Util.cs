using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using Object = UnityEngine.Object;

namespace Sketch {

static class Util
{
    public unsafe static Span<T>
      GetSpan<T>(this NativeArray<T> array) where T : unmanaged
        => new Span<T>(NativeArrayUnsafeUtility.GetUnsafePtr(array),
                       array.Length);

    public static NativeArray<T>
      NewNativeArray<T>(int length) where T : unmanaged
        => new NativeArray<T>(length, Allocator.Persistent,
                              NativeArrayOptions.UninitializedMemory);

    public static void DestroyObject(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying)
            Object.Destroy(o);
        else
            Object.DestroyImmediate(o);
    }
}

} // namespace Sketch

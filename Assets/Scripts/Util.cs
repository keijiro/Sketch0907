using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using Object = UnityEngine.Object;

namespace Sketch {

// Used to pass Span<T> to Burst functions
unsafe readonly struct UntypedSpan
{
    public readonly void* Pointer;
    public readonly int Length;

    public Span<T> GetTyped<T>(int ext = 0)
      => new Span<T>(Pointer, Length + ext);

    public UntypedSpan(void* ptr, int len)
    {
        Pointer = ptr;
        Length = len;
    }
}

static class UnsafeExtensions
{
    public unsafe static void*
      GetUnsafePtr<T>(this NativeArray<T> array) where T : unmanaged
        => NativeArrayUnsafeUtility.GetUnsafePtr(array);

    public unsafe static Span<T>
      GetSpan<T>(this NativeArray<T> array) where T : unmanaged
        => new Span<T>(GetUnsafePtr(array), array.Length);

    public unsafe static UntypedSpan
      GetUntyped<T>(this Span<T> span) where T : unmanaged
    {
        fixed (T* p = span) return new UntypedSpan(p, span.Length);
    }

    public unsafe static UntypedSpan
      GetUntypedSpan<T>(this NativeArray<T> array) where T : unmanaged
        => new UntypedSpan(GetUnsafePtr(array), array.Length);
}

static class Util
{
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

using UnityEngine;
using Unity.Mathematics;
using System;
using Random = Unity.Mathematics.Random;

namespace Sketch {

// Configuration struct for SceneBuilder
[Serializable]
struct SceneConfig
{
    #region Editable attributes

    public int PoleCount;
    public float BaseRange;
    public float NodeStride;

    [Space]
    public float Lifetime;
    public float Rotation;
    public float Twist;
    public float PoleDelay;
    public float NodeDelay;
    [Range(0, 0.5f)]
    public float FadeRange;

    [Space, Range(0, 1)]
    public float EmissionRate;
    public Color EmissionColor1;
    public Color EmissionColor2;
    public float EmissionIntensity;

    [Space]
    public uint Seed;

    #endregion

    #region Default configuration

    public static SceneConfig Default()
      => new SceneConfig()
        { PoleCount = 80,
          BaseRange = 1.25f,
          NodeStride = 0.1f,
          Lifetime = 5,
          Rotation = 2,
          Twist = 2,
          PoleDelay = 2,
          NodeDelay = 2,
          FadeRange = 0.3f,
          EmissionRate = 0.4f,
          EmissionColor1 = Color.red,
          EmissionColor2 = Color.blue,
          EmissionIntensity = 600,
          Seed = 10 };

    #endregion

    #region Helper methods

    public Color ChooseEmission(float random)
      => (random < EmissionRate ?
           (random < EmissionRate / 2 ? EmissionColor1 : EmissionColor2)
             : Color.clear) * EmissionIntensity;

    #endregion
}

// SceneBuilder: Model-level scene building
static class SceneBuilder
{
    // Public entry point
    public static Span<Modeler> Build
      (in SceneConfig cfg,
       (GeometryCacheRef board, GeometryCacheRef pole) shapes,
       float time,
       Span<Modeler> buffer)
    {
        // PRNG for the root level
        var root_hash = new XXHash(cfg.Seed);

        // Buffer output count
        var count = 0;

        for (var i = 0; i < cfg.PoleCount; i++)
        {
            // Per-pole PRNG
            var hash = new XXHash(root_hash.UInt((uint)i));

            // Pole instance
            var slice = AddPole(cfg, shapes, hash, time, buffer.Slice(count));
            count += slice.Length;
        }

        // Used area in the model buffer
        return buffer.Slice(0, count);
    }

    // Builder method: Pole
    static Span<Modeler> AddPole
      (in SceneConfig cfg,
       (GeometryCacheRef board, GeometryCacheRef pole) shapes,
       XXHash hash,
       float time,
       Span<Modeler> buffer)
    {
        // PRNG seed
        var seed = 0u;

        // Buffer output count
        var count = 0;

        // Time parameter
        var t = time - hash.Float(0, cfg.PoleDelay, seed++);

        // Pole origin (base position)
        var origin = math.float3(hash.InCircle(seed++) * cfg.BaseRange, 0);

        // Base angle
        var angle = hash.Bool(seed++) ? 0 : math.PI / 4;
        angle += t * cfg.Rotation / cfg.Lifetime;

        // Emission color (zero for no-emission)
        var emission = cfg.ChooseEmission(hash.Float(seed++));

        // Probability decay coefficient
        var decay = hash.Float(0.1f, 0.96f, seed++);

        // Z displacement
        var z = 0.0f;

        // Pole expansion loop
        for (var prob = 1.0f; prob > 0.2f;)
        {
            // Per-node time parameter
            var t_n = (t - z * cfg.NodeDelay) / cfg.Lifetime;

            // Per-node PRNG
            var hash_n = new XXHash(hash.UInt(seed++));

            // Is visible?
            if (0 < t_n && t_n < 1)
            {
                // Fade in / out parameter
                var f_i = math.smoothstep(0, cfg.FadeRange, t_n);
                var f_o = math.smoothstep(1 - cfg.FadeRange, 1, t_n);

                // Per-node scale / angle
                var scale_n = f_i - f_o;
                var angle_n = angle + (f_i + f_o) * cfg.Twist;

                // Node instance
                var slice = AddNode(cfg, shapes,
                                    hash_n, prob,
                                    origin, z, angle_n, scale_n,
                                    buffer.Slice(count));
                count += slice.Length;
            }

            // Z stride
            z += cfg.NodeStride;

            // Probability decay
            prob *= hash.Float(decay, 1, seed++);
        }

        // Emitter model
        if (emission.a > 0)
        {
            z += cfg.NodeStride * hash.Float(1, 3, seed++);
            var f_i = math.saturate(t / (cfg.NodeDelay * z + cfg.FadeRange * cfg.Lifetime));
            var f_o = math.saturate((t - (cfg.Lifetime - cfg.FadeRange * cfg.Lifetime)) / (cfg.FadeRange * cfg.Lifetime + cfg.NodeDelay * z));

            //var f_i = math.smoothstep(0, cfg.NodeDelay * z + cfg.FadeRange * cfg.Lifetime, t);
            //var f_o = math.smoothstep(cfg.Lifetime - cfg.FadeRange * cfg.Lifetime, cfg.Lifetime + cfg.NodeDelay * z, t);

            if (f_i > 0 && f_o < 1)
            buffer[count++] = new Modeler(position: origin + math.float3(0, 0, z / 2 * (f_i + f_o)),
                                          rotation: 0,
                                          scale: math.float3(1, 1, z / cfg.NodeStride * (f_i - f_o)),
                                          color: emission,
                                          shape: shapes.pole);
        }

        return buffer.Slice(0, count);
    }

    // Bbuilder method: Node
    static Span<Modeler> AddNode
      (in SceneConfig cfg,
       (GeometryCacheRef board, GeometryCacheRef pole) shapes,
       XXHash hash, float prob,
       float3 origin, float z, float angle, float scale,
       Span<Modeler> buffer)
    {
        var count = 0;

        for (var i = 0u; i < 4u; i++)
        {
            // Rotation matrix
            var rot = float2x2.Rotate(angle);

            // Board: Displacement / position
            var d1 = math.float2(cfg.NodeStride / 2, 0);
            var p1 = origin + math.float3(math.mul(rot, d1), z);

            // Pole: Displacement / position
            var d2 = (float2)(cfg.NodeStride / 2);
            var p2 = origin + math.float3(math.mul(rot, d2), z);

            // Board model
            if (hash.Float(i) < prob)
                buffer[count++] = new Modeler(position: p1,
                                              rotation: angle,
                                              scale: scale,
                                              color: Color.black,
                                              shape: shapes.board);

            // Pole model
            buffer[count++] = new Modeler(position: p2,
                                          rotation: angle,
                                          scale: scale,
                                          color: Color.black,
                                          shape: shapes.pole);

            // Rotation
            angle += math.PI / 2;
        }

        return buffer.Slice(0, count);
    }
}

} // namespace Sketch

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
    public static Span<Modeler> Build
      (in SceneConfig cfg,
       (GeometryCacheRef board, GeometryCacheRef pole) shapes,
       float time,
       Span<Modeler> buffer)
    {
        // PRNG
        var (hash, seed) = (new XXHash(cfg.Seed), 0u);

        // Buffer output count
        var count = 0;

        // Pole population
        for (var i = 0; i < cfg.PoleCount; i++)
        {
            // Time parameter
            var t = time - hash.Float(0, cfg.PoleDelay, seed++);

            // Base position
            var pos = math.float3(hash.InCircle(seed++) * cfg.BaseRange, 0);

            // Base angle
            var angle = hash.Bool(seed++) ? 0 : math.PI / 4;
            angle += t * cfg.Rotation / cfg.Lifetime;

            // Emission color (zero for no-emission)
            var emission = cfg.ChooseEmission(hash.Float(seed++));

            // Probability decay coefficient
            var decay = hash.Float(0.1f, 0.96f, seed++);

            // Pole extension loop
            var min_prob = 0.2f * math.pow(decay, hash.Float(2, 7, seed++));
            for (var prob = 1.0f; prob > min_prob;)
            {
                // Per-node time parameter
                var t2 = (t - pos.z * cfg.NodeDelay) / cfg.Lifetime;

                // Fade in / out parameter
                var f_i = math.smoothstep(0, cfg.FadeRange, t2);
                var f_o = math.smoothstep(1 - cfg.FadeRange, 1, t2);

                // Per-node scale / angle
                var scale = f_i - f_o;
                var angle2 = angle + (f_i + f_o) * cfg.Twist;

                for (var k = 0; k < 4; k++)
                {
                    // Rotation matrix
                    var rot = float2x2.Rotate(angle2);

                    // Board: Displacement / position
                    var d1 = math.float2(cfg.NodeStride / 2, 0);
                    var p1 = pos + math.float3(math.mul(rot, d1), 0);

                    // Pole: Displacement / position
                    var d2 = (float2)(cfg.NodeStride / 2);
                    var p2 = pos + math.float3(math.mul(rot, d2), 0);

                    // Board model
                    if (math.max(0.2f, hash.Float(seed++)) < prob)
                        buffer[count++] = new Modeler(position: p1,
                                                      rotation: angle2,
                                                      scale: scale,
                                                      color: Color.black,
                                                      shape: shapes.board);

                    // Pole model
                    if (0.2f < prob)
                        buffer[count++] = new Modeler(position: p2,
                                                      rotation: angle2,
                                                      scale: scale,
                                                      color: Color.black,
                                                      shape: shapes.pole);

                    // Emitter model
                    if (emission.a > 0)
                        buffer[count++] = new Modeler(position: pos,
                                                      rotation: 0,
                                                      scale: scale,
                                                      color: emission,
                                                      shape: shapes.pole);

                    // Rotation
                    angle2 += math.PI / 2;
                }

                // Stride
                pos.z += cfg.NodeStride;

                // Probability decay
                prob *= hash.Float(decay, 1, seed++);
            }
        }

        return buffer.Slice(0, count);
    }
}

} // namespace Sketch

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
    public float EmissionRate;
    public Color EmissionColor1;
    public Color EmissionColor2;
    public float EmissionIntensity;
    public uint Seed;

    #endregion

    #region Default configuration

    public static SceneConfig Default()
      => new SceneConfig()
        { PoleCount = 80,
          BaseRange = 1.25f,
          NodeStride = 0.1f,
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
            // Position / angle
            var pos = math.float3(hash.InCircle(seed++) * cfg.BaseRange, 0);
            var angle = hash.Bool(seed++) ? 0 : math.PI / 4;

            // Emission color (zero for no-emission)
            var emission = cfg.ChooseEmission(hash.Float(seed++));

            // Probability decay coefficient
            var decay = hash.Float(0.1f, 0.96f, seed++);

            //
            var t = math.frac(time / hash.Float(1, 3, seed++));
            var s = math.min(math.smoothstep(0, 0.5f, t), 1 - math.smoothstep(0.5f, 1, t));

            // Pole extension loop
            for (var prob = 1.0f; prob > 0.2f;)
            {
                for (var k = 0; k < 4; k++)
                {
                    // Rotation matrix
                    var rot = float2x2.Rotate(angle);

                    // Board: Displacement / position
                    var d1 = math.float2(cfg.NodeStride / 2, 0);
                    var p1 = pos + math.float3(math.mul(rot, d1), 0);

                    // Pole: Displacement / position
                    var d2 = (float2)(cfg.NodeStride / 2);
                    var p2 = pos + math.float3(math.mul(rot, d2), 0);

                    // Board model
                    if (hash.Float(seed++) < prob)
                        buffer[count++] = new Modeler(position: p1,
                                                      rotation: angle,
                                                      scale: s,
                                                      color: Color.black,
                                                      shape: shapes.board);

                    // Pole model
                    buffer[count++] = new Modeler(position: p2,
                                                  rotation: angle,
                                                  scale: s,
                                                  color: Color.black,
                                                  shape: shapes.pole);

                    // Emitter model
                    if (emission.a > 0)
                        buffer[count++] = new Modeler(position: pos,
                                                      rotation: 0,
                                                      scale: s,
                                                      color: emission,
                                                      shape: shapes.pole);

                    // Rotation
                    angle += math.PI / 2;
                }

                // Stride
                pos.z += cfg.NodeStride;

                // Probability decay
                prob *= hash.Float(decay, 1, seed++);
            }

            // Emitter extension
            var ext = emission.a > 0 ? hash.Int(10, seed++) : 0;

            for (var j = 0; j < ext; j++)
            {
                // Emitter model
                buffer[count++] = new Modeler(position: pos,
                                              rotation: 0,
                                              scale: s,
                                              color: emission,
                                              shape: shapes.pole);

                // Stride
                pos.z += cfg.NodeStride;
            }
        }

        return buffer.Slice(0, count);
    }
}

} // namespace Sketch

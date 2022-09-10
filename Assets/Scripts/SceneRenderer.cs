using UnityEngine;
using System;

namespace Sketch {

[ExecuteInEditMode]
sealed class SceneRenderer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] SceneConfig _config = SceneConfig.Default();
    [SerializeField] Mesh _boardMesh = null;
    [SerializeField] Mesh _poleMesh = null;
    [SerializeField] uint _modelCapacity = 10000;
    [SerializeField] float _stillTime = -1;
    [SerializeField] float _timeOffset = 0;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Temporary mesh object
        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        // State reset
        _prevTime = -1;
    }

    void Update()
      => ConstructMesh();

    void OnDestroy()
      => Util.DestroyObject(_mesh);

    void OnValidate()
      => ConstructMesh(true);

    #endregion

    #region Private members

    Mesh _mesh;
    Modeler[] _sceneBuffer;
    float _prevTime;

    void ConstructMesh(bool forceUpdate = false)
    {
        if (_mesh == null) return;

        // Time control
        var time = _stillTime < 0 ? Time.time + _timeOffset : _stillTime;
        if (!forceUpdate && _prevTime >= 0 && _prevTime == time) return;
        _prevTime = time;

        // Scene buffer (modeler array) allocation
        if ((_sceneBuffer?.Length ?? 0) != _modelCapacity)
            _sceneBuffer = new Modeler[_modelCapacity];

        // Geometry cache (should live until mesh building)
        using var board = new GeometryCache(_boardMesh);
        using var pole = new GeometryCache(_poleMesh);

        // Model-level scene building
        var scene = SceneBuilder.Build
          (_config, (board, pole), time, _sceneBuffer);

        // Mesh building from the model array
        MeshBuilder.Build(scene, _mesh);
    }

    #endregion
}

} // namespace Sketch

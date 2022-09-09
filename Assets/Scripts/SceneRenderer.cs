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

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Temporary mesh object
        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    void Update()
      => ConstructMesh();

    void OnDestroy()
      => Util.DestroyObject(_mesh);

    #endregion

    #region Private members

    Mesh _mesh;
    Modeler[] _sceneBuffer;

    void ConstructMesh()
    {
        if (_mesh == null) return;

        // Scene buffer (modeler array) allocation
        if ((_sceneBuffer?.Length ?? 0) != _modelCapacity)
            _sceneBuffer = new Modeler[_modelCapacity];

        // Geometry cache (should live until mesh building)
        using var board = new GeometryCache(_boardMesh);
        using var pole = new GeometryCache(_poleMesh);

        // Model-level scene building
        var scene = SceneBuilder.Build
          (_config, (board, pole), Time.time, _sceneBuffer);

        // Mesh building from the model array
        MeshBuilder.Build(scene, _mesh);
    }

    #endregion
}

} // namespace Sketch

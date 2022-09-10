using UnityEngine;
using Unity.Mathematics;

namespace Sketch {

sealed class FocusController : MonoBehaviour
{
    [SerializeField] float _fadeDuration = 1.5f;
    [SerializeField] float _endTime = 8;

    Camera _camera;
    float _targetFocus;

    void Start()
    {
        _camera = GetComponent<Camera>();
        _targetFocus = _camera.focusDistance;
    }

    void Update()
    {
        var t1 = math.saturate(Time.time / _fadeDuration);
        var t2 = math.saturate((_endTime - Time.time) / _fadeDuration);
        var x = 1 - math.pow(1 - math.min(t1, t2), 2);
        _camera.focusDistance = math.lerp(0.1f, _targetFocus, x);
    }
}

} // namespace Sketch

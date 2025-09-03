using UnityEngine;

public class Headbob : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool _enable = true;

    [Header("Normal Speed Settings")]
    [SerializeField, Range(0, 0.1f)] private float _NormalAmplitude = 0.015f;
    [SerializeField, Range(0, 30)] private float _NormalFrequency = 10.0f;

    [Header("Increased Speed Settings")]
    [SerializeField, Range(0, 0.1f)] private float _IncreasedAmplitude = 0.03f;
    [SerializeField, Range(0, 30)] private float _IncreasedFrequency = 15.0f;

    [SerializeField] private Transform _cameraTarget;

    private float _toggleSpeed = 1.0f;
    private float _Amplitude;
    private float _frequency;
    private CharacterController _controller;
    private Vector3 _originalPos;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            Debug.LogError("CharacterController is missing on " + gameObject.name);
        }
        if (_cameraTarget != null)
        {
            _originalPos = _cameraTarget.localPosition;
        }
        else
        {
            Debug.LogError("Camera Target (Eyes) is not assigned!");
        }

        // Set default values to normal
        _Amplitude = _NormalAmplitude;
        _frequency = _NormalFrequency;
    }

    private void Update()
    {
        if (!_enable || _cameraTarget == null) return;

        CheckMotion();
    }

    private Vector3 FootStepMotion()
    {
        Vector3 motion = Vector3.zero;
        motion.y += Mathf.Sin(Time.time * _frequency) * _Amplitude;
        motion.x += Mathf.Cos(Time.time * _frequency / 2) * _Amplitude * 2;
        return motion;
    }

    private void CheckMotion()
    {
        float speed = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;

        if (speed == 0)
        {
            return; // No headbob when standing still
        }

        if (speed > 3)
        {
            _Amplitude = _IncreasedAmplitude;
            _frequency = _IncreasedFrequency;
        }
        else
        {
            _Amplitude = _NormalAmplitude;
            _frequency = _NormalFrequency;
        }

        if (speed > _toggleSpeed)
        {
            PlayMotion(FootStepMotion());
        }
        else
        {
            ResetPosition();
        }
    }

    private void PlayMotion(Vector3 motion)
    {
        _cameraTarget.localPosition = _originalPos + motion;
    }

    private void ResetPosition()
    {
        _cameraTarget.localPosition = _originalPos;
    }
}

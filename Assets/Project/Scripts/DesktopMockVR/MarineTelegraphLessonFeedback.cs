using UnityEngine;

public sealed class MarineTelegraphLessonFeedback : MonoBehaviour
{
    [SerializeField] private DesktopLeverInteractable telegraph;
    [SerializeField] private Renderer asternLamp;
    [SerializeField] private Renderer stopLamp;
    [SerializeField] private Renderer aheadLamp;
    [SerializeField] private Transform indicatorNeedle;
    [SerializeField] private float stateAngleThreshold = 14f;
    [SerializeField] private float needleMinAngle = 42f;
    [SerializeField] private float needleMaxAngle = -42f;
    [SerializeField] private Color inactiveColor = new Color(0.045f, 0.055f, 0.05f, 1f);
    [SerializeField] private Color asternColor = new Color(0.85f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color stopColor = new Color(1f, 0.68f, 0.12f, 1f);
    [SerializeField] private Color aheadColor = new Color(0.08f, 0.75f, 0.25f, 1f);

    private MaterialPropertyBlock _propertyBlock;

    private void Update()
    {
        if (telegraph == null)
        {
            return;
        }

        float angle = telegraph.CurrentAngle;
        int state = GetTelegraphState(angle);
        SetLamp(asternLamp, state < 0 ? asternColor : inactiveColor);
        SetLamp(stopLamp, state == 0 ? stopColor : inactiveColor);
        SetLamp(aheadLamp, state > 0 ? aheadColor : inactiveColor);
        UpdateNeedle(telegraph.NormalizedValue);
    }

    private int GetTelegraphState(float angle)
    {
        if (angle <= -stateAngleThreshold)
        {
            return -1;
        }

        if (angle >= stateAngleThreshold)
        {
            return 1;
        }

        return 0;
    }

    private void SetLamp(Renderer lamp, Color color)
    {
        if (lamp == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        lamp.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        _propertyBlock.SetColor("_EmissionColor", color * 1.6f);
        lamp.SetPropertyBlock(_propertyBlock);
    }

    private void UpdateNeedle(float normalizedValue)
    {
        if (indicatorNeedle == null)
        {
            return;
        }

        float angle = Mathf.Lerp(needleMinAngle, needleMaxAngle, Mathf.Clamp01(normalizedValue));
        indicatorNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}

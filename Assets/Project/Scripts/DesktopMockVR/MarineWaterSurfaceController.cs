using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class MarineWaterSurfaceController : MonoBehaviour
{
    private static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
    private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
    private static readonly int FoamColorId = Shader.PropertyToID("_FoamColor");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int TimeMultiplierId = Shader.PropertyToID("_TimeMultiplier");
    private static readonly int AmplitudeMultiplierId = Shader.PropertyToID("_AmplitudeMultiplier");
    private static readonly int ChaosId = Shader.PropertyToID("_Chaos");
    private static readonly int WaveScaleId = Shader.PropertyToID("_WaveScale");
    private static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
    private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
    private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
    private static readonly int FoamAmountId = Shader.PropertyToID("_FoamAmount");
    private static readonly int FoamCutoffId = Shader.PropertyToID("_FoamCutoff");
    private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");
    private static readonly int SpecularPowerId = Shader.PropertyToID("_SpecularPower");
    private static readonly int SpecularIntensityId = Shader.PropertyToID("_SpecularIntensity");
    private static readonly int DistantWindVectorId = Shader.PropertyToID("_DistantWindVector");
    private static readonly int LocalWindVectorId = Shader.PropertyToID("_LocalWindVector");
    private static readonly int CurrentVectorId = Shader.PropertyToID("_CurrentVector");
    private static readonly int SunDirectionId = Shader.PropertyToID("_SunDirection");

    [Header("Simulation")]
    [SerializeField] private float timeMultiplier = 1f;
    [SerializeField, Range(1, 3)] private int simulationBands = 3;
    [SerializeField, Range(0f, 35f)] private float distantWindSpeed = 8f;
    [SerializeField, Range(0f, 360f)] private float distantWindOrientation = 24f;
    [SerializeField, Range(0f, 35f)] private float localWindSpeed = 5f;
    [SerializeField, Range(0f, 360f)] private float localWindOrientation = 58f;
    [SerializeField, Range(0f, 1f)] private float chaos = 0.28f;
    [SerializeField, Range(0f, 2.5f)] private float amplitudeMultiplier = 0.72f;
    [SerializeField, Range(0f, 3f)] private float currentSpeed = 0.28f;
    [SerializeField, Range(0f, 360f)] private float currentOrientation = 0f;

    [Header("Foam")]
    [SerializeField] private bool foamEnabled = true;
    [SerializeField, Range(0f, 1f)] private float foamIntensity = 0.3f;
    [SerializeField, Range(0f, 1f)] private float foamCutoff = 0.78f;
    [SerializeField, Range(0f, 1f)] private float windSpeedFoamDimmer = 0.65f;

    [Header("Appearance")]
    [SerializeField] private Color shallowColor = new Color(0.18f, 0.66f, 0.77f, 1f);
    [SerializeField] private Color deepColor = new Color(0.012f, 0.13f, 0.21f, 1f);
    [SerializeField] private Color foamColor = new Color(0.82f, 0.94f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float alpha = 0.92f;
    [SerializeField, Range(0.05f, 0.5f)] private float waveScale = 0.17f;
    [SerializeField, Range(0.03f, 0.6f)] private float baseWaveHeight = 0.2f;
    [SerializeField, Range(0.1f, 2f)] private float baseWaveSpeed = 0.46f;
    [SerializeField, Range(0.5f, 8f)] private float fresnelPower = 3.1f;
    [SerializeField, Range(8f, 256f)] private float specularPower = 94f;
    [SerializeField, Range(0f, 3f)] private float specularIntensity = 0.82f;
    [SerializeField] private Vector3 sunDirection = new Vector3(-0.35f, 0.72f, 0.22f);

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        ApplyToRenderer();
    }

    private void OnValidate()
    {
        timeMultiplier = Mathf.Max(0f, timeMultiplier);
        sunDirection = sunDirection.sqrMagnitude > 0.0001f ? sunDirection.normalized : new Vector3(-0.35f, 0.72f, 0.22f);
        ApplyToRenderer();
    }

    [ContextMenu("Apply Calm Sea")]
    private void ApplyCalmSea()
    {
        distantWindSpeed = 7f;
        localWindSpeed = 4f;
        chaos = 0.2f;
        amplitudeMultiplier = 0.58f;
        currentSpeed = 0.2f;
        foamIntensity = 0.12f;
        foamCutoff = 0.82f;
        ApplyToRenderer();
    }

    [ContextMenu("Apply Moderate Sea")]
    private void ApplyModerateSea()
    {
        distantWindSpeed = 14f;
        localWindSpeed = 9f;
        chaos = 0.42f;
        amplitudeMultiplier = 1.05f;
        currentSpeed = 0.45f;
        foamIntensity = 0.34f;
        foamCutoff = 0.72f;
        ApplyToRenderer();
    }

    [ContextMenu("Apply Storm Sea")]
    private void ApplyStormSea()
    {
        distantWindSpeed = 27f;
        localWindSpeed = 22f;
        chaos = 0.82f;
        amplitudeMultiplier = 1.75f;
        currentSpeed = 1.2f;
        foamIntensity = 0.82f;
        foamCutoff = 0.58f;
        ApplyToRenderer();
    }

    public void ApplyToRenderer()
    {
        _renderer = _renderer != null ? _renderer : GetComponent<MeshRenderer>();
        if (_renderer == null)
        {
            return;
        }

        _propertyBlock ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_propertyBlock);

        float distantWind01 = Mathf.InverseLerp(0f, 30f, distantWindSpeed);
        float localWind01 = Mathf.InverseLerp(0f, 24f, localWindSpeed);
        float bandMultiplier = simulationBands switch
        {
            1 => 0.55f,
            2 => 0.82f,
            _ => 1f
        };
        float windWaveHeight = baseWaveHeight * Mathf.Lerp(0.65f, 1.75f, distantWind01) * bandMultiplier;
        float windWaveSpeed = baseWaveSpeed * Mathf.Lerp(0.75f, 1.85f, distantWind01);
        float rippleStrength = Mathf.Lerp(0.08f, 0.72f, localWind01) * bandMultiplier;
        float foamWind = Mathf.Lerp(1f - windSpeedFoamDimmer, 1f, distantWind01);
        float foamAmount = foamEnabled ? foamIntensity * foamWind : 0f;

        Vector2 distantWind = DirectionFromDegrees(distantWindOrientation);
        Vector2 localWind = DirectionFromDegrees(localWindOrientation);
        Vector2 current = DirectionFromDegrees(currentOrientation);

        _propertyBlock.SetColor(ShallowColorId, shallowColor);
        _propertyBlock.SetColor(DeepColorId, deepColor);
        _propertyBlock.SetColor(FoamColorId, foamColor);
        _propertyBlock.SetFloat(AlphaId, alpha);
        _propertyBlock.SetFloat(TimeMultiplierId, timeMultiplier);
        _propertyBlock.SetFloat(AmplitudeMultiplierId, amplitudeMultiplier);
        _propertyBlock.SetFloat(ChaosId, chaos);
        _propertyBlock.SetFloat(WaveScaleId, waveScale);
        _propertyBlock.SetFloat(WaveHeightId, windWaveHeight);
        _propertyBlock.SetFloat(WaveSpeedId, windWaveSpeed);
        _propertyBlock.SetFloat(RippleStrengthId, rippleStrength);
        _propertyBlock.SetFloat(FoamAmountId, foamAmount);
        _propertyBlock.SetFloat(FoamCutoffId, foamCutoff);
        _propertyBlock.SetFloat(FresnelPowerId, fresnelPower);
        _propertyBlock.SetFloat(SpecularPowerId, specularPower);
        _propertyBlock.SetFloat(SpecularIntensityId, specularIntensity);
        _propertyBlock.SetVector(DistantWindVectorId, new Vector4(distantWind.x, distantWind.y, 0f, 0f));
        _propertyBlock.SetVector(LocalWindVectorId, new Vector4(localWind.x, localWind.y, 0f, 0f));
        _propertyBlock.SetVector(CurrentVectorId, new Vector4(current.x, current.y, currentSpeed, 0f));
        _propertyBlock.SetVector(SunDirectionId, new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));

        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private static Vector2 DirectionFromDegrees(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
    }
}

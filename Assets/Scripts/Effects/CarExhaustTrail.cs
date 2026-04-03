using UnityEngine;

/// <summary>
/// 차량 뒤쪽 배기가스/불꽃 파티클 트레일.
/// 속도에 비례해 파티클 양과 색이 변함.
/// </summary>
public class CarExhaustTrail : MonoBehaviour
{
    private ParticleSystem exhaustPS;
    private ParticleSystem sparkPS;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        CreateExhaustParticle();
        CreateSparkParticle();
    }

    private Material CreateMaterial(string texturePath)
    {
        string[] shaderNames = {
            "Particles/Standard Unlit",
            "Mobile/Particles/Alpha Blended",
            "Sprites/Default"
        };

        Shader shader = null;
        foreach (var name in shaderNames)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        var mat = new Material(shader);
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        var tex = Resources.Load<Texture2D>(texturePath);
        if (tex != null)
            mat.mainTexture = tex;

        return mat;
    }

    private void CreateExhaustParticle()
    {
        var obj = new GameObject("ExhaustTrail");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0, -0.7f, 0);

        exhaustPS = obj.AddComponent<ParticleSystem>();

        var main = exhaustPS.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.25f, 0.27f, 0.35f, 0.85f),
            new Color(0.4f, 0.4f, 0.45f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.gravityModifier = -0.05f;

        var emission = exhaustPS.emission;
        emission.rateOverTime = 0f;

        var shape = exhaustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var sizeOverLifetime = exhaustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1, 0f)));

        var colorOverLifetime = exhaustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.4f, 0.42f, 0.5f), 0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.35f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.25f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;
        renderer.material = CreateMaterial("Sprites/Cars/particle/Particle-CarExhaustTrail");
    }

    private void CreateSparkParticle()
    {
        var obj = new GameObject("SparkTrail");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0, -0.7f, 0);

        sparkPS = obj.AddComponent<ParticleSystem>();

        var main = sparkPS.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startColor = new Color(1f, 0.5f, 0.2f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.gravityModifier = 0.5f;

        var emission = sparkPS.emission;
        emission.rateOverTime = 0f;

        var shape = sparkPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 6;
        renderer.material = CreateMaterial("Sprites/Cars/particle/Particle-CarExhaustTrail");
    }

    private void Update()
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;

        if (exhaustPS != null)
        {
            var emission = exhaustPS.emission;
            if (speed > 0.5f)
                emission.rateOverTime = Mathf.Lerp(5f, 30f, speed / 10f);
            else
                emission.rateOverTime = 2f;
        }

        if (sparkPS != null)
        {
            var emission = sparkPS.emission;
            emission.rateOverTime = speed > 4f ? Mathf.Lerp(0f, 15f, (speed - 4f) / 6f) : 0f;
        }
    }
}

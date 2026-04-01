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

    private void CreateExhaustParticle()
    {
        var obj = new GameObject("ExhaustTrail");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0, -0.7f, 0);

        exhaustPS = obj.AddComponent<ParticleSystem>();

        var main = exhaustPS.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.2f, 0.6f),
            new Color(0.5f, 0.5f, 0.5f, 0.3f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.gravityModifier = -0.05f;

        var emission = exhaustPS.emission;
        emission.rateOverTime = 0f; // Update에서 속도 기반 제어

        var shape = exhaustPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        // 크기 감소
        var sizeOverLifetime = exhaustPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1, 0f)));

        // 색 페이드아웃
        var colorOverLifetime = exhaustPS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0.4f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }

    private void CreateSparkParticle()
    {
        var obj = new GameObject("SparkTrail");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(0, -0.7f, 0);

        sparkPS = obj.AddComponent<ParticleSystem>();

        var main = sparkPS.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(1f, 0.8f, 0.3f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;
        main.gravityModifier = 0.5f;

        var emission = sparkPS.emission;
        emission.rateOverTime = 0f;

        var shape = sparkPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 6;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }

    private void Update()
    {
        if (rb == null) return;

        float speed = rb.velocity.magnitude;

        // 배기가스: 속도 비례
        if (exhaustPS != null)
        {
            var emission = exhaustPS.emission;
            if (speed > 0.5f)
            {
                emission.rateOverTime = Mathf.Lerp(5f, 30f, speed / 10f);
            }
            else
            {
                emission.rateOverTime = 2f; // 정지 시에도 약간의 배기가스
            }
        }

        // 스파크: 고속일 때만
        if (sparkPS != null)
        {
            var emission = sparkPS.emission;
            emission.rateOverTime = speed > 4f ? Mathf.Lerp(0f, 15f, (speed - 4f) / 6f) : 0f;
        }
    }
}

using UnityEngine;

/// <summary>
/// 차량 뒤에 먼지/연기 파티클 트레일.
/// 이동 중일 때만 파티클 방출.
/// </summary>
public class DustTrail : MonoBehaviour
{
    private ParticleSystem ps;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        var dustObj = new GameObject("DustTrail");
        dustObj.transform.SetParent(transform, false);
        dustObj.transform.localPosition = new Vector3(0, -0.6f, 0);

        ps = dustObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new Color(0.6f, 0.55f, 0.5f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1f),
            new Keyframe(1, 0f)));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.6f, 0.55f, 0.5f), 0f),
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f) },
            new[] { new GradientAlphaKey(0.4f, 0f),
                    new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 1;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = Color.white;
    }

    private void Update()
    {
        if (ps == null || rb == null) return;

        var emission = ps.emission;
        emission.enabled = rb.velocity.sqrMagnitude > 1f;
    }
}

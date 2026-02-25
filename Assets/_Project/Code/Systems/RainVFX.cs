using UnityEngine;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// VFX de lluvia. Coloca el GameObject a la altura de spawn.
    ///
    /// ÁREA: Añade un BoxCollider al mismo GameObject (isTrigger = true),
    /// asígnalo al campo "Volume Box" y el sistema leerá su tamaño automáticamente.
    /// Si no asignas ninguno, usa los valores de "Manual Area".
    ///
    /// VELOCIDAD: Si ves velocidad baja del script antiguo, pulsa el menú ⋮
    /// del componente → Reset para aplicar los defaults nuevos.
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Rain VFX")]
    [RequireComponent(typeof(ParticleSystem))]
    public class RainVFX : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Rain Area")]
        [Tooltip("Arrastra aquí un BoxCollider (isTrigger=true) para definir el área de lluvia " +
                 "con sus handles. Si está vacío se usan los valores de Manual Area.")]
        public BoxCollider volumeBox;

        [Header("Manual Area  (ignorado si Volume Box está asignado)")]
        public float areaWidth  = 25f;
        public float areaDepth  = 25f;
        [Tooltip("Distancia en Y que caen las gotas (spawn → suelo).")]
        public float fallHeight = 22f;

        [Header("Intensity")]
        [Range(0f, 1f)]
        public float intensity       = 0.75f;
        public float maxEmissionRate = 3000f;

        [Header("Drop Physics")]
        public float dropSpeed         = 40f;
        public float dropSpeedVariance = 6f;
        public float windX             = -2.5f;
        public float windZ             = 0f;

        [Header("Drop Appearance  (ANCHO de la gota, no el largo)")]
        public float dropWidth = 0.012f;
        [ColorUsage(true, true)]
        public Color dropColor = new Color(0.55f, 0.78f, 1f, 0.5f);

        [Header("Splash")]
        public bool  enableSplash = true;
        public int   splashCount  = 3;
        public Color splashColor  = new Color(0.8f, 0.9f, 1f, 0.35f);

        [Header("Auto Wetness")]
        public RainShaderController shaderController;
        public float wetnessRate = 8f;

        // ── Private ───────────────────────────────────────────────────────────
        private ParticleSystem _ps;
        private ParticleSystem _splashPs;
        private float          _wetness;

        // ── Helpers ───────────────────────────────────────────────────────────
        private float EffectiveWidth  => volumeBox != null ? volumeBox.size.x * transform.lossyScale.x : areaWidth;
        private float EffectiveDepth  => volumeBox != null ? volumeBox.size.z * transform.lossyScale.z : areaDepth;
        private float EffectiveHeight => volumeBox != null ? volumeBox.size.y * transform.lossyScale.y : fallHeight;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        // Start en lugar de Awake para evitar conflicto con playOnAwake del PS
        private void Start()
        {
            _ps = GetComponent<ParticleSystem>();

            // Detener el PS (puede haberse iniciado por playOnAwake) antes de configurar
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            BuildDrops();
            if (enableSplash) BuildSplash();

            _ps.Play(true);
        }

        private void Update()
        {
            if (_ps == null) return;

            float spd = dropSpeed;
            float spv = dropSpeedVariance;
            float h   = EffectiveHeight;

            // ── Emisión ───────────────────────────────────────────────────────
            var em = _ps.emission;
            em.rateOverTime = maxEmissionRate * intensity;

            // ── Velocidad (refleja cambios del Inspector en tiempo real) ───────
            var vel = _ps.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(windX, windX);
            vel.y = new ParticleSystem.MinMaxCurve(-(spd + spv), -(spd - spv));
            vel.z = new ParticleSystem.MinMaxCurve(windZ, windZ);

            // ── Lifetime (depende de la velocidad) ────────────────────────────
            var main = _ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                h / (spd + spv),
                h / Mathf.Max(0.5f, spd - spv));

            // ── Área de emisión (refleja BoxCollider si cambia su tamaño) ─────
            var sh = _ps.shape;
            sh.scale = new Vector3(EffectiveWidth, 0.1f, EffectiveDepth);

            // ── Auto Wetness ──────────────────────────────────────────────────
            if (shaderController != null)
            {
                _wetness = Mathf.MoveTowards(_wetness, intensity,
                    Time.deltaTime / Mathf.Max(0.01f, wetnessRate));
                shaderController.SetWetness(_wetness);
            }
        }


        public void SetIntensity(float v) => intensity = Mathf.Clamp01(v);

        // ── Build drops ───────────────────────────────────────────────────────
        private void BuildDrops()
        {
            float h   = EffectiveHeight;
            float spd = dropSpeed;
            float spv = dropSpeedVariance;

            // ── Main ──────────────────────────────────────────────────────────
            var m             = _ps.main;
            m.loop            = true;
            m.playOnAwake     = false;
            m.maxParticles    = 15000;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startSpeed      = 0f;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(
                h / (spd + spv),
                h / Mathf.Max(0.5f, spd - spv));
            m.startSize       = new ParticleSystem.MinMaxCurve(dropWidth * 0.5f, dropWidth);
            m.startColor      = dropColor;
            m.gravityModifier = 0f;

            // ── Emission ──────────────────────────────────────────────────────
            var em          = _ps.emission;
            em.enabled      = true;
            em.rateOverTime = maxEmissionRate * intensity;

            // ── Shape ─────────────────────────────────────────────────────────
            var sh       = _ps.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale     = new Vector3(EffectiveWidth, 0.1f, EffectiveDepth);
            sh.position  = Vector3.zero;

            // ── Velocity (World) ──────────────────────────────────────────────
            // IMPORTANTE: los tres ejes deben usar el MISMO modo de MinMaxCurve.
            // Usamos TwoConstants en todos (dos floats) para que Unity no se queje.
            // X y Z pasan el mismo valor dos veces → comportamiento constante.
            var v     = _ps.velocityOverLifetime;
            v.enabled = true;
            v.space   = ParticleSystemSimulationSpace.World;
            v.x       = new ParticleSystem.MinMaxCurve(windX, windX);
            v.y       = new ParticleSystem.MinMaxCurve(-(spd + spv), -(spd - spv));
            v.z       = new ParticleSystem.MinMaxCurve(windZ, windZ);

            // ── Renderer ──────────────────────────────────────────────────────
            var r                = _ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode         = ParticleSystemRenderMode.Stretch;
            r.velocityScale      = 0.04f;
            r.lengthScale        = 2f;
            r.cameraVelocityScale = 0f;
            r.sortingFudge       = -10f;
            r.sharedMaterial     = MakeMat(dropColor);

            // ── Collision ─────────────────────────────────────────────────────
            var c                   = _ps.collision;
            c.enabled               = true;
            c.type                  = ParticleSystemCollisionType.World;
            c.mode                  = ParticleSystemCollisionMode.Collision3D;
            c.bounce                = 0f;
            c.lifetimeLoss          = 1f;
            c.sendCollisionMessages = enableSplash;
        }

        // ── Build splash ──────────────────────────────────────────────────────
        private void BuildSplash()
        {
            var go    = new GameObject("Rain_Splash");
            go.transform.SetParent(transform, false);
            _splashPs = go.AddComponent<ParticleSystem>();

            var m             = _splashPs.main;
            m.loop            = false;
            m.playOnAwake     = false;
            m.maxParticles    = 800;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
            m.startColor      = splashColor;
            m.gravityModifier = 2.5f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em          = _splashPs.emission;
            em.enabled      = true;
            em.rateOverTime = 0f;
            em.SetBurst(0, new ParticleSystem.Burst(0f, splashCount));

            var sh       = _splashPs.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius    = 0.04f;

            _splashPs.GetComponent<ParticleSystemRenderer>().sharedMaterial = MakeMat(splashColor);

            var sub     = _ps.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(_splashPs,
                ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // ── Material helper ───────────────────────────────────────────────────
        private static Material MakeMat(Color color)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.renderQueue = 3000;
            return mat;
        }

        // ── Gizmos (estilo BoxCollider) ───────────────────────────────────────
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Wireframe azul siempre visible (como BoxCollider en verde)
            Gizmos.color = new Color(0.15f, 0.55f, 1f, 0.35f);
            Vector3 c = transform.position - Vector3.up * (EffectiveHeight * 0.5f);
            Vector3 s = new Vector3(EffectiveWidth, EffectiveHeight, EffectiveDepth);
            Gizmos.DrawWireCube(c, s);
        }

        private void OnDrawGizmosSelected()
        {
            float w = EffectiveWidth;
            float d = EffectiveDepth;
            float h = EffectiveHeight;

            // Plano de spawn (techo)
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(w, 0.05f, d));

            // Líneas de caída con ángulo de viento
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.7f);
            Vector3 fall = new Vector3(windX, -dropSpeed, windZ).normalized * h;
            for (int xi = 0; xi < 4; xi++)
            for (int zi = 0; zi < 4; zi++)
            {
                float fx = Mathf.Lerp(-w * .5f, w * .5f, xi / 3f);
                float fz = Mathf.Lerp(-d * .5f, d * .5f, zi / 3f);
                Vector3 o = transform.position + new Vector3(fx, 0, fz);
                Gizmos.DrawLine(o, o + fall);
                Gizmos.DrawSphere(o + fall, 0.05f);
            }
        }
        #endif
    }
}

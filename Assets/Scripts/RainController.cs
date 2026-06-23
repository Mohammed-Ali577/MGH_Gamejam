using System.Collections;
using UnityEngine;

/// <summary>
/// Simple, reusable system that makes it "rain" randomly in the scene.
/// - Attach to a central scene object (e.g. an empty GameObject called "Weather").
/// - Assign a ParticleSystem prefab configured for rain (prefab should NOT be a child in the scene).
/// - Optionally assign a looping rain AudioClip and/or thunder clip via an AudioSource.
/// - The controller periodically rolls a random chance; when it succeeds it starts rain for a random duration
///   with a randomized intensity (emission multiplier).
/// - When rain starts it will also attempt to "clean" eligible objects with a per-object chance (uses SendMessage so it won't require a specific method).
/// - Public ForceStart/Stop methods allow scripted control (for events, debug, or designer hooks).
/// </summary>
public class RainController : MonoBehaviour
{
    [Header("Prefabs & Audio")]
    [Tooltip("ParticleSystem prefab that emits rain. Prefab should be configured for continuous emission.")]
    public ParticleSystem rainParticlePrefab;

    [Tooltip("Optional looping rain sound. Played while raining if provided.")]
    public AudioClip rainLoopClip;

    [Tooltip("Optional thunder/clap to play at rain start.")]
    public AudioClip thunderClip;

    [Header("Random Rain Schedule")]
    [Tooltip("How often (seconds) the system checks whether to start a rain event.")]
    public float checkInterval = 30f;

    [Tooltip("Probability (0..1) to start rain each check.")]
    [Range(0f, 1f)]
    public float chanceToStartPerCheck = 0.2f;

    [Tooltip("Minimum and maximum rain duration (seconds).")]
    public float minDuration = 10f;
    public float maxDuration = 60f;

    [Header("Intensity")]
    [Tooltip("Multiplier applied to ParticleSystem emission rate while raining.")]
    public float minIntensity = 0.6f;
    public float maxIntensity = 1.4f;

    [Header("Cleaning")]
    [Tooltip("Chance per eligible object to be cleaned when rain starts (0..1).")]
    [Range(0f, 1f)]
    public float cleanChancePerObject = 0.25f;

    [Tooltip("If true the system performs a single cleaning pass immediately when rain starts.")]
    public bool cleanOnceAtStart = true;

    [Header("Runtime")]
    [Tooltip("Parent transform where the instantiated rain will be placed. If null, uses this transform.")]
    public Transform rainParent;

    // runtime state
    private ParticleSystem rainInstance;
    private AudioSource sfxSource;
    private AudioSource loopSource;
    private bool isRaining = false;
    private float defaultEmissionMultiplier = 1f;

    private void Awake()
    {
        rainParent = rainParent ?? this.transform;

        // Create audio sources for loop and transient SFX
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f; // global

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (rainParticlePrefab != null)
        {
            // Instantiate but keep stopped/inactive so it's ready to be used.
            rainInstance = Instantiate(rainParticlePrefab, rainParent.position, Quaternion.identity, rainParent);
            rainInstance.gameObject.SetActive(true);
            rainInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // capture default emission multiplier so we can restore later
            var em = rainInstance.emission;
            defaultEmissionMultiplier = em.rateOverTimeMultiplier;
        }

        StartCoroutine(RandomRainLoop());
    }

    private IEnumerator RandomRainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // if already raining skip check (rain continues until its routine stops it)
            if (isRaining) continue;

            if (Random.value <= chanceToStartPerCheck)
            {
                float duration = Random.Range(minDuration, maxDuration);
                float intensity = Random.Range(minIntensity, maxIntensity);
                StartCoroutine(RainRoutine(duration, intensity));
            }
        }
    }

    private IEnumerator RainRoutine(float duration, float intensity)
    {
        if (rainParticlePrefab == null)
        {
            Debug.LogWarning("RainController: No rainParticlePrefab assigned.");
            yield break;
        }

        isRaining = true;

        // Prepare particle system
        var em = rainInstance.emission;
        em.rateOverTimeMultiplier = intensity;
        rainInstance.Play(true);

        // Start loop audio if assigned
        if (rainLoopClip != null)
        {
            loopSource.clip = rainLoopClip;
            loopSource.volume = Mathf.Clamp01(0.6f * intensity);
            loopSource.Play();
        }

        // Play thunder once at start if provided (optional)
        if (thunderClip != null)
        {
            sfxSource.PlayOneShot(thunderClip);
        }

        // Perform a cleaning pass if enabled (calls SendMessage so it won't break if target doesn't implement handler)
        if (cleanOnceAtStart)
            CleanObjectsDuringRain();

        yield return new WaitForSeconds(duration);

        // Stop rain
        rainInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        em.rateOverTimeMultiplier = defaultEmissionMultiplier;

        if (loopSource.isPlaying)
            loopSource.Stop();

        isRaining = false;
    }

    /// <summary>
    /// Forcefully starts a rain event with specified duration and intensity.
    /// </summary>
    public void ForceStartRain(float duration, float intensity)
    {
        intensity = Mathf.Clamp(intensity, 0.01f, 10f);
        StartCoroutine(RainRoutine(duration, intensity));
    }

    /// <summary>
    /// Forcefully stops rain immediately.
    /// </summary>
    public void ForceStopRain()
    {
        if (rainInstance != null)
        {
            rainInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var em = rainInstance.emission;
            em.rateOverTimeMultiplier = defaultEmissionMultiplier;
        }

        if (loopSource != null && loopSource.isPlaying)
            loopSource.Stop();

        StopAllCoroutines();
        // restart the random loop so future random rain can still happen
        StartCoroutine(RandomRainLoop());
        isRaining = false;
    }

    // Expose state for other systems (e.g. enemies or player)
    public bool IsRaining => isRaining;

    // Performs a single pass over scene objects that can be cleaned by rain.
    // Uses SendMessage so this call is safe even if the component doesn't implement the handler.
    private void CleanObjectsDuringRain()
    {
        // Use the newer API to avoid the obsolete warning and allow specifying sort mode for better performance.
        var spawners = UnityEngine.Object.FindObjectsByType<EnemySpawner>(UnityEngine.FindObjectsSortMode.None);
        foreach (var s in spawners)
        {
            if (s == null) continue;
            if (Random.value <= cleanChancePerObject)
            {
                // Use SendMessage so the call is optional and won't cause a compile/link dependency.
                s.gameObject.SendMessage("CleanByRain", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}

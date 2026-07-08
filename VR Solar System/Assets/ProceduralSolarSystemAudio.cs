using UnityEngine;
using System.Collections;

public class ProceduralSolarSystemAudio : MonoBehaviour
{
    public static ProceduralSolarSystemAudio Instance { get; private set; }

    [Header("Volumes")]
    [Range(0f, 1f)] public float ambientVolume = 0.18f;
    [Range(0f, 1f)] public float laserVolume = 0.55f;
    [Range(0f, 1f)] public float hitVolume = 0.18f;
    [Range(0f, 1f)] public float splitVolume = 0.70f;
    [Range(0f, 1f)] public float shipThrusterVolume = 0.50f;
    [Range(0f, 1f)] public float playerThrusterVolume = 0.28f;

    [Header("Split / Blast")]
    [Tooltip("Lower pitch makes the imported breaking sound deeper and longer.")]
    [Range(0.45f, 1f)] public float splitPitch = 0.72f;
    [Tooltip("Short hit version of the imported breaking sound for asteroids, lobby orbs, and laser impacts.")]
    [Range(0.08f, 0.8f)] public float hitBlastDuration = 0.32f;
    [Range(0.7f, 1.4f)] public float hitBlastPitch = 1.08f;

    private AudioSource ambientSource;
    private AudioSource oneShotSource;
    private AudioSource hitBlastSource;
    private AudioSource splitSource;
    private AudioSource shipThrusterSource;
    private AudioSource playerThrusterSource;

    private AudioClip ambientClip;
    private AudioClip laserClip;
    private AudioClip hitClip;
    private AudioClip splitClip;
    private AudioClip shipThrusterClip;
    private AudioClip playerThrusterClip;
    private Coroutine stopHitBlastRoutine;

    public static ProceduralSolarSystemAudio Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject existing = GameObject.Find("Procedural Solar System Audio");

        if (existing != null)
        {
            Instance = existing.GetComponent<ProceduralSolarSystemAudio>();
        }

        if (Instance == null)
        {
            GameObject audioObject = new GameObject("Procedural Solar System Audio");
            Instance = audioObject.AddComponent<ProceduralSolarSystemAudio>();
        }

        Instance.Initialize();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    void Initialize()
    {
        if (ambientSource != null)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);

        ambientSource = CreateSource("Ambient Source", true, ambientVolume);
        oneShotSource = CreateSource("One Shot Source", false, 1f);
        hitBlastSource = CreateSource("Short Hit Blast Source", false, hitVolume);
        splitSource = CreateSource("Split Blast Source", false, splitVolume);
        shipThrusterSource = CreateSource("Ship Thruster Source", true, 0f);
        playerThrusterSource = CreateSource("Player Thruster Source", true, 0f);

        ambientClip = CreateAmbientClip();
        laserClip = CreateLaserClip();
        hitClip = CreateHitClip();
        splitClip = Resources.Load<AudioClip>("PlanetBreakingSound");

        if (splitClip == null)
        {
            splitClip = CreateSplitClip();
        }

        shipThrusterClip = CreateThrusterClip(0.90f);
        playerThrusterClip = CreateThrusterClip(0.55f);

        ambientSource.clip = ambientClip;
        ambientSource.Play();

        shipThrusterSource.clip = shipThrusterClip;
        shipThrusterSource.Play();

        playerThrusterSource.clip = playerThrusterClip;
        playerThrusterSource.Play();
    }

    AudioSource CreateSource(string objectName, bool loop, float volume)
    {
        GameObject sourceObject = new GameObject(objectName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.pitch = 1f;
        return source;
    }

    public void PlayLaser()
    {
        oneShotSource.PlayOneShot(laserClip, laserVolume);
    }

    public void PlayHit()
    {
        if (splitClip == null)
        {
            oneShotSource.PlayOneShot(hitClip, hitVolume);
            return;
        }

        hitBlastSource.Stop();
        hitBlastSource.clip = splitClip;
        hitBlastSource.pitch = hitBlastPitch;
        hitBlastSource.volume = hitVolume;
        hitBlastSource.Play();

        if (stopHitBlastRoutine != null)
        {
            StopCoroutine(stopHitBlastRoutine);
        }

        stopHitBlastRoutine = StartCoroutine(StopHitBlastAfterDelay());
    }

    public void PlaySplit()
    {
        splitSource.pitch = splitPitch;
        splitSource.PlayOneShot(splitClip, splitVolume);
    }

    IEnumerator StopHitBlastAfterDelay()
    {
        yield return new WaitForSeconds(hitBlastDuration);

        if (hitBlastSource != null)
        {
            hitBlastSource.Stop();
        }

        stopHitBlastRoutine = null;
    }

    public void SetShipThruster(bool active, float intensity)
    {
        shipThrusterSource.volume = active ? shipThrusterVolume * Mathf.Clamp01(intensity) : 0f;
        shipThrusterSource.pitch = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(intensity));
    }

    public void SetPlayerThruster(bool active, float intensity)
    {
        playerThrusterSource.volume = active ? playerThrusterVolume * Mathf.Clamp01(intensity) : 0f;
        playerThrusterSource.pitch = Mathf.Lerp(0.95f, 1.20f, Mathf.Clamp01(intensity));
    }

    AudioClip CreateAmbientClip()
    {
        return CreateClip("Solar Ambient", 4.0f, (i, t) =>
        {
            float low = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.22f;
            float mid = Mathf.Sin(2f * Mathf.PI * 110f * t + Mathf.Sin(t * 0.7f) * 2f) * 0.08f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.015f;
            return (low + mid + shimmer) * 0.45f;
        });
    }

    AudioClip CreateLaserClip()
    {
        return CreateClip("Laser Zap", 0.18f, (i, t) =>
        {
            float progress = Mathf.Clamp01(t / 0.18f);
            float env = Mathf.Exp(-t * 22f) * (1f - progress * 0.35f);
            float sweep = Mathf.Lerp(2600f, 620f, progress);
            float main = Mathf.Sin(2f * Mathf.PI * sweep * t) * 0.78f;
            float chirp = Mathf.Sin(2f * Mathf.PI * sweep * 1.75f * t) * 0.26f;
            float spark = (RandomValue(i) * 2f - 1f) * Mathf.Exp(-t * 34f) * 0.10f;
            return (main + chirp + spark) * env;
        });
    }

    AudioClip CreateHitClip()
    {
        return CreateClip("Laser Hit", 0.20f, (i, t) =>
        {
            float env = Mathf.Exp(-t * 18f);
            float noise = RandomValue(i) * 2f - 1f;
            float tone = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.45f;
            return (noise * 0.55f + tone) * env;
        });
    }

    AudioClip CreateSplitClip()
    {
        return CreateClip("Planet Split Boom", 2.0f, (i, t) =>
        {
            float boom = Mathf.Sin(2f * Mathf.PI * 42f * t) * Mathf.Exp(-t * 1.55f);
            float deepTail = Mathf.Sin(2f * Mathf.PI * 24f * t) * Mathf.Exp(-t * 0.95f);
            float crack = (RandomValue(i) * 2f - 1f) * Mathf.Exp(-t * 5.5f);
            float high = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 8f);
            return boom * 0.70f + deepTail * 0.45f + crack * 0.30f + high * 0.06f;
        });
    }

    AudioClip CreateThrusterClip(float toneStrength)
    {
        return CreateClip("Thruster Loop", 2.0f, (i, t) =>
        {
            float rumble = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.35f;
            float tone = Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.18f * toneStrength;
            float noise = (RandomValue(i) * 2f - 1f) * 0.28f;
            return (rumble + tone + noise) * 0.55f;
        });
    }

    delegate float SampleGenerator(int index, float time);

    AudioClip CreateClip(string clipName, float duration, SampleGenerator generator)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = Mathf.Clamp(generator(i, i / (float)sampleRate), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    float RandomValue(int index)
    {
        uint x = (uint)index * 747796405u + 2891336453u;
        x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737;
        x = (x >> 22) ^ x;
        return (x & 0xFFFFFF) / 16777215f;
    }
}

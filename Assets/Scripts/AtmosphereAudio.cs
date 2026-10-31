using UnityEngine;

/// <summary>
/// Создаёт и управляет звуками лаборатории.
/// Звуки генерируются во время запуска, поэтому внешние файлы не нужны.
/// </summary>
public class AtmosphereAudio : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float reactorHumVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float alarmVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float coolingVolume = 0.28f;
    [SerializeField, Min(0f)] private float alarmPulseSpeed = 8f;

    [Header("Steam")]
    [Tooltip("Громкость шума пара на каждой точке выхода.")]
    [SerializeField, Range(0f, 1f)] private float steamVolume = 0.25f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource alarmSource;
    [SerializeField] private AudioSource coolingSource;
    [SerializeField] private AudioSource reactorHumSource;

    [Header("3D Sound Positions")]
    [SerializeField] private Transform alarmPoint;
    [Tooltip("Все точки, в которых должен быть слышен пар. Можно добавить любое количество точек.")]
    [SerializeField] private Transform[] steamPoints;
    [SerializeField] private Transform coolingPoint;
    [SerializeField] private Transform reactorHumPoint;

    [Header("Generated Audio")]
    [SerializeField, Min(20f)] private float reactorHumFrequency = 70f;
    [SerializeField, Min(20f)] private float alarmFrequency = 440f;
    [SerializeField, Min(0f)] private float generatedToneVolume = 0.22f;
    [SerializeField, Min(0f)] private float generatedSteamVolume = 0.18f;
    [SerializeField, Min(0f)] private float alarmMinVolume = 0.05f;
    [SerializeField, Min(0f)] private float alarmMaxVolume = 0.22f;
    [SerializeField] private bool addListenerToMainCamera;
    [SerializeField, Min(0.1f)] private float threeDMinDistance = 2f;
    [SerializeField, Min(1f)] private float threeDMaxDistance = 25f;

    private AudioSource reactorHum;
    private AudioSource alarm;
    private AudioSource[] steamSources;
    private AudioSource cooling;

    private bool alarmEnabled;
    private bool steamEnabled;

    private void Awake()
    {
        reactorHum = ConfigureSource(reactorHumSource, CreateTone("ReactorHum", reactorHumFrequency, generatedToneVolume), reactorHumPoint);
        reactorHum.volume = reactorHumVolume;

        alarm = ConfigureSource(alarmSource, CreateTone("Alarm", alarmFrequency, generatedToneVolume), alarmPoint);
        alarm.volume = 0f;

        AudioClip steamClip = CreateNoise("Steam", generatedSteamVolume);
        steamSources = ConfigureSteamSources(steamClip);

        cooling = ConfigureSource(coolingSource, CreateTone("Cooling", reactorHumFrequency + 110f, generatedToneVolume), coolingPoint);
        cooling.volume = 0f;

        if (addListenerToMainCamera && Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }
    }

    private AudioSource ConfigureSource(AudioSource source, AudioClip clip, Transform point)
    {
        if (point != null)
        {
            source = point.GetComponent<AudioSource>();
            if (source == null) source = point.gameObject.AddComponent<AudioSource>();
        }
        else if (source == null)
        {
            GameObject sourceObject = new GameObject(clip.name + " Source");
            sourceObject.transform.SetParent(transform, false);
            source = sourceObject.AddComponent<AudioSource>();
        }
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = threeDMinDistance;
        source.maxDistance = threeDMaxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.Play();
        return source;
    }

    private AudioSource[] ConfigureSteamSources(AudioClip clip)
    {
        if (steamPoints == null || steamPoints.Length == 0)
        {
            AudioSource fallbackSource = ConfigureSource(null, clip, null);
            fallbackSource.volume = 0f;
            return new[] { fallbackSource };
        }

        AudioSource[] sources = new AudioSource[steamPoints.Length];

        for (int i = 0; i < steamPoints.Length; i++)
        {
            sources[i] = ConfigureSource(null, clip, steamPoints[i]);
            sources[i].volume = 0f;
        }

        return sources;
    }

    public void SetAlarm(bool enabled)
    {
        alarmEnabled = enabled;
        alarm.volume = enabled ? alarmVolume : 0f;
    }

    public void SetSteam(bool enabled)
    {
        steamEnabled = enabled;

        if (steamSources == null) return;

        foreach (AudioSource source in steamSources)
        {
            if (source != null) source.volume = enabled ? steamVolume : 0f;
        }
    }

    public void SetCooling(bool enabled)
    {
        cooling.volume = enabled ? coolingVolume : 0f;
    }

    private void Update()
    {
        if (alarmEnabled)
        {
            float pulse = (Mathf.Sin(Time.time * alarmPulseSpeed) + 1f) * 0.5f;
            alarm.volume = Mathf.Lerp(alarmMinVolume, alarmMaxVolume, pulse);
        }

        if (steamSources != null)
        {
            float target = steamEnabled ? steamVolume : 0f;

            foreach (AudioSource source in steamSources)
            {
                if (source != null) source.volume = target;
            }
        }
    }

    private AudioClip CreateTone(string name, float frequency, float volume)
    {
        const int sampleRate = 44100;
        const int durationSeconds = 2;
        int sampleCount = sampleRate * durationSeconds;

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateNoise(string name, float volume)
    {
        const int sampleRate = 44100;
        const int durationSeconds = 2;
        int sampleCount = sampleRate * durationSeconds;

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(3);

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = ((float)random.NextDouble() * 2f - 1f) * volume;
        }

        clip.SetData(samples, 0);
        return clip;
    }
}

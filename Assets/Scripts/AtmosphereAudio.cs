using UnityEngine;

/// <summary>
/// Создаёт и управляет звуками лаборатории.
/// Звуки генерируются во время запуска, поэтому внешние файлы не нужны.
/// </summary>
public class AtmosphereAudio : MonoBehaviour
{
    private AudioSource reactorHum;
    private AudioSource alarm;
    private AudioSource steam;
    private AudioSource cooling;

    private bool alarmEnabled;

    private void Awake()
    {
        reactorHum = CreateLoopingSource(CreateTone("ReactorHum", 70f, 0.22f));
        reactorHum.volume = 0.35f;

        alarm = CreateLoopingSource(CreateTone("Alarm", 440f, 0.35f));
        alarm.volume = 0f;

        steam = CreateLoopingSource(CreateNoise("SteamHiss", 0.18f));
        steam.volume = 0f;

        cooling = CreateLoopingSource(CreateTone("Cooling", 180f, 0.22f));
        cooling.volume = 0f;

        if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }
    }

    private AudioSource CreateLoopingSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.Play();
        return source;
    }

    public void SetAlarm(bool enabled)
    {
        alarmEnabled = enabled;
        alarm.volume = enabled ? 0.18f : 0f;
    }

    public void SetSteam(bool enabled)
    {
        steam.volume = enabled ? 0.25f : 0f;
    }

    public void SetCooling(bool enabled)
    {
        cooling.volume = enabled ? 0.28f : 0f;
    }

    private void Update()
    {
        if (!alarmEnabled)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
        alarm.volume = Mathf.Lerp(0.05f, 0.22f, pulse);
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

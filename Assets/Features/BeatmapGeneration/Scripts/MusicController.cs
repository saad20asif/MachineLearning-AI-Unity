using UnityEngine;
using System.Collections;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;

public enum BeatType
{
    Beat,
    Onset,
    MFCC
}

[System.Serializable]
public class MusicData
{
    public float tempo;
    public int[] beat_frames;   // These will be converted to time in seconds.
    public float[] onset_times; // These are already in seconds.
    public float[][] mfccs;
}

public class MusicController : SerializedMonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI tempoText;
    public TextMeshProUGUI styleText;
    public AudioSource audioSource;
    public TextAsset TextAsset;
    public Transform[] cubes;

    [Header("Settings")]
    public BeatType beatType = BeatType.Beat;
    public float hopLength = 512f;
    public float sampleRate = 22050f; // Must match the value used in Python/Librosa

    public MusicData musicData;

    private int currentIndex = 0;
    // Track if a cube is currently pulsing (to avoid overlapping effects)
    private bool[] cubePulsing;

    // Reference to the running sync coroutine so we can restart it if needed.
    private Coroutine syncCoroutine;

    void Start()
    {
        Initialize();

        if (cubes != null && cubes.Length > 0)
            cubePulsing = new bool[cubes.Length];
        else
            cubePulsing = new bool[0];

        audioSource.Play();
        syncCoroutine = StartCoroutine(SyncWithBeatData());
    }

    [Button]
    private void Initialize()
    {
        if (TextAsset != null)
        {
            string json = TextAsset.text;
            musicData = JsonUtility.FromJson<MusicData>(json);

            if (tempoText != null)
                tempoText.text = "Tempo: " + musicData.tempo.ToString("F2");

            if (styleText != null)
                styleText.text = "Style: " + beatType.ToString();

            currentIndex = 0;
            Debug.Log("Loaded Tempo: " + musicData.tempo);
        }
        else
        {
            Debug.LogError("TextAsset is null.");
        }
    }

    void Update()
    {
        // Press 1 for Beat, 2 for Onset, 3 for MFCC
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetBeatType(BeatType.Beat);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetBeatType(BeatType.Onset);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetBeatType(BeatType.MFCC);
        }
    }

    void SetBeatType(BeatType newType)
    {
        if (beatType != newType)
        {
            beatType = newType;
            if (styleText != null)
                styleText.text = "Style: " + beatType.ToString();
            Debug.Log("Switching BeatType to: " + beatType);

            // Reset the current index to start from the beginning
            currentIndex = 0;

            // Stop the current sync routine and restart it with new settings
            if (syncCoroutine != null)
                StopCoroutine(syncCoroutine);

            syncCoroutine = StartCoroutine(SyncWithBeatData());
        }
    }

    IEnumerator SyncWithBeatData()
    {
        float[] timeArray = GetCurrentTimeArray();
        while (audioSource.isPlaying && currentIndex < timeArray.Length)
        {
            float currentTriggerTime = timeArray[currentIndex];

            if (audioSource.time >= currentTriggerTime)
            {
                TriggerEffect(currentIndex);
                Debug.Log($"[{beatType}] Triggered at {audioSource.time:F2} (expected {currentTriggerTime:F2})");
                currentIndex++;
            }
            yield return null;
        }
    }

    float[] GetCurrentTimeArray()
    {
        switch (beatType)
        {
            case BeatType.Beat:
                return ConvertFramesToSeconds(musicData.beat_frames);
            case BeatType.Onset:
                // Onset times are already in seconds – no conversion needed.
                return musicData.onset_times;
            case BeatType.MFCC:
                return GenerateDummyMFCCTriggers(); // Optional MFCC logic.
            default:
                return new float[0];
        }
    }

    float[] ConvertFramesToSeconds(int[] frames)
    {
        if (frames == null) return new float[0];

        float[] seconds = new float[frames.Length];
        float step = hopLength / sampleRate;
        for (int i = 0; i < frames.Length; i++)
        {
            seconds[i] = frames[i] * step;
        }
        return seconds;
    }

    float[] GenerateDummyMFCCTriggers()
    {
        // Dummy MFCC trigger logic: trigger every 0.1 seconds over the song's length.
        int count = Mathf.FloorToInt(audioSource.clip.length / 0.1f);
        float[] dummy = new float[count];
        for (int i = 0; i < count; i++)
        {
            dummy[i] = i * 0.1f;
        }
        return dummy;
    }

    void TriggerEffect(int index)
    {
        if (cubes == null || cubes.Length == 0) return;

        int cubeIndex = index % cubes.Length;

        // Prevent overlapping pulses on the same cube.
        if (cubePulsing[cubeIndex])
            return;

        cubePulsing[cubeIndex] = true;
        Transform cube = cubes[cubeIndex];
        StartCoroutine(Pulse(cube, cubeIndex));
    }

    IEnumerator Pulse(Transform target, int cubeIndex)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * 1.5f;

        Renderer rend = target.GetComponent<Renderer>();
        Color originalColor = rend.material.color;
        Color pulseColor = Color.Lerp(originalColor, Color.red, 0.8f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            rend.material.color = Color.Lerp(originalColor, pulseColor, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            rend.material.color = Color.Lerp(pulseColor, originalColor, t);
            yield return null;
        }

        cubePulsing[cubeIndex] = false;
    }
}

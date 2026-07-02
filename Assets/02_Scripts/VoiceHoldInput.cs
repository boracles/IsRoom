using UnityEngine;

public class VoiceHoldInput : MonoBehaviour
{
    public bool IsHolding { get; private set; }
    public float CurrentVolume { get; private set; }

    [Header("Microphone")]
    public bool useMicrophone = true;
    public int sampleWindow = 128;
    public float volumeMultiplier = 30f;

    private AudioClip micClip;
    private string micName;
    private const int sampleRate = 44100;

    private void Start()
    {
        if (!useMicrophone) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone found. Fake volume will be used.");
            useMicrophone = false;
            return;
        }

        micName = Microphone.devices[0];
        micClip = Microphone.Start(micName, true, 10, sampleRate);
    }

    private void Update()
    {
        IsHolding = Input.GetMouseButton(0) || HasTouchHold();

        if (useMicrophone && micClip != null)
        {
            CurrentVolume = GetMicVolume();
        }
        else
        {
            CurrentVolume = IsHolding ? Mathf.PingPong(Time.time * 0.5f, 0.35f) + 0.05f : 0f;
        }
    }

    private bool HasTouchHold()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Ended;
    }

    private float GetMicVolume()
    {
        int micPosition = Microphone.GetPosition(micName) - sampleWindow;

        if (micPosition < 0)
        {
            return 0f;
        }

        float[] samples = new float[sampleWindow];
        micClip.GetData(samples, micPosition);

        float sum = 0f;

        for (int i = 0; i < sampleWindow; i++)
        {
            sum += samples[i] * samples[i];
        }

        float rms = Mathf.Sqrt(sum / sampleWindow);
        return Mathf.Clamp01(rms * volumeMultiplier);
    }
}
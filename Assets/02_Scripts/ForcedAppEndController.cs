using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class ForcedAppEndController : MonoBehaviour
{
    public enum ForcedEndMode
    {
        SpecificDateTime,
        ElapsedAfterAppStart
    }

    [Header("Enable")]
    public bool enableForcedEnd = true;

    [Header("Forced End Mode")]
    public ForcedEndMode forcedEndMode = ForcedEndMode.ElapsedAfterAppStart;

    [Header("Forced End Date")]
    public int targetMonth = 7;
    public int targetDay = 14;

    [Header("Forced End Time")]
    public int targetHour = 17;
    public int targetMinute = 15;
    public int targetSecond = 0;

    [Tooltip("지정한 날짜와 시각 이후에 앱을 켰을 때도 바로 종료할지 여부")]
    public bool triggerImmediatelyIfPastTime = true;

    [Header("Elapsed End Time")]
    [Tooltip("앱 실행 후 몇 초 뒤에 강제 종료 루틴을 시작할지. 3분 30초 = 210초")]
    public float endAfterSeconds = 210f;

    [Header("UI")]
    public GameObject forcedEndPanel;
    public TMP_Text forcedEndText;

    [TextArea]
    public string forcedEndMessage = "이제 화면 밖의 소리를 들어볼 시간이에요.\n휴대폰을 내려주세요.";

    public float messageShowTime = 4f;

    [Header("Fade")]
    public float audioFadeOutTime = 2f;

    [Header("Sequence Controllers To Stop")]
    public IRoomSequenceManager iRoomSequenceManager;
    public RoomStepController roomStepController;
    public FinalSequenceController finalSequenceController;
    public FinalDoorUnlockSequence finalDoorUnlockSequence;

    [Header("Input / Detection To Disable")]
    public VoiceHoldInput voiceInput;
    public CubeFaceRoomDetector cubeFaceRoomDetector;

    [Header("Optional Roots To Hide")]
    public GameObject scanGuideGroup;
    public GameObject touchGuideObject;
    public GameObject keyholeGuideObject;

    private bool hasTriggered = false;
    private DateTime targetDateTime;
    private float appStartTime;

    private void Start()
    {
        appStartTime = Time.time;

        DateTime now = DateTime.Now;

        targetDateTime = new DateTime(
            now.Year,
            targetMonth,
            targetDay,
            targetHour,
            targetMinute,
            targetSecond
        );

        if (forcedEndPanel != null)
        {
            forcedEndPanel.SetActive(false);
        }

        Debug.Log($"[ForcedAppEndController] 강제 종료 모드: {forcedEndMode}");
        Debug.Log($"[ForcedAppEndController] 날짜/시각 기준: {targetDateTime:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"[ForcedAppEndController] 앱 실행 후 종료 기준: {endAfterSeconds}초");
    }

    private void Update()
    {
        if (!enableForcedEnd) return;
        if (hasTriggered) return;

        switch (forcedEndMode)
        {
            case ForcedEndMode.SpecificDateTime:
                CheckSpecificDateTimeEnd();
                break;

            case ForcedEndMode.ElapsedAfterAppStart:
                CheckElapsedTimeEnd();
                break;
        }
    }

    private void CheckSpecificDateTimeEnd()
    {
        DateTime now = DateTime.Now;

        // 지정 날짜 이전이면 실행 안 함
        if (now.Date < targetDateTime.Date)
        {
            return;
        }

        // 지정 날짜 이후면 실행 안 함
        // 즉, 정확히 targetMonth / targetDay에만 작동
        if (now.Date > targetDateTime.Date)
        {
            return;
        }

        if (now >= targetDateTime)
        {
            if (!triggerImmediatelyIfPastTime)
            {
                return;
            }

            StartForcedEnd();
        }
    }

    private void CheckElapsedTimeEnd()
    {
        float elapsed = Time.time - appStartTime;

        if (elapsed >= endAfterSeconds)
        {
            StartForcedEnd();
        }
    }

    [ContextMenu("Test Forced End Now")]
    public void StartForcedEnd()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        Debug.Log("[ForcedAppEndController] 강제 종료 시퀀스 시작.");

        StartCoroutine(ForcedEndRoutine());
    }

    private IEnumerator ForcedEndRoutine()
    {
        StopKnownSequences();

        DisableInputAndDetection();

        HideInteractionGuides();

        ShowForcedEndMessage();

        yield return StartCoroutine(FadeOutAllAudio());

        yield return new WaitForSeconds(messageShowTime);

        QuitApplication();
    }

    private void StopKnownSequences()
    {
        if (iRoomSequenceManager != null)
        {
            iRoomSequenceManager.StopAllCoroutines();
        }

        if (roomStepController != null)
        {
            roomStepController.StopAllCoroutines();
        }

        if (finalSequenceController != null)
        {
            finalSequenceController.StopAllCoroutines();
        }

        if (finalDoorUnlockSequence != null)
        {
            finalDoorUnlockSequence.StopAllCoroutines();
        }

        Debug.Log("[ForcedAppEndController] 주요 시퀀스 코루틴 정지 완료.");
    }

    private void DisableInputAndDetection()
    {
        if (voiceInput != null)
        {
            voiceInput.enabled = false;
        }

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.enabled = false;
        }
    }

    private void HideInteractionGuides()
    {
        if (scanGuideGroup != null)
        {
            scanGuideGroup.SetActive(false);
        }

        if (touchGuideObject != null)
        {
            touchGuideObject.SetActive(false);
        }

        if (keyholeGuideObject != null)
        {
            keyholeGuideObject.SetActive(false);
        }
    }

    private void ShowForcedEndMessage()
    {
        if (forcedEndPanel != null)
        {
            forcedEndPanel.SetActive(true);
        }

        if (forcedEndText != null)
        {
            forcedEndText.text = forcedEndMessage;
        }
    }

    private IEnumerator FadeOutAllAudio()
    {
#if UNITY_2023_1_OR_NEWER
        AudioSource[] sources = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
#else
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
#endif

        if (sources == null || sources.Length == 0)
        {
            yield break;
        }

        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null) continue;
            startVolumes[i] = sources[i].volume;
        }

        float elapsed = 0f;

        while (elapsed < audioFadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / audioFadeOutTime);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;

                sources[i].volume = Mathf.Lerp(
                    startVolumes[i],
                    0f,
                    t
                );
            }

            yield return null;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null) continue;

            sources[i].volume = 0f;
            sources[i].Stop();
        }

        Debug.Log($"[ForcedAppEndController] AudioSource {sources.Length}개 페이드아웃 후 정지.");
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("[ForcedAppEndController] 앱 종료 지점입니다. 실제 기기에서는 Application.Quit 실행.");
#else
        Application.Quit();
#endif
    }
}
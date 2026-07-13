using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    public Button restartButton;

    [Header("Elapsed Timer Start")]
    [Tooltip("앱 실행과 동시에 카운트할지 여부. 시작 버튼 기준으로 재려면 false.")]
    public bool startElapsedTimerAutomatically = false;

    private bool elapsedTimerStarted = false;
    private float elapsedTimerStartTime = 0f;

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
    private bool restartButtonVisible = false;

    private void Start()
    {
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

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
                restartButton.onClick.RemoveListener(RestartBeforeForcedEnd);
                restartButton.onClick.AddListener(RestartBeforeForcedEnd);
            }
        }

        if (forcedEndMode == ForcedEndMode.ElapsedAfterAppStart && startElapsedTimerAutomatically)
        {
            StartElapsedTimerFromNow();
        }

        Debug.Log($"[ForcedAppEndController] 강제 종료 모드: {forcedEndMode}");
        Debug.Log($"[ForcedAppEndController] 날짜/시각 기준: {targetDateTime:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"[ForcedAppEndController] 경과 시간 종료 기준: {endAfterSeconds}초");
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
        if (!elapsedTimerStarted)
        {
            return;
        }

        float elapsed = Time.time - elapsedTimerStartTime;

        if (elapsed >= endAfterSeconds)
        {
            StartForcedEnd();
        }
    }

    public void StartElapsedTimerFromNow()
    {
        if (elapsedTimerStarted)
        {
            Debug.Log("[ForcedAppEndController] 경과 시간 타이머가 이미 시작되어 기존 기준 시간을 유지합니다.");
            return;
        }

        elapsedTimerStarted = true;
        elapsedTimerStartTime = Time.time;

        Debug.Log(
            "[ForcedAppEndController] 경과 시간 타이머 시작. 기준 시간: "
            + elapsedTimerStartTime
            + " / 종료까지: "
            + endAfterSeconds
            + "초"
        );
    }

    public void ShowRestartButton()
    {
        if (hasTriggered)
        {
            return;
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButtonVisible = true;
        }
    }

    public void HideRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButtonVisible = false;
        }
    }

    public bool IsForcedEndTimeReached()
    {
        if (!elapsedTimerStarted)
        {
            return false;
        }

        float elapsed = Time.time - elapsedTimerStartTime;
        return elapsed >= endAfterSeconds;
    }

    public void RestartBeforeForcedEnd()
    {
        if (hasTriggered)
        {
            return;
        }

        if (IsForcedEndTimeReached())
        {
            StartForcedEnd();
            return;
        }

        Debug.Log("[ForcedAppEndController] 3분 30초 전이므로 처음으로 돌아갑니다.");

        StopKnownSequences();
        DisableInputAndDetection();
        HideInteractionGuides();

        if (finalSequenceController != null)
        {
            finalSequenceController.ResetFinalStateForRestart();
        }

        if (finalDoorUnlockSequence != null)
        {
            finalDoorUnlockSequence.ResetFinalDoorStateForRestart();
        }

        if (iRoomSequenceManager != null)
        {
            iRoomSequenceManager.ResetToBeginningForRestart();
        }

        EnableInputAndDetection();

        HideRestartButton();

        // 시작 버튼 화면이 아니라 AR 인식 흐름으로 복귀
        if (roomStepController != null)
        {
            roomStepController.ResetToScanGuideForRestart();
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
        HideRestartButton();

        // FinalSequenceController는 직접 엔딩 연출을 해야 하므로 여기서 Stop하지 않음
        if (iRoomSequenceManager != null)
        {
            iRoomSequenceManager.StopAllCoroutines();
        }

        if (roomStepController != null)
        {
            roomStepController.StopAllCoroutines();
        }

        if (finalDoorUnlockSequence != null)
        {
            finalDoorUnlockSequence.StopAllCoroutines();
        }

        DisableInputAndDetection();
        HideInteractionGuides();

        if (finalSequenceController != null)
        {
            finalSequenceController.StartForcedOutsideSoundEnding();
            yield break;
        }

        // fallback: FinalSequenceController가 없을 때만 기존 방식
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

    private void EnableInputAndDetection()
    {
        if (voiceInput != null)
        {
            voiceInput.enabled = true;
        }

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.enabled = true;
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
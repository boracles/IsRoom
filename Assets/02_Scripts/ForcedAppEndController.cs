using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class ForcedAppEndController : MonoBehaviour
{
    [Header("Forced End Time")]
    public int targetHour = 17;
    public int targetMinute = 15;
    public int targetSecond = 0;

    [Tooltip("앱을 17:15 이후에 켰을 때도 바로 종료할지 여부")]
    public bool triggerImmediatelyIfPastTime = true;

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
    private DateTime todayTargetTime;

    private void Start()
    {
        DateTime now = DateTime.Now;

        todayTargetTime = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            targetHour,
            targetMinute,
            targetSecond
        );

        if (forcedEndPanel != null)
        {
            forcedEndPanel.SetActive(false);
        }

        Debug.Log($"[ForcedAppEndController] 강제 종료 목표 시각: {todayTargetTime:yyyy-MM-dd HH:mm:ss}");
    }

    private void Update()
    {
        if (hasTriggered) return;

        DateTime now = DateTime.Now;

        if (now >= todayTargetTime)
        {
            if (!triggerImmediatelyIfPastTime)
            {
                // 앱을 이미 목표 시각 이후에 켰고, 즉시 종료하지 않도록 설정한 경우
                return;
            }

            StartForcedEnd();
        }
    }

    [ContextMenu("Test Forced End Now")]
    public void StartForcedEnd()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        Debug.Log("[ForcedAppEndController] 17:15 강제 종료 시퀀스 시작.");

        StartCoroutine(ForcedEndRoutine());
    }

    private IEnumerator ForcedEndRoutine()
    {
        // 1. 다른 진행 코루틴 정지
        StopKnownSequences();

        // 2. 입력 / 인식 비활성화
        DisableInputAndDetection();

        // 3. 불필요한 가이드 UI 끄기
        HideInteractionGuides();

        // 4. 종료 안내 표시
        ShowForcedEndMessage();

        // 5. 모든 오디오 페이드아웃
        yield return StartCoroutine(FadeOutAllAudio());

        // 6. 안내 문구 잠깐 유지
        yield return new WaitForSeconds(messageShowTime);

        // 7. 앱 종료
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
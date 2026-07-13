using System.Collections;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FinalSequenceController : MonoBehaviour
{
    [Header("Timing")]
    public float completedMusicListenTime = 40f;
    public float finalMessageTime = 3f;
    public float letterShowTime = 2f;
    public float disappearTime = 3f;
    public float endingMessageTime = 3f;

    [Header("Final Outside Sound Guide")]
    public AudioSource guideVoiceSource;
    public AudioClip outsideSoundGuideClip;

    [Header("Final Transition Music")]
    public AudioSource transitionMusicSource;
    public AudioClip transitionToLivePianoClip;

    [Header("Final Completed Music")]
    public AudioSource finalCompletedMusicSource;
    public AudioClip finalCompletedMusicClip;
    public bool playFinalCompletedMusic = true;
    public bool skipCompletedMusicListenTime = true;
    public int finalCompletedMusicLoopCount = 2;

    [Header("Forced End / Restart")]
    public ForcedAppEndController forcedAppEndController;
    public bool waitForForcedEndAfterFinalClose = true;

    [Header("UI")]
    public GameObject finalMessagePanel;
    public TMP_Text finalMessageText;

    [TextArea]
    public string finalMessage = "흩어져 있던 조각들이 하나의 편지가 되었습니다.";

    [TextArea]
    public string endingMessage = "이제 화면 밖의 소리를 들어볼 시간이에요. 휴대폰을 내려주세요.";

    [Header("Completed Letter")]
    public GameObject completedLetterPrefab;
    public Transform completedLetterSpawnPoint;
    public Transform performerHoldPoint;

    [Header("Final Door Close")]
    public FinalDoorUnlockSequence finalDoorUnlockSequence;

    [Header("Audio Fade Out")]
    public float audioFadeOutTime = 2f;

    [System.Serializable]
    public class EmissionTarget
    {
        public Renderer renderer;
        public int materialIndex = 0;
    }

    [Header("Emission Off")]
    public EmissionTarget[] emissionTargets;
    public string emissionPropertyName = "_EmissionColor";
    public float emissionFadeTime = 2f;

    [Header("Room Disappear")]
    public GameObject roomRoot;

    [Header("Performer I")]
    public AIPerformerI performerI;
    public float performerFadeOutTime = 4f;

    [Header("Performer Disappear")]
    public Camera arCamera;
    public float performerDisappearDistance = 1.5f;

    private bool isRunning;
    private GameObject spawnedLetter;

    private void Awake()
    {
        PrepareAudioSource(finalCompletedMusicSource);
        PrepareAudioSource(transitionMusicSource);
        PrepareAudioSource(guideVoiceSource);
    }

    private void PrepareAudioSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.Stop();
    }

    public void StartFinalSequence()
    {
        if (isRunning)
        {
            Debug.Log("[FinalSequenceController] 이미 FinalSequence가 실행 중이라 중복 호출을 무시합니다.");
            return;
        }

        Debug.Log("[FinalSequenceController] StartFinalSequence 호출됨. 이제 final completed music 시퀀스를 시작합니다.");

        StartCoroutine(FinalRoutine());
    }

    private IEnumerator FinalRoutine()
    {
        isRunning = true;

        if (playFinalCompletedMusic)
        {
            yield return StartCoroutine(PlayFinalCompletedMusicRoutine());
        }
        else if (!skipCompletedMusicListenTime)
        {
            yield return new WaitForSeconds(completedMusicListenTime);
        }

        // 2. 마무리 멘트
        ShowMessage(finalMessage);
        yield return new WaitForSeconds(finalMessageTime);

        // 3. 완성된 편지 프리팹 등장
        SpawnCompletedLetter();

        // 편지가 등장하는 순간 턴테이블 circle 회전 정지
        if (finalDoorUnlockSequence != null)
        {
            finalDoorUnlockSequence.StopCircleRotation();
        }

        // 편지가 등장하는 순간 연주 오디오 정지 / 페이드아웃
        yield return StartCoroutine(FadeOutAllAudio());

        yield return new WaitForSeconds(letterShowTime);

        // 4. 뚜껑 + 벽 + 문 닫힘
        if (finalDoorUnlockSequence != null)
        {
            yield return StartCoroutine(finalDoorUnlockSequence.CloseFinalRoomRoutine());
        }
        else
        {
            Debug.LogWarning("[FinalSequenceController] finalDoorUnlockSequence가 연결되지 않았습니다.");
        }

        // 문/벽/뚜껑이 완전히 닫힌 뒤에만 처음으로 돌아가기 버튼 표시
        if (forcedAppEndController != null && !forcedAppEndController.IsForcedEndTimeReached())
        {
            forcedAppEndController.ShowRestartButton();
        }

        if (waitForForcedEndAfterFinalClose)
        {
            Debug.Log("[FinalSequenceController] Final close 완료. 강제 종료 타이머를 기다립니다.");
            yield break;
        }

        // 7. 벽 emission off
        yield return StartCoroutine(FadeEmissionOff());

        // 8. 퍼포머 아이가 편지 위치로 이동
        if (performerI != null)
        {
            performerI.MoveToFinalLetterPosition();
            yield return new WaitUntil(() => performerI.HasArrivedAtCurrentTarget(0.05f));
        }

        // 9. 편지를 퍼포머 아이에게 붙임
        if (performerI != null && spawnedLetter != null && performerHoldPoint != null)
        {
            performerI.AttachObjectToPerformer(spawnedLetter.transform, performerHoldPoint);
        }

        yield return new WaitForSeconds(0.8f);

        // 10. 방 사라짐
        yield return StartCoroutine(FadeRoomOut());

        // 11. 퍼포머 아이가 카메라로부터 멀어지면서 사라짐
        if (performerI != null)
        {
            performerI.MoveAwayFromCamera(arCamera, performerDisappearDistance);

            // 멀어지는 움직임이 보일 때까지 기다림
            yield return new WaitUntil(() => performerI.HasArrivedAtCurrentTarget(0.05f));

            // 완전히 도착한 뒤 잠깐 머무름
            yield return new WaitForSeconds(0.8f);

            // 그다음 천천히 페이드아웃
            performerI.FadeOut(performerFadeOutTime);

            yield return new WaitForSeconds(performerFadeOutTime);
        }

        // 12. 엔딩 멘트
        ShowMessage(endingMessage);
        PlayGuideVoiceClip(outsideSoundGuideClip);

        yield return null;

        // 12-1. “아이패드 내려주세요” 안내 음성이 끝날 때까지 대기
        if (outsideSoundGuideClip != null && guideVoiceSource != null)
        {
            yield return new WaitWhile(() => guideVoiceSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(endingMessageTime);
        }

        // 12-2. AR 사운드에서 현실 피아노/조명으로 넘어가는 연결음 재생
        PlayTransitionMusic();

        yield return null;

        // 연결음이 끝날 때까지 앱 종료하지 않음
        if (transitionToLivePianoClip != null && transitionMusicSource != null)
        {
            yield return new WaitWhile(() => transitionMusicSource.isPlaying);
        }

        // 13. 앱 종료
        QuitApplication();
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
            Debug.LogWarning("[FinalSequenceController] 씬에서 AudioSource를 찾지 못했습니다.");
            yield break;
        }

        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null) continue;
            if (sources[i] == guideVoiceSource) continue;
            if (sources[i] == transitionMusicSource) continue;

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
                if (sources[i] == guideVoiceSource) continue;
                if (sources[i] == transitionMusicSource) continue;

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
            if (sources[i] == guideVoiceSource) continue;
            if (sources[i] == transitionMusicSource) continue;

            sources[i].volume = 0f;
            sources[i].Stop();
        }

        Debug.Log($"[FinalSequenceController] 씬 전체 AudioSource {sources.Length}개 페이드아웃 후 정지.");
    }    

    private void ShowMessage(string message)
    {
        if (finalMessagePanel != null)
            finalMessagePanel.SetActive(true);

        if (finalMessageText != null)
            finalMessageText.text = message;
    }

    private void PlayGuideVoiceClip(AudioClip clip)
    {
        if (clip == null || guideVoiceSource == null)
        {
            return;
        }

        guideVoiceSource.Stop();
        guideVoiceSource.PlayOneShot(clip);
    }

    private void PlayTransitionMusic()
    {
        if (transitionToLivePianoClip == null || transitionMusicSource == null)
        {
            return;
        }

        transitionMusicSource.Stop();
        transitionMusicSource.clip = transitionToLivePianoClip;
        transitionMusicSource.loop = false;
        transitionMusicSource.volume = 1f;
        transitionMusicSource.Play();
    }

    private IEnumerator PlayFinalCompletedMusicRoutine()
    {
        if (finalCompletedMusicSource == null || finalCompletedMusicClip == null)
        {
            Debug.LogWarning("[FinalSequenceController] Final Completed Music Source 또는 Clip이 없습니다.");

            if (!skipCompletedMusicListenTime)
            {
                yield return new WaitForSeconds(completedMusicListenTime);
            }

            yield break;
        }

        int loopCount = Mathf.Max(1, finalCompletedMusicLoopCount);
        float totalPlayTime = finalCompletedMusicClip.length * loopCount;

        finalCompletedMusicSource.Stop();
        finalCompletedMusicSource.clip = finalCompletedMusicClip;
        finalCompletedMusicSource.playOnAwake = false;
        finalCompletedMusicSource.loop = true;
        finalCompletedMusicSource.volume = 1f;
        finalCompletedMusicSource.time = 0f;
        finalCompletedMusicSource.Play();

        Debug.Log(
            "[FinalSequenceController] Final Completed Music loop 재생 시작: "
            + finalCompletedMusicClip.name
            + " / clip length: "
            + finalCompletedMusicClip.length.ToString("F2")
            + "s / loop count: "
            + loopCount
            + " / total: "
            + totalPlayTime.ToString("F2")
            + "s"
        );

        yield return new WaitForSeconds(totalPlayTime);

        finalCompletedMusicSource.Stop();
        finalCompletedMusicSource.loop = false;
        finalCompletedMusicSource.time = 0f;

        Debug.Log("[FinalSequenceController] Final Completed Music loop 재생 완료.");
    }

    private void SpawnCompletedLetter()
    {
        if (completedLetterPrefab == null || completedLetterSpawnPoint == null)
        {
            Debug.LogWarning("[FinalSequenceController] completedLetterPrefab 또는 spawnPoint가 없습니다.");
            return;
        }

        spawnedLetter = Instantiate(
            completedLetterPrefab,
            completedLetterSpawnPoint.position,
            completedLetterSpawnPoint.rotation
        );

        spawnedLetter.transform.localScale = completedLetterSpawnPoint.localScale;
    }

    private IEnumerator FadeEmissionOff()
    {
        if (emissionTargets == null || emissionTargets.Length == 0)
            yield break;

        Material[] materials = new Material[emissionTargets.Length];
        Color[] startColors = new Color[emissionTargets.Length];

        for (int i = 0; i < emissionTargets.Length; i++)
        {
            if (emissionTargets[i] == null) continue;
            if (emissionTargets[i].renderer == null) continue;

            Material[] mats = emissionTargets[i].renderer.materials;
            int index = emissionTargets[i].materialIndex;

            if (index < 0 || index >= mats.Length)
            {
                Debug.LogWarning($"[FinalSequenceController] Emission material index가 범위를 벗어났습니다: {emissionTargets[i].renderer.name}, index {index}");
                continue;
            }

            materials[i] = mats[index];

            if (materials[i].HasProperty(emissionPropertyName))
            {
                startColors[i] = materials[i].GetColor(emissionPropertyName);
            }
            else
            {
                Debug.LogWarning($"[FinalSequenceController] {materials[i].name}에 {emissionPropertyName} 프로퍼티가 없습니다.");
            }
        }

        float elapsed = 0f;

        while (elapsed < emissionFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / emissionFadeTime);

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                if (!materials[i].HasProperty(emissionPropertyName)) continue;

                Color c = Color.Lerp(startColors[i], Color.black, t);
                materials[i].SetColor(emissionPropertyName, c);
            }

            yield return null;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            if (!materials[i].HasProperty(emissionPropertyName)) continue;

            materials[i].SetColor(emissionPropertyName, Color.black);
            materials[i].DisableKeyword("_EMISSION");
        }
    }

    private IEnumerator FadeRoomOut()
    {
        // Transparent 머티리얼이 아니어도 확실하게 사라지게 하는 방식
        yield return new WaitForSeconds(disappearTime);

        if (roomRoot != null)
        {
            roomRoot.SetActive(false);
            Debug.Log("[FinalSequenceController] Room Root 비활성화 완료.");
        }
    }

    public void ResetFinalStateForRestart()
    {
        StopAllCoroutines();

        isRunning = false;

        if (spawnedLetter != null)
        {
            Destroy(spawnedLetter);
            spawnedLetter = null;
        }

        if (finalCompletedMusicSource != null)
        {
            finalCompletedMusicSource.Stop();
            finalCompletedMusicSource.loop = false;
            finalCompletedMusicSource.time = 0f;
        }

        if (transitionMusicSource != null)
        {
            transitionMusicSource.Stop();
            transitionMusicSource.loop = false;
            transitionMusicSource.time = 0f;
        }

        if (guideVoiceSource != null)
        {
            guideVoiceSource.Stop();
        }

        if (finalMessagePanel != null)
        {
            finalMessagePanel.SetActive(false);
        }

        if (finalMessageText != null)
        {
            finalMessageText.text = "";
        }

        Debug.Log("[FinalSequenceController] 처음으로 돌아가기: Final 상태 초기화 완료.");
    }

    public void StartForcedOutsideSoundEnding()
    {
        StopAllCoroutines();
        StartCoroutine(ForcedOutsideSoundEndingRoutine());
    }

    private IEnumerator ForcedOutsideSoundEndingRoutine()
    {
        isRunning = true;

        // 혹시 final 음악이 재생 중이면 정지
        if (finalCompletedMusicSource != null)
        {
            finalCompletedMusicSource.Stop();
            finalCompletedMusicSource.loop = false;
            finalCompletedMusicSource.time = 0f;
        }

        // 완성 편지가 아직 없으면 생성
        if (spawnedLetter == null)
        {
            SpawnCompletedLetter();
        }

        // 편지를 I에게 붙임
        if (performerI != null && spawnedLetter != null && performerHoldPoint != null)
        {
            performerI.AttachObjectToPerformer(spawnedLetter.transform, performerHoldPoint);
        }

        // 안내 메시지/음성
        ShowMessage(endingMessage);
        PlayGuideVoiceClip(outsideSoundGuideClip);

        yield return null;

        if (outsideSoundGuideClip != null && guideVoiceSource != null)
        {
            yield return new WaitWhile(() => guideVoiceSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(endingMessageTime);
        }

        // 방/AR 오브젝트 숨김
        if (roomRoot != null)
        {
            roomRoot.SetActive(false);
            Debug.Log("[FinalSequenceController] Forced Ending: Room Root 비활성화.");
        }

        // I가 카메라에서 멀어지며 사라짐
        if (performerI != null)
        {
            performerI.MoveAwayFromCamera(arCamera, performerDisappearDistance);

            yield return new WaitUntil(() => performerI.HasArrivedAtCurrentTarget(0.05f));

            yield return new WaitForSeconds(0.5f);

            performerI.FadeOut(performerFadeOutTime);

            yield return new WaitForSeconds(performerFadeOutTime);
        }

        // 전환음 재생
        PlayTransitionMusic();

        yield return null;

        if (transitionToLivePianoClip != null && transitionMusicSource != null)
        {
            yield return new WaitWhile(() => transitionMusicSource.isPlaying);
        }

        QuitApplication();
    }

    private void QuitApplication()
    {
    #if UNITY_EDITOR
        Debug.Log("[FinalSequenceController] 앱 종료 지점입니다. Editor Play Mode를 종료합니다.");
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
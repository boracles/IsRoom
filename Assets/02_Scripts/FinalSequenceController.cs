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

    public void StartFinalSequence()
    {
        if (isRunning) return;
        StartCoroutine(FinalRoutine());
    }

    private IEnumerator FinalRoutine()
    {
        isRunning = true;

        // 1. 마지막 레이어가 올라간 뒤 완성 음악 40초 감상
        yield return new WaitForSeconds(completedMusicListenTime);

        // 2. 마무리 멘트
        ShowMessage(finalMessage);
        yield return new WaitForSeconds(finalMessageTime);

        // 3. 완성된 편지 프리팹 등장
        SpawnCompletedLetter();
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

        // 6. 음악 페이드아웃
        yield return StartCoroutine(FadeOutAllAudio());

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

        if (outsideSoundGuideClip != null && guideVoiceSource != null)
        {
            yield return new WaitWhile(() => guideVoiceSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(endingMessageTime);
        }

        // 혹시 너무 바로 꺼지는 느낌이면 아주 짧게 여운
        yield return new WaitForSeconds(0.3f);

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
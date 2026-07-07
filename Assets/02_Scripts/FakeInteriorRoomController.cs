using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class FakeInteriorRoomController : MonoBehaviour
{
    [System.Serializable]
    public class TargetMaterialSlot
    {
        public Renderer renderer;

        [Tooltip("MeshRenderer Materials 배열에서 Interior_LightRoom이 들어간 Element 번호")]
        public int materialIndex = 0;
    }

    [Header("Room Space")]
    public Transform roomAnchor;

    [Tooltip("Fake room width in RoomAnchor local X.")]
    public float roomWidth = 3.0f;

    [Tooltip("Fake room height in RoomAnchor local Y.")]
    public float roomHeight = 2.7f;

    [Tooltip("Fake room depth in RoomAnchor local Z. Front is Z=0, inside is +Z.")]
    public float roomDepth = 3.0f;

    [Header("Target Material Slots")]
    public TargetMaterialSlot[] targetMaterialSlots;

    [Header("Random EXR Textures")]
    public Texture2D[] interiorTextures;

    [Header("Reveal")]
    public bool startBlack = true;
    public float fadeDuration = 1.5f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Material Values")]
    public Color tint = Color.white;
    public float finalExposure = 1.0f;

    private static readonly int WorldToRoomID = Shader.PropertyToID("_WorldToRoom");
    private static readonly int RoomMinID = Shader.PropertyToID("_RoomMin");
    private static readonly int RoomMaxID = Shader.PropertyToID("_RoomMax");
    private static readonly int InteriorTexID = Shader.PropertyToID("_InteriorTex");
    private static readonly int ExposureID = Shader.PropertyToID("_Exposure");
    private static readonly int TintID = Shader.PropertyToID("_Tint");

    private MaterialPropertyBlock propertyBlock;
    private Texture2D currentTexture;
    private float currentExposure;
    private Coroutine revealCoroutine;

    private void OnEnable()
    {
        currentExposure = startBlack ? 0f : finalExposure;
        Apply();
    }

    private void OnValidate()
    {
        roomWidth = Mathf.Max(0.01f, roomWidth);
        roomHeight = Mathf.Max(0.01f, roomHeight);
        roomDepth = Mathf.Max(0.01f, roomDepth);
        fadeDuration = Mathf.Max(0.01f, fadeDuration);
        finalExposure = Mathf.Max(0f, finalExposure);

        // 여기서 exposure를 매번 0으로 되돌리지 않는다.
        // 인스펙터 값을 바꿔도 현재 상태를 유지하게 하기 위함.
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    public void RevealRandomInterior()
    {
        if (interiorTextures == null || interiorTextures.Length == 0)
        {
            Debug.LogWarning("[FakeInteriorRoomController] Interior Textures가 비어 있습니다.");
            return;
        }

        int index = Random.Range(0, interiorTextures.Length);
        currentTexture = interiorTextures[index];

        Debug.Log("[FakeInteriorRoomController] Random interior selected: " + currentTexture.name);

        if (Application.isPlaying)
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
            }

            revealCoroutine = StartCoroutine(RevealRoutine());
        }
        else
        {
            currentExposure = finalExposure;
            Apply();
        }
    }

    public void SetBlack()
    {
        currentExposure = 0f;
        Apply();
    }

    public void ShowCurrentImmediately()
    {
        if (currentTexture == null)
        {
            PickRandomTextureOnly();
        }

        currentExposure = finalExposure;
        Apply();
    }

    public void PickRandomTextureOnly()
    {
        if (interiorTextures == null || interiorTextures.Length == 0)
        {
            Debug.LogWarning("[FakeInteriorRoomController] Interior Textures가 비어 있습니다.");
            return;
        }

        int index = Random.Range(0, interiorTextures.Length);
        currentTexture = interiorTextures[index];

        Debug.Log("[FakeInteriorRoomController] Texture picked only: " + currentTexture.name);

        Apply();
    }

    [ContextMenu("TEST / Reveal Random Interior")]
    private void TestRevealRandomInterior()
    {
        RevealRandomInterior();
    }

    [ContextMenu("TEST / Set Black")]
    private void TestSetBlack()
    {
        SetBlack();
    }

    [ContextMenu("TEST / Pick Random Texture Only")]
    private void TestPickRandomTextureOnly()
    {
        PickRandomTextureOnly();
    }

    [ContextMenu("TEST / Show Current Immediately")]
    private void TestShowCurrentImmediately()
    {
        ShowCurrentImmediately();
    }

    private IEnumerator RevealRoutine()
    {
        currentExposure = 0f;
        Apply();

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curved = fadeCurve.Evaluate(t);

            currentExposure = Mathf.Lerp(0f, finalExposure, curved);
            Apply();

            yield return null;
        }

        currentExposure = finalExposure;
        Apply();

        revealCoroutine = null;
    }

    private void Apply()
    {
        if (roomAnchor == null)
        {
            roomAnchor = transform;
        }

        if (targetMaterialSlots == null || targetMaterialSlots.Length == 0)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Matrix4x4 worldToRoom = roomAnchor.worldToLocalMatrix;

        Vector4 roomMin = new Vector4(
            -roomWidth * 0.5f,
            -roomHeight * 0.5f,
            0f,
            0f
        );

        Vector4 roomMax = new Vector4(
            roomWidth * 0.5f,
            roomHeight * 0.5f,
            roomDepth,
            0f
        );

        foreach (TargetMaterialSlot target in targetMaterialSlots)
        {
            if (target == null || target.renderer == null)
            {
                continue;
            }

            int materialCount = target.renderer.sharedMaterials.Length;

            if (target.materialIndex < 0 || target.materialIndex >= materialCount)
            {
                Debug.LogWarning(
                    "[FakeInteriorRoomController] Material index가 Renderer의 material 개수 범위를 벗어났습니다: "
                    + target.renderer.name
                );
                continue;
            }

            target.renderer.GetPropertyBlock(propertyBlock, target.materialIndex);

            propertyBlock.SetMatrix(WorldToRoomID, worldToRoom);
            propertyBlock.SetVector(RoomMinID, roomMin);
            propertyBlock.SetVector(RoomMaxID, roomMax);
            propertyBlock.SetFloat(ExposureID, currentExposure);
            propertyBlock.SetColor(TintID, tint);

            if (currentTexture != null)
            {
                propertyBlock.SetTexture(InteriorTexID, currentTexture);
            }

            target.renderer.SetPropertyBlock(propertyBlock, target.materialIndex);
        }
    }
}
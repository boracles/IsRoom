using System.Collections;
using System.Collections.Generic;
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

    [Header("Debug")]
    [SerializeField] private int debugCurrentTextureIndex = -1;
    [SerializeField] private string debugCurrentTextureName = "";
    [SerializeField] private int debugRemainingBagCount = 0;
    [SerializeField] private int debugValidTextureCount = 0;
    [SerializeField] private int debugRandomSeed = 0;

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

    private List<int> textureBag = new List<int>();
    private int lastTextureIndex = -1;
    private int sequentialTextureIndex = -1;

    private System.Random systemRandom;

    private void OnEnable()
    {
        EnsureRandomReady();

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

        // 인스펙터에서 Texture 배열을 수정했을 가능성이 있으므로 다음 랜덤 라운드는 다시 섞도록 비움.
        if (textureBag != null)
        {
            textureBag.Clear();
        }

        debugRemainingBagCount = 0;
        debugValidTextureCount = CountValidTextures();

        Apply();
    }

    private void Update()
    {
        Apply();
    }

    public void RevealRandomInterior()
    {
        int index = GetNextTextureIndex();

        if (!IsValidTextureIndex(index))
        {
            Debug.LogWarning("[FakeInteriorRoomController] 선택 가능한 Interior Texture가 없습니다.");
            return;
        }

        SetCurrentTexture(index);

        Debug.Log(
            "[FakeInteriorRoomController] Random interior selected: index "
            + index
            + " / "
            + currentTexture.name
        );

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
        int index = GetNextTextureIndex();

        if (!IsValidTextureIndex(index))
        {
            Debug.LogWarning("[FakeInteriorRoomController] 선택 가능한 Interior Texture가 없습니다.");
            return;
        }

        SetCurrentTexture(index);

        Debug.Log(
            "[FakeInteriorRoomController] Texture picked only: index "
            + index
            + " / "
            + currentTexture.name
        );

        currentExposure = finalExposure;
        Apply();
    }

    public void PickNextTextureOnly()
    {
        int index = GetNextSequentialTextureIndex();

        if (!IsValidTextureIndex(index))
        {
            Debug.LogWarning("[FakeInteriorRoomController] 선택 가능한 Interior Texture가 없습니다.");
            return;
        }

        SetCurrentTexture(index);

        Debug.Log(
            "[FakeInteriorRoomController] NEXT texture selected: index "
            + index
            + " / "
            + currentTexture.name
        );

        currentExposure = finalExposure;
        Apply();
    }

    public void ResetRandomBag()
    {
        if (textureBag != null)
        {
            textureBag.Clear();
        }

        lastTextureIndex = -1;
        sequentialTextureIndex = -1;

        EnsureRandomReady(true);

        debugCurrentTextureIndex = -1;
        debugCurrentTextureName = "";
        debugRemainingBagCount = 0;
        debugValidTextureCount = CountValidTextures();

        Debug.Log("[FakeInteriorRoomController] Random bag reset.");
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

    [ContextMenu("TEST / Pick Next Texture Only")]
    private void TestPickNextTextureOnly()
    {
        PickNextTextureOnly();
    }

    [ContextMenu("TEST / Show Current Immediately")]
    private void TestShowCurrentImmediately()
    {
        ShowCurrentImmediately();
    }

    [ContextMenu("TEST / Reset Random Bag")]
    private void TestResetRandomBag()
    {
        ResetRandomBag();
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
            float curved = fadeCurve != null ? fadeCurve.Evaluate(t) : t;

            currentExposure = Mathf.Lerp(0f, finalExposure, curved);
            Apply();

            yield return null;
        }

        currentExposure = finalExposure;
        Apply();

        revealCoroutine = null;
    }

    private void EnsureRandomReady(bool forceNewSeed = false)
    {
        if (systemRandom != null && !forceNewSeed)
        {
            return;
        }

        int seed = System.Guid.NewGuid().GetHashCode();
        systemRandom = new System.Random(seed);
        debugRandomSeed = seed;

        Debug.Log("[FakeInteriorRoomController] Random seed: " + seed);
    }

    private int GetNextTextureIndex()
    {
        if (interiorTextures == null || interiorTextures.Length == 0)
        {
            return -1;
        }

        EnsureRandomReady();

        if (textureBag == null)
        {
            textureBag = new List<int>();
        }

        if (textureBag.Count == 0)
        {
            RefillAndShuffleTextureBag();
        }

        if (textureBag.Count == 0)
        {
            Debug.LogWarning("[FakeInteriorRoomController] 유효한 Interior Texture가 없습니다. 배열에 None이 아닌 텍스처를 넣어주세요.");
            return -1;
        }

        int textureIndex = textureBag[0];
        textureBag.RemoveAt(0);

        // 새 라운드 첫 번째가 직전 index와 같으면 뒤로 넘김.
        if (
            textureBag.Count > 0 &&
            lastTextureIndex >= 0 &&
            textureIndex == lastTextureIndex
        )
        {
            textureBag.Add(textureIndex);
            textureIndex = textureBag[0];
            textureBag.RemoveAt(0);
        }

        lastTextureIndex = textureIndex;
        debugRemainingBagCount = textureBag.Count;

        Debug.Log(
            "[FakeInteriorRoomController] Picked index: "
            + textureIndex
            + " / "
            + interiorTextures[textureIndex].name
            + " / Remaining: "
            + textureBag.Count
        );

        return textureIndex;
    }

    private void RefillAndShuffleTextureBag()
    {
        textureBag.Clear();

        for (int i = 0; i < interiorTextures.Length; i++)
        {
            if (interiorTextures[i] != null)
            {
                textureBag.Add(i);
            }
        }

        debugValidTextureCount = textureBag.Count;

        if (textureBag.Count == 0)
        {
            return;
        }

        // Fisher-Yates Shuffle
        for (int i = textureBag.Count - 1; i > 0; i--)
        {
            int j = systemRandom.Next(0, i + 1);

            int temp = textureBag[i];
            textureBag[i] = textureBag[j];
            textureBag[j] = temp;
        }

        // 새 라운드의 첫 번째가 직전 텍스처와 같으면 다른 위치와 교환.
        if (textureBag.Count > 1 && lastTextureIndex >= 0 && textureBag[0] == lastTextureIndex)
        {
            int swapIndex = systemRandom.Next(1, textureBag.Count);

            int temp = textureBag[0];
            textureBag[0] = textureBag[swapIndex];
            textureBag[swapIndex] = temp;
        }

        debugRemainingBagCount = textureBag.Count;

        Debug.Log(
            "[FakeInteriorRoomController] Texture bag shuffled. Count: "
            + textureBag.Count
        );
    }

    private int GetNextSequentialTextureIndex()
    {
        if (interiorTextures == null || interiorTextures.Length == 0)
        {
            return -1;
        }

        int safety = 0;

        do
        {
            sequentialTextureIndex++;

            if (sequentialTextureIndex >= interiorTextures.Length)
            {
                sequentialTextureIndex = 0;
            }

            safety++;

            if (safety > interiorTextures.Length + 5)
            {
                return -1;
            }

        } while (interiorTextures[sequentialTextureIndex] == null);

        return sequentialTextureIndex;
    }

    private bool IsValidTextureIndex(int index)
    {
        return interiorTextures != null &&
               index >= 0 &&
               index < interiorTextures.Length &&
               interiorTextures[index] != null;
    }

    private int CountValidTextures()
    {
        if (interiorTextures == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < interiorTextures.Length; i++)
        {
            if (interiorTextures[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void SetCurrentTexture(int index)
    {
        currentTexture = interiorTextures[index];

        debugCurrentTextureIndex = index;
        debugCurrentTextureName = currentTexture != null ? currentTexture.name : "";

        Apply();
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

            propertyBlock.Clear();

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
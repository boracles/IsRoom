using UnityEngine;

[ExecuteAlways]
public class FakeInteriorMatrixSetter : MonoBehaviour
{
    [Header("Room Space")]
    public Transform roomAnchor;

    [Tooltip("Fake room width in RoomAnchor local X.")]
    public float roomWidth = 1.0f;

    [Tooltip("Fake room height in RoomAnchor local Y.")]
    public float roomHeight = 0.8f;

    [Tooltip("Fake room depth in RoomAnchor local Z. Front is Z=0, inside is +Z.")]
    public float roomDepth = 1.2f;

    [Header("Target Renderers")]
    public Renderer[] targetRenderers;

    private static readonly int WorldToRoomID = Shader.PropertyToID("_WorldToRoom");
    private static readonly int RoomMinID = Shader.PropertyToID("_RoomMin");
    private static readonly int RoomMaxID = Shader.PropertyToID("_RoomMax");

    private MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (roomAnchor == null)
        {
            roomAnchor = transform;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Matrix4x4 worldToRoom = roomAnchor.worldToLocalMatrix;

        Vector4 roomMin = new Vector4(
            -roomWidth * 0.5f,
            -roomHeight * 0.5f,
            0.0f,
            0.0f
        );

        Vector4 roomMax = new Vector4(
            roomWidth * 0.5f,
            roomHeight * 0.5f,
            roomDepth,
            0.0f
        );

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetMatrix(WorldToRoomID, worldToRoom);
            propertyBlock.SetVector(RoomMinID, roomMin);
            propertyBlock.SetVector(RoomMaxID, roomMax);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
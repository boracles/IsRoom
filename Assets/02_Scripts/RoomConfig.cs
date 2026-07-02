using UnityEngine;

[CreateAssetMenu(menuName = "I Room/Room Config")]
public class RoomConfig : ScriptableObject
{
    [Header("Room")]
    public RoomType roomType;

    [TextArea]
    public string questionText;

    [Header("Audio")]
    public AudioClip firstToneClip;
    public AudioClip questionVoiceClip;
    public AudioClip releaseClip;
    public AudioClip createPieceClip;

    [Header("Piece")]
    public GameObject[] piecePrefabs;

    [Header("Spawn")]
    public Transform pieceSpawnPoint;

    [Header("I Visual")]
    public Color iParticleColor = Color.yellow;
}
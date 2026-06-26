using UnityEngine;

namespace GrandpasAtticDemo
{
    public class CrouchArea : MonoBehaviour
    {
        FPSControllerSimple player;

        // Start is called before the first frame update
        void Start()
        {
            player = FindObjectOfType<FPSControllerSimple>();
        }

        private void OnTriggerEnter(Collider other)
        {
            player.canStandUp = false;
        }

        private void OnTriggerExit(Collider other)
        {
            player.canStandUp = true;
        }
    }
}
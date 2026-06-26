using UnityEngine;

namespace GrandpasAtticDemo
{
    public class CursorLock : MonoBehaviour {

        public bool lockCursorInPlayMode;
        public bool hideCursorInPlayMode;


        void Start () {
            if (lockCursorInPlayMode)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;

            if (hideCursorInPlayMode)
                Cursor.visible = false;
            else
                Cursor.visible = true;

        }
    }
}
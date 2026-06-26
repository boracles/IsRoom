using UnityEngine;

namespace GrandpasAtticDemo
{
    public class FPSControllerSimple : MonoBehaviour {
    [Header ("Speed Settings")]
    public float speed;
    public float walkSpeedSlow= 1.5f;
    public float walkSpeedFast = 3f;
    private float crouchSpeed;

    [Header ("Player Abilities (for debug)")]
    public bool canWalkFaster = true;
    public bool canCrouch = true;
    public bool canStandUp = true;
    public bool lockMouseMovement;

    [Header("Camera & View Settings")]
    public GameObject cameraParent;
    public GameObject defaultCamera;
    public GameObject zoomCamera;
    public float sensitivity;
    public float defaultSensitivity = 2f;
    float zoomSensitivity;
    CharacterController player;
    [Range(1,100)]
    public float mouseSnappiness = 20f;

    float moveVertical, moveHorizontal, rotX, rotY, mouseXVelocity, mouseYVelocity;
    [HideInInspector]
    public float vertVelocity;
    
    //crouching
    private bool isCrouching;
    private float target_height;
    private float standingHeight = 1.65f;
    private float crouchHeight = 0.75f;
    private float previous_y = 0;

    void Start () {
        player = GetComponent<CharacterController>();
        speed = walkSpeedSlow;
        sensitivity = defaultSensitivity;
        zoomSensitivity = defaultSensitivity / 3;
        target_height = standingHeight;
        crouchSpeed = walkSpeedSlow / 2;
	}
	
	void Update () {
        ApplyGravity();
        Movement();

        Rotation();

        //walk faster
        if (canWalkFaster)
        {

            if (Input.GetKeyDown(KeyCode.LeftShift) /*|| Input.GetButtonDown("RunXboxController")*/)
            {
                speed = walkSpeedFast;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift) /*|| Input.GetButtonUp("RunXboxController")*/)
            {
                speed = walkSpeedSlow;
            }
        }

        //crouching
        if (canCrouch)
        {
            previous_y = player.transform.position.y - player.height / 2 - player.skinWidth;
            //previous_y = 0f;
            if (Input.GetKeyDown(KeyCode.C) /*|| Input.GetButtonDown("CrouchXboxController")*/)
            {
                if (isCrouching == false)
                {
                    isCrouching = true;
                    speed = walkSpeedSlow;
                    canWalkFaster = false;
                    target_height = crouchHeight;
                    speed = crouchSpeed;
                }
                else if(canStandUp)
                {
                    isCrouching = false;
                    canWalkFaster = true;
                    target_height = standingHeight;
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        speed = walkSpeedSlow;
                    }
                    else if (Input.GetKey(KeyCode.LeftShift))
                    {
                        speed = walkSpeedFast;
                    }
                }
            }
            player.height = Mathf.Lerp(player.height, target_height, 5f * Time.deltaTime);
            cameraParent.transform.position = Vector3.Lerp(cameraParent.transform.position, new Vector3(cameraParent.transform.position.x, player.transform.position.y + target_height / 2 - 0.1f, cameraParent.transform.position.z), 5f * Time.deltaTime);
            player.transform.position = Vector3.Lerp(player.transform.position, new Vector3(player.transform.position.x, previous_y + target_height / 2 + player.skinWidth, player.transform.position.z), 5f * Time.deltaTime);
        }

        //zooming
        if (Input.GetMouseButtonDown(1) /*|| Input.GetButtonDown("ZoomXboxController")*/)
        {
            defaultCamera.SetActive(!defaultCamera.activeInHierarchy);
            zoomCamera.SetActive(!zoomCamera.activeInHierarchy);
            sensitivity = zoomSensitivity;
        }
        if (Input.GetMouseButtonUp(1) /*|| Input.GetButtonUp("ZoomXboxController")*/)
        {
            defaultCamera.SetActive(!defaultCamera.activeInHierarchy);
            zoomCamera.SetActive(!zoomCamera.activeInHierarchy);
            sensitivity = defaultSensitivity;
        }
        if (!Input.GetMouseButton(1))
        {
            if (!defaultCamera.activeSelf)
            {
                defaultCamera.SetActive(!defaultCamera.activeInHierarchy);
                zoomCamera.SetActive(!zoomCamera.activeInHierarchy);

            }
            sensitivity = defaultSensitivity;
        }

        //Debug.Log("Speed: " + speed + " | moveVert: " + moveVertical + " | moveHor: " + moveHorizontal);

    }

    void Rotation()
    {
        rotX = Input.GetAxisRaw("Mouse X") * sensitivity;
        rotY -= Input.GetAxisRaw("Mouse Y") * sensitivity;

        mouseXVelocity = Mathf.Lerp(mouseXVelocity, rotX, mouseSnappiness * Time.deltaTime);
        mouseYVelocity = Mathf.Lerp(mouseYVelocity, rotY, mouseSnappiness * Time.deltaTime);

        if (!lockMouseMovement)
        {
            rotY = Mathf.Clamp(rotY, -70f, 70f);
            transform.Rotate(0, mouseXVelocity, 0);
            cameraParent.transform.localRotation = Quaternion.Euler(mouseYVelocity, 0, 0);
        }
    }

    void Movement()
    {
        moveVertical = Input.GetAxisRaw("Vertical");
        moveHorizontal = Input.GetAxisRaw("Horizontal");

        Vector3 movement = new Vector3(moveHorizontal, 0, moveVertical);
        
        // limit diaginal movement
        movement = movement.normalized;

        // add vertical force
        movement = movement + new Vector3(0, vertVelocity, 0);
      
        movement = transform.rotation * movement;
        player.Move(movement * Time.deltaTime * speed);
    }

    private void ApplyGravity()
    {
        if (player.isGrounded)
        {
            vertVelocity = Physics.gravity.y ;
        }
        else
        {
            vertVelocity += Physics.gravity.y;
        }
    }
}

}

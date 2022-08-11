using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	private Rigidbody rb;
	private Camera cam;
	private float speed = 100; // Acceleration force on player, actual speed much slower than this value
	private float maxSpeed = 10;
	private float jumpForce = 8;
	private float mouseSensitivity = 10;
	private float rotLR = 0; // Holds camera rotation
	private float rotUD = 0;
	public bool movementEnabled = true;

	private float hitForce = 100; // Strength of the force applied when the player hits an object
	private float meleeReach = 10; // How far the player can reach with melee attacks

	public bool isGrounded;
	void Start()
	{
		rb = GetComponent<Rigidbody>();
		cam = GetComponentInChildren<Camera>();
		// Lock mouse
		// Cursor.lockState = CursorLockMode.Locked;
	}

	void OnCollisionStay() // When player is on ground (!!! This is allowing a double jump !!!)
	{
		isGrounded = true;
	}

	void Update()
	{
		// While pressing a wasd button, AND movementEnabled, accelerate the player, respecting maxSpeed
		if (movementEnabled && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)))
		{
			// Get keys pressed
			int ww = Input.GetKey(KeyCode.W) ? 1 : 0;
			int aa = Input.GetKey(KeyCode.A) ? 1 : 0;
			int ss = Input.GetKey(KeyCode.S) ? 1 : 0;
			int dd = Input.GetKey(KeyCode.D) ? 1 : 0;

			// Calculate acceleration vector
			float twoWay = ww + aa + ss + dd >= 2 ? 0.707f : 1f;
			float newX = (dd + -aa) * speed * twoWay * Time.deltaTime;
			float newZ = (ww + -ss) * speed * twoWay * Time.deltaTime;
			Vector3 acceleration = new Vector3(newX, 0, newZ) * speed * Time.deltaTime;
			// Rotate acceleration vector based on camera rotation
			acceleration = Quaternion.Euler(0, rotLR, 0) * acceleration;

			// Get current velocity
			Vector3 currentVelocity = rb.velocity;
			// Calculate new velocity, ignoring y component
			Vector3 newVelocity = new Vector3(currentVelocity.x + acceleration.x, 0, currentVelocity.z + acceleration.z);
			// If new velocity is greater than maxSpeed, set it to maxSpeed
			if (newVelocity.magnitude > maxSpeed)
			{
				newVelocity = newVelocity.normalized * maxSpeed;
			}
			// Set new velocity, including y component
			rb.velocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);
		}
		else
		{
			// Apply stopping force if movement is enabled (Meaning stronger knockback if movement is disabled)
			if (movementEnabled)
			{
				rb.velocity = new Vector3(rb.velocity.x * 0.99f, rb.velocity.y, rb.velocity.z * 0.99f);
			}
		}

		// Handle player rotation using mouse movement
		rotLR += Input.GetAxis("Mouse X") * mouseSensitivity;
		rotUD += Input.GetAxis("Mouse Y") * mouseSensitivity;
		rotUD = Mathf.Clamp(rotUD, -90f, 90f);
		cam.transform.localRotation = Quaternion.Euler(-rotUD, rotLR, 0f);

		// Handle Jumping
		if (Input.GetKeyDown(KeyCode.Space) && isGrounded && movementEnabled)
		{
			rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
			isGrounded = false;
		}

		// On left click, check if there is a rigidbody within melee reach of the player, and within a 20 degree cone from the player camera
		// This currently has a few problems: It has trouble with friction when dashing along ground, sometimes it seems to dash directly down, and targets up close are VERY hard to hit (Perhaps if within certian distance just ignore angle check?)
		// Also it should probably give prio to closer targets, but this is not implemented yet.
		if (Input.GetMouseButtonDown(0) && movementEnabled)
		{
			// Get all rigidbodies within melee reach of the player
			Collider[] hitColliders = Physics.OverlapSphere(cam.transform.position, meleeReach);
			// Loop through all rigidbodies
			foreach (Collider hitCollider in hitColliders)
			{
				// If the rigidbody is not the player, and is a rigidbody, dash towards the rigidbody
				if (hitCollider.gameObject != gameObject && hitCollider.GetComponent<Rigidbody>())
				{
					// Get rigidbody's position
					Vector3 hitPosition = hitCollider.transform.position;
					// Get direction to rigidbody
					Vector3 direction = hitPosition - cam.transform.position;
					// Get angle between direction and camera's forward vector
					float angle = Vector3.Angle(direction, cam.transform.forward);
					Debug.Log("Angle: " + angle);
					// If angle is less than 20 degrees, dash towards the rigidbody
					if (angle < 20)
					{
						// Turn to face rigidbody and dash
						cam.transform.rotation = Quaternion.LookRotation(direction);
						rb.AddForce(direction.normalized * hitForce, ForceMode.Impulse);
						break; // Dashed
					}
				}
			}
		}
	}
}

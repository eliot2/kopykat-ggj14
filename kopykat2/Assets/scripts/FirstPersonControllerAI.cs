﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//[RequireComponent (typeof(characterController))]
//this requires the character controller or compiler errors occur.
public class FirstPersonControllerAI : MonoBehaviour {
	
	private float movementSpeed = 2.001f; 
	private float mouseSensetivity = 10.0f;
	private float rotYSpeed = 1.01f;
	private float upDownRange = 69.0f;
	private float rotUpDown = 0.0f;
	private float jumpSpeed = 2f;
	private float verticalVelocity = 0.0f;
    private float forwardSpeed;
    private float sideSpeed;
    private List<string> states = new List<string>();
    private List<string> stateQueue = new List<string>();
	private float playerDist;
    private bool toJump = false;
    private string newState;
	CharacterController characterController;
	float waitTick = 50f;
	float reqTick;
	float modTick = 50f;
    bool caseState;
    string myState;
	UnityEngine.GameObject playerA;
	UnityEngine.GameObject playerB;
    private Animator animator;

	// Use this for initialization
	void Start () {
        states.Add("standing"); states.Add("walking"); states.Add("jumping"); states.Add("hands up");
		Screen.lockCursor = true;
		characterController = GetComponent<CharacterController>();
		if (!characterController) {
			//freak out
		}
		playerA = GameObject.Find ("PlayerA");
        animator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
        waitTick++;
        caseState = playerA.GetComponent<FirstPersonController>().getStateBool();
        if (caseState == true)
        {
            myState = playerA.GetComponent<FirstPersonController>().getState();
            stateQueue.Add(myState);
            Debug.Log("I am adding the state: " + myState + " to my QUEUE!");

        }
		//Debug.Log(playerA.GetComponent<FirstPersonController> ().getState()[0] + ", " + playerA.GetComponent<FirstPersonController> ().getState()[1] + "TEST2");

		playerDist = Vector3.Distance(GameObject.Find("PlayerA").transform.position, this.transform.position);
		reqTick = playerDist * modTick;
		if(waitTick >= reqTick){
			waitTick = 0f;
            if (stateQueue.Count > 0)
            {
                newState = stateQueue[0];
                stateQueue.RemoveAt(0);
                switch (newState)
                {
                    case "standing":
                        forwardSpeed = 0;
                        sideSpeed = 0;
                        animator.SetBool("isJumping", false);
                        animator.SetBool("isWalking", false);
                        Debug.Log("I am standing!!");
                        return;
                    case "walking":
                        forwardSpeed = Random.Range(-1, 1);
                        sideSpeed = Random.Range(-1, 1);
                        Debug.Log("I am walking!!");
                        animator.SetBool("isWalking", true);
                        animator.SetBool("isJumping", false);
                        return;
                    case "jumping":
                        toJump = true;
                        Debug.Log("I will jump!!");
                        return;
                    case "hands up":
                        return;
                    default:
                        return;
                }
            }

		}


		//player rotation
		//left and right
		/*float rotLeftRight = Input.GetAxis("Mouse X")*mouseSensetivity;
		transform.Rotate(0, rotLeftRight, 0);*/
						

		verticalVelocity += Physics.gravity.y * Time.deltaTime;
        //Debug.Log("IS HE GRUNDED? " + characterController.isGrounded);
		if (characterController.isGrounded && toJump){//Input.GetButtonDown("Jump")){
            Debug.Log("I JUMP!!");
			verticalVelocity = jumpSpeed;
            animator.SetBool("isJumping", true);
		}

		Vector3 speed = new Vector3( sideSpeed*movementSpeed, verticalVelocity*.50f, forwardSpeed*movementSpeed);
		
		//speed = transform.rotation * speed;
		

		characterController.Move( speed * Time.deltaTime);
	}
}

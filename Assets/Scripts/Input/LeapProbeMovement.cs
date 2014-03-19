﻿using UnityEngine;
using System.Collections;

public class LeapProbeMovement : MonoBehaviour {

	/// Singleton object to provide Leap input data.
	private LeapManager _leapManager;

	/// The skin GameObject.
	private GameObject skin;

	/// Use this for initialization
	void Start () {
		_leapManager = (GameObject.Find("LeapManager")as GameObject).GetComponent(typeof(LeapManager)) as LeapManager;
		UltrasoundDebug.Assert (null != _leapManager,
		                        "Could not find LeapManager object. Add a LeapManager prefab to this scene.",
								this);

		skin = GameObject.FindGameObjectWithTag("Skin");
		UltrasoundDebug.Assert(null != skin, 
		                       "Could not find Skin object. Did you forget to tag it?",
		                       this);
		UltrasoundDebug.Assert(null != skin.collider, 
		                       "Skin object does not have a collider.",
		                       this);
	}
	
	/// Update is called once per frame
	void Update () {
		UpdateLeapLocation();
	}

	/// Retrieves location data from the Leap controller, if available, and moves the object to the appropriate
	/// position.
	private void UpdateLeapLocation() {
		if(_leapManager != null) { 
			if(_leapManager.pointerAvailible)
			{
				Vector3 targetPosition = _leapManager.pointerPositionWorld;
				Vector3 direction = targetPosition - transform.position;
				Debug.Log(direction);
				this.rigidbody.AddForce(80 * (direction));
				if(!renderer.enabled) { renderer.enabled = true; }
			}
			else
			{
				renderer.enabled = false;
			}
		}
	}
}

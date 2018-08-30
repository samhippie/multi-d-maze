using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalController : MonoBehaviour {

	public Vector3 angularVelocity = new Vector3(20, 20, 20);

	Rigidbody rb;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		Quaternion dRot = Quaternion.Euler(angularVelocity * Time.deltaTime);
		rb.MoveRotation(rb.rotation * dRot);
	}
}

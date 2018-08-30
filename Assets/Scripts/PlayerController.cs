using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	public class Player {
		public List<float> pos = new List<float>(new float[] {0,0,0,0});
		public List<float> rot = new List<float>(new float[] {1,0,0,0});
		public List<float> up = new List<float>(new float[] {0,1,0,0});
		public bool isPosInvalid = false;
		public int outDimen = 3;


		public void SetOutDimen(int outDimen) {
			this.outDimen = outDimen;
			this.isPosInvalid = true;
			GameManager.instance.UpdateOutDimen(outDimen, (int) player.pos[outDimen]);
		}

		private Vector3 trimToVector3(List<float> vec4) {
			var subVec = new List<float>();
			for(int i = 0; i < 4; i++) {
				if(i != outDimen) {
					subVec.Add(vec4[i]);
				}
			}
			return new Vector3(subVec[0], subVec[1], subVec[2]);
		}

		private void setVector3To4(List<float> vec4, Vector3 vec3) {
			int j = 0;
			for(int i = 0; i < 4; i++) {
				if(i == outDimen) {
					continue;
				}
				vec4[i] = vec3[j];
				j++;
			}
		}

		public Vector3 GetPos() {
			return trimToVector3(pos);
		}

		public void SetPos(Vector3 pos) {
			setVector3To4(this.pos, pos);
		}
		
		public Quaternion GetRot() {
			return Quaternion.LookRotation(trimToVector3(rot), trimToVector3(up));
		}

		public Vector3 GetRotVector() {
			return trimToVector3(rot).normalized;
		}

		public void LookUp(float step) {
			var subRot = trimToVector3(rot).normalized;
			var subUp = trimToVector3(up).normalized;
			var axis = Vector3.Cross(subRot, subUp).normalized;
			var quat = Quaternion.AngleAxis(30 * step, axis);
			subRot = quat * subRot;
			subUp = quat * subUp;
			setVector3To4(rot, subRot);
			setVector3To4(up, subUp);
		}

		public void LookLeft(float step) {
			var subRot = trimToVector3(rot).normalized;
			var subUp = trimToVector3(up).normalized;
			var quat = Quaternion.AngleAxis(30 * step, subUp);
			subRot = quat * subRot;
			setVector3To4(rot, subRot);
		}

		public void RollCW(float step) {
			var subRot = trimToVector3(rot).normalized;
			var subUp = trimToVector3(up).normalized;
			var quat = Quaternion.AngleAxis(20 * step, subRot);
			subUp = quat * subUp;
			setVector3To4(up, subUp);
		}
	}

	private Rigidbody rb;
	public float speed;
	public float smooth;
	public static Player player;
	private bool focus = false;
	private bool hasCollided = false;
	private Vector3 colNorm = new Vector3(0,0,0);
	public Text dimenText;

	void Start() {
		rb = GetComponent<Rigidbody>();
		player = new Player();
		transform.position = player.GetPos();
		transform.rotation = player.GetRot();
	}

	string GetDimenText(int outDimen) {
		if(outDimen == 0) {
			return "Dimensions: (y,z,w)";
		} else if(outDimen == 1) {
			return "Dimensions: (x,z,w)";
		} else if(outDimen == 2) {
			return "Dimensions: (x,y,w)";
		} else if(outDimen == 3) {
			return "Dimensions: (x,y,z)";
		} else {
			return "error";
		}
	}

	void SetFocus(bool focus) {
		this.focus = focus;
		if(focus) {
			//Cursor.lockState = CursorLockMode.Locked;
		} else {
			//Cursor.lockState = CursorLockMode.None;
		}
	}

	/*
	//collision code is a bit of a mess but it works right now
	void OnCollisionEnter(Collision col) {
		if(!hasCollided) {
			hasCollided = true;
			if(col.contacts.Length > 0) {
				colNorm = col.contacts[0].normal;
			}
			player.fixMove(colNorm);
		}
	}

	void OnCollisionStay(Collision col) {
		if(!hasCollided) {
			hasCollided = true;
			if(col.contacts.Length > 0) {
				//-1 because I think the normal gets flipped around
				//when you're inside
				colNorm = 1 * col.contacts[0].normal;
			}
			player.fixMove(colNorm);
		}
	}
	*/

	void FixedUpdate () {
		//handle focus
		if(Input.GetMouseButtonDown(0)) {
			SetFocus(true);
		}
		if(Input.GetKeyDown("escape")) {
			SetFocus(false);
		}

		if(Input.GetKeyDown("1") && player.outDimen != 0) {
			player.SetOutDimen(0);
		} else if(Input.GetKeyDown("2") && player.outDimen != 1) {
			player.SetOutDimen(1);
		} else if(Input.GetKeyDown("3") && player.outDimen != 2) {
			player.SetOutDimen(2);
		} else if(Input.GetKeyDown("4") && player.outDimen != 3) {
			player.SetOutDimen(3);
		}
		dimenText.text = GetDimenText(player.outDimen);

		//get all the inputs
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVertical = Input.GetAxis("Vertical");
		float moveRoll = Input.GetAxis("Roll");
		float mouseX = 0;
		float mouseY = 0;
		if(focus) {
			mouseX = Input.GetAxis("Mouse X");
			mouseY = Input.GetAxis("Mouse Y");
		}


		//apply all the inputs to the player
		player.LookUp(mouseY);
		player.LookLeft(mouseX);
		player.RollCW(moveRoll * Time.deltaTime * smooth);
		if(player.isPosInvalid) {
			rb.MovePosition(player.GetPos());
			player.isPosInvalid = false;
		} else {
			rb.AddForce(100 * player.GetRotVector() * moveVertical * Time.deltaTime * speed);
			player.SetPos(transform.position);
		}
		rb.MoveRotation(player.GetRot());
	}
}

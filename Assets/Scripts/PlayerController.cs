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

		public Vector3 GetUpVector() {
			return trimToVector3(up).normalized;
		}

		public void LookUp(float step) {
			var subRot = trimToVector3(rot).normalized;
			var subUp = trimToVector3(up).normalized;
			var axis = Vector3.Cross(subRot, subUp).normalized;
			var quat = Quaternion.AngleAxis(10 * step, axis);
			subRot = quat * subRot;
			subUp = quat * subUp;
			setVector3To4(rot, subRot);
			setVector3To4(up, subUp);
		}

		public void LookLeft(float step) {
			var subRot = trimToVector3(rot).normalized;
			var subUp = trimToVector3(up).normalized;
			var quat = Quaternion.AngleAxis(10 * step, subUp);
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

	string GetDirText()
	{
		return "Looking: (" + string.Join(", ", player.rot.Select(p => p.ToString("0.0"))) + ")";
	}

	void SetFocus(bool focus) {
		this.focus = focus;
		if(focus) {
			//Cursor.lockState = CursorLockMode.Locked;
		} else {
			//Cursor.lockState = CursorLockMode.None;
		}
	}

	private int keyDown = 0;

	void Update()
    {
		if (Input.GetKeyDown("1"))
			keyDown = 1;
		else if (Input.GetKeyDown("2"))
			keyDown = 2;
		else if (Input.GetKeyDown("3"))
			keyDown = 3;
		else if (Input.GetKeyDown("4"))
			keyDown = 4;
    }

	void FixedUpdate () {
		//handle focus
		if(Input.GetMouseButtonDown(0)) {
			SetFocus(true);
		}
		if(Input.GetKeyDown("escape")) {
			SetFocus(false);
		}

		if(keyDown == 1 && player.outDimen != 0) {
			player.SetOutDimen(0);
			keyDown = 0;
		} else if(keyDown == 2 && player.outDimen != 1) {
			player.SetOutDimen(1);
			keyDown = 0;
		} else if(keyDown == 3 && player.outDimen != 2) {
			player.SetOutDimen(2);
			keyDown = 0;
		} else if(keyDown == 4 && player.outDimen != 3) {
			player.SetOutDimen(3);
			keyDown = 0;
		}
		dimenText.text = GetDimenText(player.outDimen) + " " + GetDirText();

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
			rb.AddForce(100 * Vector3.Cross(player.GetUpVector(), player.GetRotVector()) * moveHorizontal * Time.deltaTime * speed);
			player.SetPos(transform.position);
		}
		GameManager.instance.UpdatePlayerPostion(player.pos);
		rb.MoveRotation(player.GetRot());
	}
}

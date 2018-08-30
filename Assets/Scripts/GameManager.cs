using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Linq;

public class GameManager : MonoBehaviour {

	public static GameManager instance = null;

	public GameObject wall;
	public GameObject goal;

	public List<int> startPos;

	private int order = 4;
	public int size;
	private int offset;
	private int[] data;
	private int outDimen = -1;//-1 is invalid, which forces an initial update

	private List<GameObject> curGameObjects = new List<GameObject>();

	long CoordsToIndex(List<int> coords) {
		long index = 0;
		for(int d = 0; d < order; d++) {
			index *= size;
			index += coords[d];
		}
		return index;
	}

	List<int> IndexToCoords(long index) {
		List<int> coords = new List<int>();
		for(int i = 0; i < order; i++) {
			//this should work, trust me
			long num = index % ((long) Math.Pow(size, order - i));
			long denom = (long) Math.Pow(size, order - i - 1);
			coords.Add((int) (num / denom) - offset);
		}
		return coords;
	}

	void SetData(List<int> coords, int val) {
		long index = CoordsToIndex(coords);
		data[index] = val;
	}

	int GetData(List<int> coords) {
		long index = CoordsToIndex(coords);
		return data[index];
	}

	class Node {
		public List<int> coords;
		public List<int> offCoords;
	}

	void Awake() {
		if(instance == null) {
			instance = this;
		} 
		//initialize the data
		offset = size / 2;
		data = new int[(long) Math.Pow(size, order)];
		//for now, we'll just wrap everything in a wall
		for(int i = 0; i < data.Length; i++) {
			var coords = IndexToCoords(i);
			if(coords.Exists(c => c == 0 || c == size-1)) {
				data[i] = 1;
			} else {
				data[i] = 1;
			}
		}
		//make a maze
		var start = new Node();
		start.coords = new List<int>();
		for(int i = 0; i < order; i++) {
			start.coords.Add(offset);
		}
		start.offCoords = start.coords;

		startPos = start.coords;

		//nodes we need to visit
		var open = new List<Node>();
		open.Add(start);
		//var closed = new List<List<int>>();
		Node last = null;
		
		while(open.Count > 0) {
			//pick random node
			int index = Random.Range(0, open.Count);
			var node = open[index];
			open.RemoveAt(index);

			//if it hasn't been taken, put the maze here
			
			if(node.coords.TrueForAll(c => c > 0 && c < size-1) &&
				data[CoordsToIndex(node.coords)] == 1) {

				//put the maze tile in the data
				data[CoordsToIndex(node.coords)] = 0;
				data[CoordsToIndex(node.offCoords)] = 0;
				last = node;

				//generate children
				for(int d = 0; d < order; d++) {
					for(int i = -1; i <= 1; i++) {
						if(i == 0) {
							continue;
						}
						var child = new Node();
						var offCoords = new List<int>();
						var coords = new List<int>();
						//generate one child for each direction
						//(i.e. 2 per dimension)
						for(int j = 0; j < order; j++) {
							if(j == d) {
								coords.Add(node.coords[j] + i * 2);
								offCoords.Add(node.coords[j] + i);
							} else {
								coords.Add(node.coords[j]);
								offCoords.Add(node.coords[j]);
							}
						}
						child.coords = coords;
						child.offCoords = offCoords;

						open.Add(child);
					}
				}
			}
		}
		data[CoordsToIndex(last.coords)] = 2;
		UpdateOutDimen(3, 0);
	}

	private Vector3 trimToVector3(List<float> vec4) {
		var subVec = new List<float>();
		for(int i = 0; i < order; i++) {
			if(i != outDimen) {
				subVec.Add(vec4[i]);
			}
		}
		return new Vector3(subVec[0], subVec[1], subVec[2]);

	}

	public void UpdateOutDimen(int outDimen, int outDimenVal) {
		if(outDimen == this.outDimen) {
			return;
		}
		this.outDimen = outDimen;
		//delete all existing game objects
		foreach(var gameObject in curGameObjects) {
			Destroy(gameObject);
		}
		curGameObjects = new List<GameObject>();
		
		//turn data into gameobjects
		for(int i = 0; i < data.Length; i++) {
			if(data[i] == 1) {
				//walls
				var coords = IndexToCoords(i);
				if(coords[outDimen] == outDimenVal) {
					var gameObject = Instantiate(wall, trimToVector3(coords.Select(c => (float) c).ToList()), Quaternion.identity) as GameObject;
					curGameObjects.Add(gameObject);
				}
			} else if(data[i] == 2) {
				//goal
				var coords = IndexToCoords(i);
				if(coords[outDimen] == outDimenVal) {
					var gameObject = Instantiate(goal, trimToVector3(coords.Select(c => (float) c).ToList()), Quaternion.identity) as GameObject;
					curGameObjects.Add(gameObject);
				}
			}
		}
	}


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

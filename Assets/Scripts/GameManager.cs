using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Linq;
using System.IO;

public class GameManager : MonoBehaviour {

	public static GameManager instance = null;

	public GameObject wall;
	public GameObject goal;
	public GameObject breadcrumb;
	public GameObject playerMarker;
	public AudioSource goalAudio;
	public Camera minimapCamera;

	public List<int> startPos;

	private int order = 4;
	public int size;
	private int offset;
	private List<int> data;
	private int outDimen = -1;//-1 is invalid, which forces an initial update

	private List<GameObject> curGameObjects = new List<GameObject>();
	private List<GameObject> minimapObjects = new List<GameObject>();

	private List<float> goalPos;

	//the separate static methods are for making testing easier
	//you can't load any classes with the fancy unity native objects easily in a normal nunit test

	public static int CoordsToIndex(int order, int size, int offset, List<int> coords) 
	{
		int index = 0;
		for(int d = 0; d < order; d++) {
			index *= size;
			index += coords[d];
		}
		return index;
	}

	public int CoordsToIndex(List<int> coords) {
		return CoordsToIndex(order, size, offset, coords);
	}

	public static List<int> IndexToCoords(int order, int size, int offset, long index) {
		List<int> coords = new List<int>();
		for(int i = 0; i < order; i++) {
			//this should work, trust me
			long num = index % ((long) Math.Pow(size, order - i));
			long denom = (long) Math.Pow(size, order - i - 1);
			coords.Add((int) (num / denom));
		}
		return coords;
	}

	public List<int> IndexToCoords(long index)
	{
		return IndexToCoords(order, size, offset, index);
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
		//for now, we'll just wrap everything in a wall
		data = Enumerable.Repeat(1, (int) Math.Pow(size, order)).ToList();
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
		goalPos = last.coords.Select(c => (float) c).ToList();

		UpdateOutDimen(3, 0);
		var hasOther = data.Where(d => d != 1).ToList();
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

	private float Cross(List<float> a, List<float> b) => a.Zip(b, (a1, b1) => a1 * b1).Sum();

	private float Mag(List<float> a) => (float) Math.Sqrt(a.Select(x => x * x).Sum());

	public void UpdateOutDimen(int outDimen, int outDimenVal) {
		outDimenVal += offset;
		var hasOther = data.Where(d => d != 1).ToList();
		var other = data.FindIndex(d => d != 1);
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
		for(int i = 0; i < data.Count; i++) {
            var coords = IndexToCoords(i);
			if (coords[outDimen] != outDimenVal)
				continue;
			GameObject type;
			if (data[i] == 1)
				type = wall;
			else if (data[i] == 2)
				type = goal;
			else if (data[i] == 3)
				type = breadcrumb;
			else if (data[i] == 0)
				continue; // empty space
			else
				throw new InvalidOperationException($"Can't render type {data[i]} at {i}");

            var gameObject = Instantiate(type, trimToVector3(coords.Select(c => (float) c - offset).ToList()), Quaternion.identity);
            curGameObjects.Add(gameObject);
		}
	}

	private readonly float MINIMAP_DISTANCE = 10f;
	private int prevPlayerIndex = -1;
	private (int, int) prevExcludedDimens = (-1, -1);
	public void UpdateMinimap(List<float> playerPos, List<float> playerUp, int outDimen)
	{
		var playerIndex = CoordsToIndex(playerPos.Select(p => (int) Math.Round(p + offset)).ToList());
		var center = minimapCamera.transform.position + minimapCamera.transform.forward * MINIMAP_DISTANCE;

		//figuring out which plane the player is looking in
		//which is the plane with the greatest angle to the up vector
		//I think in 4d, we can find the angle between a vector v and a 3-space XYZ by letting n = (0, 0, 0, v_w)
		//and considering |dot(n, v)| / (|n| * |v|)
		//once we find minimal space XYZ, ignore the out dimension (skip the 3-space that excludes the out dimen)
		//and now we have a plane that is in view
		var bestDimen = 0;
		var maxAngle = 0f;
		for (var d = 0; d < 4; d++)
		{
			if (d == outDimen) continue;
			var n = new List<float> { 0, 0, 0, 0 };
			//can't be the best option and will mess up our math later
			if (playerUp[d] == 0) continue;
			n[d] = playerUp[d];
			//btw sin(x) = x
			var angle = Math.Abs(Cross(n, playerUp)) / (Mag(n) * Mag(playerUp));
			if (angle > maxAngle)
			{
				maxAngle = angle;
				bestDimen = d;
			}
		}

		if (prevPlayerIndex == playerIndex && prevExcludedDimens == (outDimen, bestDimen))
		{
			return;
		}
		prevPlayerIndex = playerIndex;
		prevExcludedDimens = (outDimen, bestDimen);

		foreach (var obj in minimapObjects)
		{
			Destroy(obj);
		}
		minimapObjects.Clear();
		for (var i = 0; i < size; i++)
		{
			for (var j = 0; j < size; j++)
			{
				var index = 0;
				var coord = j;
				//least significant factors correspond to the higher dimensions
				//this is not intentional, but the rest of this script has been working like that for a while
				//and I guess it doesn't matter
				for (var d = 0; d < 4; d++)
				{
					if (d == bestDimen || d == outDimen)
					{
						index += ((int) Math.Round(playerPos[d] + offset)) * (int) Math.Pow(size, 3 - d);
					}
					else
					{
                        index += coord * (int) Math.Pow(size, 3 - d);
                        coord = i;
					}
				}
                GameObject type;
				if (index == playerIndex)
					type = playerMarker;
				else if (data[index] == 1)
					type = wall;
				else if (data[index] == 3)
					type = breadcrumb;
				else
					continue;
                //I should make this generic and assume the camera isn't pointed down
                var pos = center;
                pos.x += i - size / 2;
                pos.z += j - size / 2;
                minimapObjects.Add(Instantiate(type, pos, Quaternion.identity));
			}
		}
	}

	public void UpdatePlayerPostion(List<float> playerPos, List<float> playerUp)
    {
		UpdateMinimap(playerPos, playerUp, outDimen);

		var playerIndex = CoordsToIndex(playerPos.Select(p => (int) Math.Round(p + offset)).ToList());
		if (playerIndex > data.Count || playerIndex < 0)
		{
			Debug.LogWarning("Player index invalid");
			return;
		}
		if (data[playerIndex] == 0)
		{
			data[playerIndex] = 3;
			var coords = IndexToCoords(playerIndex);
			var gameObject = Instantiate(breadcrumb, trimToVector3(coords.Select(c => (float) c - offset).ToList()), Quaternion.identity) as GameObject;
			curGameObjects.Add(gameObject);
		}

		//player pos comes from game coords, but goal pos is in our coords
		//so we have to get rid of the offset
		var offsetGoalPos = goalPos.Select(g => g - offset).ToList();
		var player3 = trimToVector3(playerPos);
		var goal3 = trimToVector3(offsetGoalPos);
		var soundPos = goal3 - player3;
		soundPos = soundPos.normalized;
		var distance4 = Math.Sqrt(playerPos.Zip(offsetGoalPos, (p, g) => (p - g) * (p - g)).Sum());
		var distance3 = (float) Math.Pow(distance4, 1.5);
		soundPos = Vector3.Scale(soundPos, new Vector3(distance3, distance3, distance3));
		soundPos += player3;
		goalAudio.gameObject.transform.SetPositionAndRotation(soundPos, Quaternion.identity);
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapAIUnit : MonoBehaviour
{

	public Vector3 startingPoint;
	public Vector3 interest;
	public int iterations = 10000;
	[Range(1, 10)] public float viewingRange = 1;
	[Range(1, 20)] public float patrolRange = 1;
	[Range(1.4f, 4.2f)] public float movement = 1;

}

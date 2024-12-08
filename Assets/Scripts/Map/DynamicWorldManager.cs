using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DynamicWorldManager : MonoBehaviour
{

	[Range(0, 1f)] public float spawnChance = 0.2f;
	public int maxEnemies = 4;
	public Vector3[] spawnLocations;
	public GameObject[] spawnableEnemies;

	private Transform enemies;

	private string[] CreateRandomParty()
	{
		int partyNumber = Random.Range(1, 4);
		string[] characters = new string[partyNumber];
		for (int i = 0; i < partyNumber; i++)
		{
			characters[i] = spawnableEnemies[Random.Range(0, spawnableEnemies.Length)].GetComponent<UnitStatsScript>().Character;
		}
		return characters;
	}

	public bool CheckSpawnPos(Vector3 test)
	{
		foreach(Transform party in enemies)
		{
			if ((party.GetComponent<MapAIUnit>().startingPoint - test).magnitude < 1)
			{
				return false;
			}
		}
		return true;
	}

	public void SpawnEnemies()
	{
		if (enemies.childCount < maxEnemies && (Random.Range(0, 1f) <= spawnChance || enemies.childCount <= 0))
		{
			int iterations = 0;

			int spawnPos = Random.Range(0, spawnLocations.Length);
			Vector3 spawnPosition = spawnLocations[spawnPos];
			while (!CheckSpawnPos(spawnPosition))
			{
				iterations++;
				if (iterations > spawnLocations.Length)
				{
					Debug.LogWarning("Too many enemies, not enough spawn points!");
					return;
				}
				spawnPos++;
				spawnPos %= spawnLocations.Length;
				spawnPosition = spawnLocations[spawnPos];
			}

			SpawnEnemies(spawnPosition);
		}
	}

	public void SpawnEnemies(Vector3 position)
	{
		MapScript.singleton.SpawnEnemy(CreateRandomParty(), position);
	}

	private void Awake()
	{
		enemies = transform.Find("RoamingEnemies");
		SaveFile _data = DataManager.LoadData();
		if (_data == null || _data.levelName != SceneManager.GetActiveScene().name)
		{
			for (int i = 0; i < spawnLocations.Length; i++)
			{
				Vector3 spawnPosition = spawnLocations[i];
				if (CheckSpawnPos(spawnPosition))
				{
					SpawnEnemies(spawnPosition);
				}
			}
		}
	}
}
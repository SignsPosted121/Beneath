using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class MapScript : MonoBehaviour
{

	public static MapScript singleton;
	public Transform Characters;
	public Transform terrain;
	public float DayTime = 480;
	public float speed = 1.5f;
	public int money = 50;
	public Transform sun;

	[Header("Stuff for the editor only.")]

	public Vector3 playerPos;
	private Transform player;
	private Grid grid;
	private Transform tileCheck;

	private Vector3 mousePos;
	private Vector2 mousePoint;
	
	private void SetMousePosition(Vector2 mousePoint)
	{
		mousePos = Camera.main.ScreenToWorldPoint(mousePoint);
	}

	private float TileMagnitude(Vector3 first, Vector3 last)
	{
		float distance = Mathf.Sqrt((Mathf.Pow(last.x / 2 - first.x / 2, 2)) + (Mathf.Pow(last.y - first.y, 2)));
		return distance;
	}

	private bool SenseEnemy()
	{
		RaycastHit2D hit = Physics2D.Raycast(playerPos, Vector2.zero, 0, LayerMask.GetMask("Entities"));
		if (hit)
		{
			if (hit.transform.parent == transform.Find("RoamingEnemies"))
			{
				InitiateCombat(hit.transform.Find("Party").GetComponent<CharacterManagerScript>());
				return true;
			}
		}
		return false;
	}

	public Transform GetEnemy()
	{
		RaycastHit2D hit = Physics2D.Raycast(playerPos, Vector2.zero, 0, LayerMask.GetMask("Entities"));
		if (hit)
		{
			if (hit.transform.parent == transform.Find("RoamingEnemies"))
			{
				return hit.transform;
			}
		}
		return null;
	}

	private void SenseEvents()
	{
		RaycastHit2D hit = Physics2D.Raycast(playerPos, Vector2.zero, 0, LayerMask.GetMask("Events"));
		if (hit)
		{
			if (hit.transform.parent == transform.Find("Events"))
			{
				hit.transform.GetComponent<MapEvent>().TriggerEvent();
			}
		}
	}

	private float GetEffect(float time)
	{
		return Mathf.Pow(Mathf.Cos(time / 1440 * Mathf.PI * 2) + 1, 5) / 32;
	}

	private void UpdateTime()
	{
		// section time into 4 chunks
		// Create 4 colors and intensities that are elevated and delevetaed by the unconverted radian
		float nightEffect = GetEffect(DayTime + 1440 / 4 * 0);
		Color night = new Color(0.47f, 0.55f, 0.86f) * nightEffect;
		float nightInt = 0.2f * nightEffect;
		float morningEffect = GetEffect(DayTime + 1440 / 4 * 3);
		Color morning = new Color(0.86f, 0.74f, 0.48f) * morningEffect;
		float morningInt = 0.6f * morningEffect;
		float dayEffect = GetEffect(DayTime + 1440 / 4 * 2);
		Color day = Color.white * dayEffect;
		float dayInt = 0.8f * dayEffect;
		float eveningEffect = GetEffect(DayTime + 1440 / 4 * 1);
		Color evening = new Color(0.86f, 0.47f, 0.48f) * eveningEffect;
		float eveningInt = 0.6f * eveningEffect;

		Color currentColor = morning + day + evening + night;
		float currentIntensity = Mathf.Clamp01(morningInt + dayInt + eveningInt + nightInt);

		sun.GetComponent<UnityEngine.Rendering.Universal.Light2D>().color = currentColor;
		sun.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = currentIntensity;
	}

	private bool TestGround(MapAIUnit enemy, Vector2 dir)
	{
		Vector3 worldPos = enemy.transform.position + new Vector3(dir.x, dir.y);
		RaycastHit2D floorHit = Physics2D.Raycast(new Vector2(enemy.transform.position.x, enemy.transform.position.y) + dir, Vector2.zero, 0, LayerMask.GetMask("Floor"));
		if (floorHit && !floorHit.transform.CompareTag("Obstacles"))
		{
			Vector3 purePos = GetTilePosition(worldPos);
			RaycastHit2D hit = Physics2D.Raycast(purePos, Vector2.zero, 0, LayerMask.GetMask("Entities"));
			if (!hit && TileMagnitude(enemy.startingPoint, purePos) <= enemy.patrolRange)
			{
				return true;
			}
		}
		return false;
	}

	private void MoveAIRandom(MapAIUnit enemy)
	{
		// Checks if the enemy is at their destination (done first so their interest points gets set first turn)

		if (enemy.iterations >= 6)
		{
			float radialInt = Random.Range(-Mathf.PI, Mathf.PI);
			Vector2 dirInt = new Vector2(Mathf.Cos(radialInt), Mathf.Sin(radialInt));
			enemy.interest = enemy.startingPoint + new Vector3(dirInt.x, dirInt.y) * Random.Range(1, enemy.patrolRange);
			enemy.iterations = 0;
		}
		else
		{
			enemy.iterations++;
		}

		// Handles finding which way to go then moves that way

		if (TileMagnitude(enemy.interest, enemy.transform.position) < 1.5f)
		{
			return;
		}

		Vector3 moveDir = (enemy.interest - enemy.transform.position).normalized;
		float radial = Mathf.Atan2(moveDir.y, moveDir.x);
		Vector2 dir = new Vector2(Mathf.Cos(radial), Mathf.Sin(radial));
		int checks = 0;
		while (!TestGround(enemy, dir) && checks < 8)
		{
			radial += Mathf.PI / 4;
			dir = new Vector2(Mathf.Cos(radial), Mathf.Sin(radial));
			checks++;
		}
		if (checks >= 8)
		{
			return;
		}
		Vector3 purePos = GetTilePosition(enemy.transform.position + new Vector3(dir.x, dir.y));
		enemy.transform.position = purePos;
	}

	private void MoveAITowardsPlayer(MapAIUnit enemy)
	{
		if (TileMagnitude(playerPos, enemy.transform.position) < 0.5f)
		{
			return;
		}
		Vector3 moveDir = (playerPos - enemy.transform.position).normalized;
		float radial = Mathf.Atan2(moveDir.y, moveDir.x);
		Vector2 dir = new Vector2(Mathf.Cos(radial), Mathf.Sin(radial));
		int checks = 0;
		while (!TestGround(enemy, dir) && checks < 8)
		{
			radial += Mathf.PI / 4;
			dir = new Vector2(Mathf.Cos(radial), Mathf.Sin(radial));
			checks++;
		}
		if (checks >= 8)
		{
			return;
		}
		Vector3 purePos = GetTilePosition(enemy.transform.position + new Vector3(dir.x, dir.y));
		enemy.transform.position = purePos;
	}

	private void ParseAI()
	{
		Transform enemyFolder = transform.Find("RoamingEnemies");
		foreach(Transform enemy in enemyFolder)
		{
			if (TileMagnitude(enemy.position, playerPos) <= enemy.GetComponent<MapAIUnit>().viewingRange && TileMagnitude(enemy.GetComponent<MapAIUnit>().startingPoint, playerPos) <= enemy.GetComponent<MapAIUnit>().patrolRange)
			{
				MoveAITowardsPlayer(enemy.GetComponent<MapAIUnit>());
			}
			else
			{
				MoveAIRandom(enemy.GetComponent<MapAIUnit>());
			}
		}
	}

	public Transform FindEnemy(string enemyName)
	{
		if (Resources.Load("Characters\\Enemy\\" + enemyName) != null)
		{
			return Instantiate((GameObject)Resources.Load("Characters\\Enemy\\" + enemyName)).transform;
		}
		else if (Resources.Load("Characters\\Player\\" + enemyName) != null)
		{
			return Instantiate((GameObject)Resources.Load("Characters\\Player\\" + enemyName)).transform;
		}
		else if (Resources.Load("Characters\\" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\\" + enemyName) != null)
		{
			return Instantiate((GameObject)Resources.Load("Characters\\" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\\" + enemyName)).transform;
		}
		else
		{
			return null;
		}
	}

	public Transform SpawnEnemy(string[] characters, Vector3 position)
	{
		Transform enemy = Instantiate((GameObject)Resources.Load("Enemy"), transform.Find("RoamingEnemies")).transform;
		enemy.position = position;
		CharacterManagerScript party = enemy.Find("Party").GetComponent<CharacterManagerScript>();
		float totalXp = 0;
		for (int i = 0; i < characters.Length; i++)
		{
			Transform member = FindEnemy(characters[i]);
			if (member != null)
			{
				member.SetParent(party.transform);
				totalXp += member.GetComponent<UnitStatsScript>().Experience;
				party.PartyNumber++;
				switch (i)
				{
					default:
						party.PartyMember1 = member;
						break;
					case 1:
						party.PartyMember2 = member;
						break;
					case 2:
						party.PartyMember3 = member;
						break;
				}
			}
		}
		party.PartyNumber = characters.Length;
		enemy.GetComponent<LootTable>().xpDrop = totalXp;
		enemy.Find("Icon").GetComponent<SpriteRenderer>().sprite = party.PartyMember1.Find("UnitIcon").GetComponent<Image>().sprite;
		enemy.GetComponent<MapAIUnit>().startingPoint = position;
		return enemy;
	}

	private IEnumerator ProcessMovement(Vector3 newPos)
	{
		playerPos = newPos;
		DayTime += 10;
		if (DayTime > 1440)
		{
			DayTime %= 1440;
		}

		foreach(Transform member in Characters)
		{
			member.GetComponent<UnitStatsScript>().CurrentMagic++;
			if (member.GetComponent<UnitStatsScript>().CurrentMagic > member.GetComponent<UnitStatsScript>().Magic)
			{
				member.GetComponent<UnitStatsScript>().CurrentMagic = member.GetComponent<UnitStatsScript>().Magic;
			}
		}

		ParseAI();
		yield return new WaitForFixedUpdate();
		if (!SenseEnemy())
		{
			SenseEvents();
		}
		UpdateTime();
		GetComponent<DynamicWorldManager>().SpawnEnemies();
	}

	private void UpdateCheck()
	{
		RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0, LayerMask.GetMask("Floor"));
		if (hit && !hit.transform.CompareTag("Obstacles"))
		{
			Vector3 tilePos = GetTilePosition(hit.point);
			if (TileMagnitude(player.position, tilePos) <= speed)
			{
				tileCheck.gameObject.SetActive(true);
				tileCheck.position = tilePos;
			} else
			{
				tileCheck.gameObject.SetActive(false);
			}
		} else
		{
			tileCheck.gameObject.SetActive(false);
		}
	}

	public void InitiateCombat(CharacterManagerScript enemies)
	{
		CombatScript.singleton.gameObject.SetActive(true);
		foreach (Transform unit in CombatScript.singleton.transform.Find("Player"))
		{
			unit.gameObject.SetActive(false);
		}
		foreach (Transform unit in CombatScript.singleton.transform.Find("Enemy"))
		{
			unit.gameObject.SetActive(false);
		}
		CharacterManagerScript charactersTotal = Characters.GetComponent<CharacterManagerScript>();
		for (int i = 0; i < charactersTotal.PartyNumber; i++)
		{
			UnitStatsScript masterStats;
			switch (i)
			{
				default:
					masterStats = charactersTotal.PartyMember1.GetComponent<UnitStatsScript>();
					break;
				case 1:
					masterStats = charactersTotal.PartyMember2.GetComponent<UnitStatsScript>();
					break;
				case 2:
					masterStats = charactersTotal.PartyMember3.GetComponent<UnitStatsScript>();
					break;
			}
			if (masterStats.Health > 0)
			{
				CombatScript.singleton.GetComponent<CombatScript>().ImportCharacter(masterStats, CombatScript.singleton.transform.Find("Player").GetChild(i), true);
				CombatScript.singleton.transform.Find("Player").GetChild(i).gameObject.SetActive(true);
			}
		}
		for (int i = 0; i < enemies.PartyNumber; i++)
		{
			UnitStatsScript masterStats;
			switch (i)
			{
				default:
					masterStats = enemies.PartyMember1.GetComponent<UnitStatsScript>();
					break;
				case 1:
					masterStats = enemies.PartyMember2.GetComponent<UnitStatsScript>();
					break;
				case 2:
					masterStats = enemies.PartyMember3.GetComponent<UnitStatsScript>();
					break;
			}
			if (masterStats.Health > 0)
			{
				CombatScript.singleton.GetComponent<CombatScript>().ImportCharacter(masterStats, CombatScript.singleton.transform.Find("Enemy").GetChild(i), true);
				CombatScript.singleton.transform.Find("Enemy").GetChild(i).gameObject.SetActive(true);
			}
		}
		LootTable encounter = enemies.GetComponent<LootTable>();
		if (!encounter)
		{
			encounter = enemies.transform.parent.GetComponent<LootTable>();
		}
		StartCoroutine(CombatScript.singleton.GetComponent<CombatScript>().StartCombat(enemies.isBoss, encounter));
	}

	private void Awake()
	{
		if (singleton)
		{
			Destroy(this);
			Debug.LogWarning("Detected 2 Map Scripts. Say goodbye to this one.");
			return;
		}
		singleton = this;
		grid = transform.Find("Tiles").GetComponent<Grid>();
		tileCheck = transform.Find("Tiles").Find("Check");
		player = transform.Find("Player");
		playerPos = player.position;
		StartCoroutine(WaitATurn());
	}

	IEnumerator WaitATurn()
	{
		yield return new WaitForEndOfFrame();
		SenseEvents();
		UpdateTime();
		switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
		{
			case "Tutorial":
				break;
			default:
				SoundManager.singleton.PlaySound("Forrestia");
				break;
		}
	}

	private void Update()
	{
		Vector3 distancing = player.position - playerPos;
		player.Find("Cover").position = playerPos;
		if (TileMagnitude(player.position, playerPos) > 0.01f)
		{
			player.position -= new Vector3(distancing.x, distancing.y, 0f) * Time.deltaTime * 10;
			Camera.main.transform.position = new Vector3(player.position.x, player.position.y, -10);
		} else if (TileMagnitude(player.position, playerPos) != 0)
		{
			player.position = new Vector3(playerPos.x, playerPos.y, 0f);
			Camera.main.transform.position = new Vector3(player.position.x, player.position.y, -10);
		}

		if (CombatScript.singleton.gameObject.activeSelf)
		{
			return;
		}

		SetMousePosition(mousePoint);
		UpdateCheck();
	}

	private Vector3 GetTilePosition(Vector3 pos)
	{
		Vector3Int cellPoint = grid.WorldToCell(pos);
		return grid.GetCellCenterWorld(cellPoint);
	}

	private bool IsMouseOverUI()
	{
		return EventSystem.current.IsPointerOverGameObject();
	}

	public void MouseClick(InputAction.CallbackContext ctx)
	{
		if (ctx.performed && !IsMouseOverUI() && !CombatScript.singleton.gameObject.activeSelf)
		{
			RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0, LayerMask.GetMask("Floor"));
			if (hit && !hit.transform.CompareTag("Obstacles"))
			{
				Vector3 moveTo = GetTilePosition(hit.point);
				if (TileMagnitude(player.position, moveTo) <= speed)
				{
					StartCoroutine(ProcessMovement(moveTo));
				}
			}
		}
	}

	public void MouseMove(InputAction.CallbackContext ctx)
	{
		mousePoint = ctx.ReadValue<Vector2>();
	}
}

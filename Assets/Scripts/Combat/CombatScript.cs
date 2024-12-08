using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(ActionDescription))]
public class CombatScript : MonoBehaviour
{
	[SerializeField]public static CombatScript singleton;
	public Transform characterInventory;
	public GameObject attackPrefab;

	private Transform target;
	private Transform currentCharacter;
	private string currentAction = "";
	private bool needTarget;

	private Transform battleField;

	private LootTable enemies;

	public enum TurnState {Waiting, Player, Ai, Ending};
	public TurnState currentTurn;

	// Startup and finish methods

	private void Awake()
	{
		if (singleton)
		{
			Destroy(gameObject);
			Debug.LogWarning("Detected 2 Combat Scripts. Say goodbye to this one.");
			return;
		}
		singleton = this;
		battleField = transform.Find("BattleField");
		gameObject.SetActive(false);
	}

	public IEnumerator StartCombat(bool isBoss, LootTable encounter)
	{
		enemies = encounter;
		battleField.Find("Confirm").GetComponent<Button>().onClick.RemoveAllListeners();
		battleField.Find("Confirm").GetComponent<Button>().onClick.AddListener(() => StartCoroutine("PlayerAttemptAction"));
		SoundManager.singleton.StopAll(Sound.SoundClass.Music);
		if (!isBoss)
		{
			SoundManager.singleton.PlaySound("Combat");
		}
		else
		{
			SoundManager.singleton.PlaySound("BossFight");
		}
		currentTurn = TurnState.Waiting;
		yield return new WaitForEndOfFrame();
		CalculateAggro(transform.Find("Player"));
		CalculateAggro(transform.Find("Enemy"));
		foreach (Transform unit in transform.Find("Player"))
		{
			unit.GetComponent<UnitStatsScript>().CurrentSpeed = 0;
			unit.GetComponent<UnitStatsScript>().UpdateStats();
		}
		foreach (Transform unit in transform.Find("Enemy"))
		{
			unit.GetComponent<UnitStatsScript>().CurrentSpeed = 0;
			unit.GetComponent<UnitStatsScript>().UpdateStats();
		}
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = "...";
	}

	private IEnumerator FinishCombat()
	{
		currentTurn = TurnState.Ending;
		yield return new WaitForSeconds(2f);
		SoundManager.singleton.StopAll(Sound.SoundClass.Music);
		battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0);
		battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 1);
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = "You have defeated the enemy!";
		yield return new WaitForSeconds(3f);
		switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
		{
			case "Tutorial":
				break;
			default:
				SoundManager.singleton.PlaySound("Forrestia");
				break;
		}
		currentTurn = TurnState.Waiting;
		MapScript.singleton.money += Mathf.RoundToInt(Random.Range(enemies.coinDrop * enemies.coinVary, enemies.coinDrop));
		float xpDropped = Mathf.RoundToInt(Random.Range(enemies.xpDrop * enemies.xpVary, enemies.xpDrop + 1) * 10) / (10 * GetTargets(transform.Find("Player")).Count);
		string overall = "";
		for (int i = 0; i < transform.Find("Player").childCount; i++)
		{
			UnitStatsScript stats = transform.Find("Player").GetChild(i).GetComponent<UnitStatsScript>();
			if (!stats.gameObject.activeSelf)
			{
				continue;
			}
			stats.Experience += xpDropped;
			overall += stats.Character + " gained " + xpDropped + " xp.";
			if (stats.Experience >= stats.Level * 100)
			{
				stats.Experience -= stats.Level * 100;
				stats.Level++;
				stats.StatPoints++;
				overall += " " + stats.Character + " leveled up!";
				// TODO : Play our cool level up sound
			}
			overall += "\n";
			ExportCharacter(stats);
		}
		foreach (Transform unit in transform.Find("Enemy"))
		{
			unit.gameObject.SetActive(false);
		}
		foreach (Transform unit in transform.Find("Player"))
		{
			unit.gameObject.SetActive(false);
		}
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = overall;
		yield return new WaitForSeconds(5);
		battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0.7f);
		battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 1);
		if (enemies.transform.parent.name == "RoamingEnemies")
		{
			Destroy(enemies.gameObject);
		}
		gameObject.SetActive(false);
	}

	// General methods

	private void SetIcon(GameObject original, Transform unit)
	{
		if (unit.Find("Health").Find("Masking").Find(original.name))
		{
			Destroy(unit.Find("Health").Find("Masking").Find(original.name).gameObject);
		}
		Transform icon = Instantiate(original).transform;
		icon.name = original.name;
		icon.SetParent(unit.Find("Health").Find("Masking"));
		icon.GetComponent<RectTransform>().offsetMin = new Vector2();
		icon.GetComponent<RectTransform>().offsetMax = new Vector2();
		icon.localScale = new Vector3(1, 1, 1);
	}

	public void ImportCharacter(UnitStatsScript replacement, Transform member, bool starting)
	{
		if (member.parent.name == "Player" && !starting)
		{
			ExportCharacter(member.GetComponent<UnitStatsScript>());
		}
		Destroy(member.GetComponent<UnitStatsScript>());
		System.Type type = replacement.GetType();
		Component copy = member.gameObject.AddComponent(type);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(replacement));
		}
		SetIcon(replacement.transform.Find("UnitIcon").gameObject, member);
		SetIcon(replacement.transform.Find("Hurt").gameObject, member);
		SetIcon(replacement.transform.Find("Buffed").gameObject, member);
		SetIcon(replacement.transform.Find("Debuffed").gameObject, member);
		member.Find("Health").Find("UnitName").GetComponent<TextMeshProUGUI>().text = replacement.Character;
		member.gameObject.SetActive(true);
	}

	private void ExportCharacter(UnitStatsScript character)
	{
		Transform replacing = characterInventory.Find(character.Character);
		if (replacing == null)
		{
			Debug.LogError("Could not find " + character.Character);
			return;
		}
		Destroy(replacing.GetComponent<UnitStatsScript>());
		System.Type type = character.GetType();
		Component copy = replacing.gameObject.AddComponent(type);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(character));
		}
		(copy as UnitStatsScript).Noticability = 10;
		(copy as UnitStatsScript).CurrentSpeed = 0;
		(copy as UnitStatsScript).Shield = 0;
	}

	private void CalculateAggro(Transform unitTab)
	{
		float totalNotice = 0;
		for (int i = 0; i < unitTab.childCount; i++)
		{
			Transform Unit = unitTab.GetChild(i);
			if (Unit.GetComponent<UnitStatsScript>().Health > 0 && Unit.gameObject.activeSelf)
			{
				totalNotice += Unit.GetComponent<UnitStatsScript>().Noticability;
			}
		}
		for (int i = 0; i < unitTab.childCount; i++)
		{
			Transform Unit = unitTab.GetChild(i);
			Unit.GetComponent<UnitStatsScript>().Aggression = Mathf.Floor(Unit.GetComponent<UnitStatsScript>().Noticability / totalNotice * 100 + 0.5f);
			if (Unit.GetComponent<UnitStatsScript>().Health <= 0 || !Unit.gameObject.activeSelf)
			{
				Unit.GetComponent<UnitStatsScript>().Aggression = 0;
			}
		}
	}

	private void AssignSpots(Transform performer, Transform victim, bool actionPage)
	{
		if (performer.parent.name == "Player")
		{
			battleField.Find("Performer").GetComponent<Image>().color = new Color(0.3f, 1, 0.3f, 1);
		}
		else
		{
			battleField.Find("Performer").GetComponent<Image>().color = new Color(1, 0, 0, 1);
		}
		if (battleField.Find("Performer").Find("Mask").childCount > 0)
		{
			Destroy(battleField.Find("Performer").Find("Mask").GetChild(0).gameObject);
		}
		Transform picture = Instantiate(performer.Find("Health").Find("Masking").Find("UnitIcon"));
		picture.GetComponent<Image>().color = new Color(1, 1, 1, 1);
		picture.SetParent(battleField.Find("Performer").Find("Mask"));
		battleField.Find("Performer").Find("UnitName").GetComponent<TextMeshProUGUI>().text = performer.GetComponent<UnitStatsScript>().Character;
		picture.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
		picture.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
		if (victim != null)
		{
			if (victim.parent.name == "Player")
			{
				battleField.Find("Target").GetComponent<Image>().color = new Color(0.3f, 1, 0.3f, 1);
			}
			else
			{
				battleField.Find("Target").GetComponent<Image>().color = new Color(1, 0, 0, 1);
			}
			if (battleField.Find("Target").Find("Mask").childCount > 0)
			{
				Destroy(battleField.Find("Target").Find("Mask").GetChild(0).gameObject);
			}
			picture = Instantiate(victim.Find("Health").Find("Masking").Find("UnitIcon"));
			picture.GetComponent<Image>().color = new Color(1, 1, 1, 1);
			picture.SetParent(battleField.Find("Target").Find("Mask"));
			battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = victim.GetComponent<UnitStatsScript>().Character;
			picture.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
			picture.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
		}
		SwitchPlayerInteracts(actionPage);
	}

	private void SwitchPlayerInteracts(bool state)
	{
		if (state)
		{
			battleField.GetChild(0).gameObject.SetActive(true);
			battleField.GetChild(1).gameObject.SetActive(true);
		}
		else
		{
			battleField.GetChild(0).gameObject.SetActive(false);
			battleField.GetChild(1).gameObject.SetActive(false);
			battleField.GetChild(2).gameObject.SetActive(false);
		}
	}

	public void SelectTarget(Transform ChosenTarget)
	{
		if (currentTurn == TurnState.Player)
		{
			if (target == ChosenTarget)
			{
				target.Find("Selected").GetComponent<Image>().color = new Color(1, 1, 1, 0);
				battleField.Find("Target").GetComponent<Image>().color = new Color(1, 0, 0, 0);
				battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = target.GetComponent<UnitStatsScript>().Character;
				battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);
				Destroy(battleField.Find("Target").Find("Mask").GetChild(0).gameObject);
				target = null;
			}
			else
			{
				if (target)
				{
					target.Find("Selected").GetComponent<Image>().color = new Color(1, 1, 1, 0);
					battleField.Find("Target").GetComponent<Image>().color = new Color(1, 0, 0, 0);
					battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = target.GetComponent<UnitStatsScript>().Character;
					battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);
					Destroy(battleField.Find("Target").Find("Mask").GetChild(0).gameObject);
				}
				target = ChosenTarget;
				target.Find("Selected").GetComponent<Image>().color = new Color(1, 1, 1, 1);
				if (ChosenTarget.parent.name == "Enemy")
				{
					battleField.Find("Target").GetComponent<Image>().color = new Color(1, 0, 0, 1);
				}
				else if (ChosenTarget != currentCharacter)
				{
					battleField.Find("Target").GetComponent<Image>().color = new Color(0, 1, 0, 1);
				}
				else
				{
					battleField.Find("Target").GetComponent<Image>().color = new Color(0, 0.2f, 1, 1);
				}
				battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = target.GetComponent<UnitStatsScript>().Character;
				battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
				Transform Picture = Instantiate(target.Find("Health").Find("Masking").Find("UnitIcon"));
				Picture.SetParent(battleField.Find("Target").Find("Mask"));
				Picture.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
				Picture.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
			}
			CheckConfirmButton();
		}
	}

	// Specific combat methods

	public void TakeTurn(Transform Character)
	{
		if (Character.parent != transform.Find("Player") && !AIThinking)
		{
			currentTurn = TurnState.Ai;
			StartCoroutine(StartAI(Character));
		}
		else
		{
			currentTurn = TurnState.Player;
			currentCharacter = Character;
			SetupPlayerActions();
			AssignSpots(Character, null, true);
		}
	}

	public IEnumerator Death(Transform Character)
	{
		UnitStatsScript Stats = Character.GetComponent<UnitStatsScript>();
		Stats.Health = 0;
		CalculateAggro(transform.Find("Player"));
		CalculateAggro(transform.Find("Enemy"));
		Character.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
		int goodGuys = 0;
		int badGuys = 0;
		for (int i = 0; i < transform.Find("Player").childCount; i++)
		{
			Transform Unit = transform.Find("Player").GetChild(i);
			if (Unit.GetComponent<UnitStatsScript>().Health > 0 && Unit.gameObject.activeSelf)
			{
				goodGuys++;
			}
		}
		for (int i = 0; i < transform.Find("Enemy").childCount; i++)
		{
			Transform Unit = transform.Find("Enemy").GetChild(i);
			if (Unit.GetComponent<UnitStatsScript>().Health > 0 && Unit.gameObject.activeSelf)
			{
				badGuys++;
			}
		}
		if (badGuys <= 0)
		{
			StartCoroutine(FinishCombat());
		}
		else if (goodGuys <= 0)
		{
			currentTurn = TurnState.Ending;
			yield return new WaitForSeconds(2f);
			yield return new WaitForEndOfFrame();
			SoundManager.singleton.StopAll(Sound.SoundClass.Music);
			battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0);
			battleField.Find("EventDesc").GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 1);
			battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = "You have perished.";
			yield return new WaitForSeconds(3f);
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}

	public void ChangeActionMenu(Transform newMenu)
	{
		Transform actionParent = battleField.Find("Actions");
		foreach(Transform menu in actionParent)
		{
			menu.gameObject.SetActive(false);
		}
		newMenu.gameObject.SetActive(true);
	}

	private void AddAction(string action, Transform parentList, int SPcost)
	{
		Transform newButton = Instantiate(attackPrefab, parentList).transform;
		newButton.name = action.Replace(" ", "");
		newButton.Find("Action").GetComponent<TextMeshProUGUI>().text = action;
		Action details = gameObject.GetComponent<ActionDescription>().FindAction(action);
		if (!details.needTarget)
		{
			newButton.Find("Target").GetComponent<TextMeshProUGUI>().text = "Multiple Targets";
		}
		if (details.magicCost > 0)
		{
			if (currentCharacter.GetComponent<UnitStatsScript>().Magic >= SPcost)
			{
				newButton.Find("Cost").GetComponent<TextMeshProUGUI>().color = new Color(0.02f, 0.33f, 0.68f, 1);
			} else
			{
				newButton.Find("Cost").GetComponent<TextMeshProUGUI>().color = new Color(0.75f, 0, 0, 1);
			}
			newButton.Find("Cost").GetComponent<TextMeshProUGUI>().text = SPcost + " SP";
		}
		newButton.GetComponent<Button>().onClick.AddListener(() => SetCurrentAction(action, newButton));
	}

	private void WipeActions()
	{
		Transform actionParent = battleField.Find("Actions");
		foreach (Transform action in actionParent.Find("Attacks").Find("Viewport").Find("Content"))
		{
			Destroy(action.gameObject);
		}
		foreach (Transform action in actionParent.Find("Abilities").Find("Viewport").Find("Content"))
		{
			Destroy(action.gameObject);
		}
		foreach (Transform action in actionParent.Find("Items").Find("Viewport").Find("Content"))
		{
			Destroy(action.gameObject);
		}
		foreach (Transform menu in actionParent)
		{
			menu.gameObject.SetActive(false);
		}
		actionParent.GetChild(0).gameObject.SetActive(true);
	}

	void SetupPlayerActions()
	{
		WipeActions();
		Transform actionParent = battleField.Find("Actions");
		List<string> actions = currentCharacter.GetComponent<UnitStatsScript>().attacks;
		foreach (string action in actions)
		{
			Action details = gameObject.GetComponent<ActionDescription>().FindAction(action);
			switch (details.currentType)
			{
				case Action.actionType.Damage:
					AddAction(action, actionParent.Find("Attacks").Find("Viewport").Find("Content"), details.magicCost);
					break;
				default:
					AddAction(action, actionParent.Find("Abilities").Find("Viewport").Find("Content"), details.magicCost);
					break;
			}
		}
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = currentCharacter.GetComponent<UnitStatsScript>().Character + " is thinking...";
	}

	public bool CheckConfirmButton()
	{
		if (currentAction == "" || (needTarget && target == null))
		{
			battleField.Find("Confirm").Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.6f);
			battleField.Find("Confirm").GetComponent<Button>().interactable = false;
			return false;
		}
		battleField.Find("Confirm").Find("Text").GetComponent<TextMeshProUGUI>().color = Color.white;
		battleField.Find("Confirm").GetComponent<Button>().interactable = true;
		return true;
	}

	private Transform currentButton;

	void SetCurrentAction(string action, Transform button)
	{
		currentAction = action;
		Action details = gameObject.GetComponent<ActionDescription>().FindAction(action);
		needTarget = details.needTarget;
		if (currentButton)
		{
			currentButton.GetComponent<Image>().color = new Color(0, 0, 0, 1);
		}
		button.GetComponent<Image>().color = new Color(1, 1, 0, 1);
		currentButton = button;
		battleField.Find("Description").gameObject.SetActive(true);
		battleField.Find("Description").Find("Text").GetComponent<TextMeshProUGUI>().text = details.desc;
		CheckConfirmButton();
	}

	public IEnumerator PlayerAttemptAction()
	{
		if (((needTarget && target) || !needTarget) && currentAction != "")
		{
			if (!PerformAction(currentAction, target, currentCharacter))
			{
				yield return null;
			}
			SwitchPlayerInteracts(false);
			yield return new WaitForSeconds(2);
			if (target)
			{
				target.Find("Selected").GetComponent<Image>().color = new Color(1, 1, 1, 0);
				battleField.Find("Target").GetComponent<Image>().color = new Color(1, 1, 1, 0);
				Destroy(battleField.Find("Target").Find("Mask").GetChild(0).gameObject);
				target = null;
			}
			if (battleField.Find("Performer").Find("Mask").GetChild(0))
			{
				Destroy(battleField.Find("Performer").Find("Mask").GetChild(0).gameObject);
			}
			battleField.Find("Description").gameObject.SetActive(false);
			battleField.Find("Performer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
			battleField.Find("Performer").Find("UnitName").GetComponent<TextMeshProUGUI>().text = "";
			battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = "";
			currentCharacter = null;
			currentAction = "";
			if (currentTurn != TurnState.Ending)
			{
				currentTurn = TurnState.Waiting;
			}
		}
		if (currentTurn != TurnState.Ending)
		{
			battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = "...";
		}
		yield return null;
	}

	private string CheckForShield(UnitStatsScript check, UnitStatsScript attacker, string original)
	{
		if (check.Shield > 1)
		{
			return attacker.Character + " cracked " + check.Character + "'s shield!";
		}
		else if (check.Shield > 0)
		{
			return attacker.Character + " broke " + check.Character + "'s shield!";
		}
		else
		{
			return original;
		}
	}

	// Ai

	private bool AIThinking = false;

	private UnitStatsScript GetTargetByAggression()
	{
		UnitStatsScript Subject = transform.Find("Player").GetChild(0).GetComponent<UnitStatsScript>();

		float ChosenTarget = Random.Range(0, 100);
		float CurrentAggression = 0;
		for (int i = 0; i < transform.Find("Player").childCount; i++)
		{
			UnitStatsScript Unit = transform.Find("Player").GetChild(i).GetComponent<UnitStatsScript>();
			if (Unit.Health <= 0 || !Unit.gameObject.activeSelf)
			{
				continue;
			}
			CurrentAggression += Unit.Aggression;
			if (ChosenTarget < CurrentAggression && ChosenTarget >= CurrentAggression - Unit.GetComponent<UnitStatsScript>().Aggression)
			{
				Subject = Unit;
			}
		}

		return Subject;
	}

	private List<UnitStatsScript> GetTargets(Transform page)
	{
		List<UnitStatsScript> targets = new List<UnitStatsScript>();
		foreach(Transform unit in page)
		{
			if (unit.gameObject.activeSelf && unit.GetComponent<UnitStatsScript>().Health > 0)
			{
				targets.Add(unit.GetComponent<UnitStatsScript>());
			}
		}
		return targets;
	}

	IEnumerator StartAI(Transform Character)
	{
		AIThinking = true;
		UnitStatsScript stats = Character.GetComponent<UnitStatsScript>();
		UnitStatsScript target;
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = stats.Character + " is thinking...";

		string actionUsing = stats.attacks[Random.Range(0, stats.attacks.Count)];
		Action details = gameObject.GetComponent<ActionDescription>().FindAction(actionUsing);

		switch (details.currentType)
		{
			default:
				target = GetTargetByAggression();
				break;
			case Action.actionType.Buff:
				if (Random.Range(0, 4) < 3)
				{
					List<UnitStatsScript> targets = GetTargets(transform.Find("Enemy"));
					target = targets[Random.Range(0, targets.Count)];
				}
				else
				{
					List<UnitStatsScript> targets = GetTargets(transform.Find("Player"));
					target = targets[Random.Range(0, targets.Count)];
				}
				break;
			case Action.actionType.Debuff:
				List<UnitStatsScript> enemies = GetTargets(transform.Find("Player"));
				target = enemies[Random.Range(0, enemies.Count)];
				break;
			case Action.actionType.Healing:
				List<UnitStatsScript> friendlies = GetTargets(transform.Find("Enemy"));
				target = friendlies[Random.Range(0, friendlies.Count)];
				break;
		}

		if (stats.CurrentMagic < details.magicCost || actionUsing == "None" || actionUsing == "")
		{
			StartCoroutine(StartAI(Character));
			yield break;
		}
		if (details.needTarget && target.Health < 0)
		{
			StartCoroutine(StartAI(Character));
			yield break;
		}
		if (target.transform == Character && actionUsing == "Speedy Tune")
		{
			StartCoroutine(StartAI(Character));
			yield break;
		}

		AssignSpots(Character, null, false);
		AssignSpots(Character, target.transform, false);
		PerformAction(actionUsing, target.transform, Character);
		yield return new WaitForSeconds(2);
		if (battleField.Find("Target").Find("Mask").childCount > 0)
		{
			battleField.Find("Target").GetComponent<Image>().color = new Color(1, 0, 0, 0);
			Destroy(battleField.Find("Target").Find("Mask").GetChild(0).gameObject);
		}
		Destroy(battleField.Find("Performer").Find("Mask").GetChild(0).gameObject);
		battleField.Find("Description").gameObject.SetActive(false);
		battleField.Find("Performer").GetComponent<Image>().color = new Color(1, 1, 1, 0);
		battleField.Find("Performer").Find("UnitName").GetComponent<TextMeshProUGUI>().text = "";
		battleField.Find("Target").Find("UnitName").GetComponent<TextMeshProUGUI>().text = "";
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = "...";
		AIThinking = false;
		if (currentTurn != TurnState.Ending)
		{
			currentTurn = TurnState.Waiting;
		}
	}

	// Core loop

	private void Update()
	{
		if (currentTurn != TurnState.Waiting)
		{
			return;
		}
		bool ChargeUnit(UnitStatsScript unit)
		{
			if (unit.Health > 0 && unit.gameObject.activeSelf)
			{
				unit.CurrentSpeed += Time.deltaTime;
			}
			else if (unit.CurrentSpeed > 0)
			{
				unit.CurrentSpeed -= Time.deltaTime * 4;
			}
			else
			{
				unit.CurrentSpeed = 0;
			}
			unit.UpdateSpeed();
			//CurrentUltimateCharge += Time.deltaTime;
			//Add these at a future time.
			if (unit.CurrentSpeed >= unit.Speed)
			{
				unit.CurrentSpeed -= unit.Speed;
				TakeTurn(unit.transform);
				return false;
			}
			return true;
		}
		foreach (Transform unit in transform.Find("Player"))
		{
			if (!ChargeUnit(unit.GetComponent<UnitStatsScript>()))
			{
				return;
			}
		}
		foreach (Transform unit in transform.Find("Enemy"))
		{
			if (!ChargeUnit(unit.GetComponent<UnitStatsScript>()))
			{
				return;
			}
		}
	}
	bool PerformAction(string action, Transform Victim, Transform Performer)
	{
		UnitStatsScript VStats = null;
		UnitStatsScript PStats = Performer.GetComponent<UnitStatsScript>();
		float DidThing = 0;
		string actionDisc = "";
		Action details = gameObject.GetComponent<ActionDescription>().FindAction(action);
		action = action.Replace(" ", "");
		if (Victim)
		{
			VStats = Victim.GetComponent<UnitStatsScript>();
			if (VStats.Health <= 0)
			{
				return false;
			}
			Debug.Log(PStats.Character + " is using " + action + " on " + VStats.Character);
		}
		else
		{
			Debug.Log(PStats.Character + " is using " + action);
		}

		// Attacks

		if (action == "Punch")
		{
			float damage = PStats.GetDamage(VStats, 1, 1);
			actionDisc = PStats.Character + " punched " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			VStats.TakeDamage(damage, 0);
			DidThing = damage / 2;
		}
		if (action == "ElbowStrike")
		{
			float damage = PStats.GetDamage(VStats, 0.9f, 1.2f);
			actionDisc = PStats.Character + " elbowed " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			VStats.TakeDamage(damage, 0);
			DidThing = damage / 2;
		}
		if (action == "Kick")
		{
			float damage = PStats.GetDamage(VStats, 0.8f, 0.8f);
			if (Random.Range(0, 4) >= 3)
			{
				damage *= 2;
			}
			actionDisc = PStats.Character + " kicked " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			VStats.TakeDamage(damage, 0);
			DidThing = damage * 0.75f;
		}
		if (action == "FullPunch")
		{
			float damage = PStats.GetDamage(VStats, 1.5f, 1.5f);
			actionDisc = PStats.Character + " sucker punched " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			VStats.TakeDamage(damage, 0);
			DidThing = damage * 0.75f;
		}
		if (action == "FocusedStrike")
		{
			float damage = 3;
			actionDisc = PStats.Character + " struck " + VStats.Character + " for " + Mathf.Round(damage) + " penetration damage!";
			VStats.TakeDamage(damage, 1);
			DidThing = 30;
		}

		// Buffs

		if (action == "SoothingWords")
		{
			DidThing = -PStats.Noticability / 2;
			actionDisc = PStats.Character + " smooth talked the enemy!";
			StartCoroutine(Performer.GetComponent<UnitStatsScript>().PlayBuffIndicator());
		}
		if (action == "MercilessCursing")
		{
			DidThing = 60;
			actionDisc = PStats.Character + " holds his ground as he slings insults!";
			Performer.GetComponent<UnitStatsScript>().Shield++;
			StartCoroutine(Performer.GetComponent<UnitStatsScript>().PlayBuffIndicator());
		}

		// Debuffs

		if (action == "Takedown")
		{
			actionDisc = PStats.Character + " drops " + VStats.Character + " to the ground, disabling them!";
			DidThing = VStats.CurrentSpeed * 6;
			VStats.CurrentSpeed = 0;
			PStats.CurrentSpeed += PStats.Speed * 0.2f;
			StartCoroutine(Victim.GetComponent<UnitStatsScript>().PlayDebuffIndicator());
		}

		// Magic

		if (action == "MotivatingWords")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			actionDisc = PStats.Character + " motivates their team!";
			for (int i = 0; i < Performer.parent.childCount; i++)
			{
				UnitStatsScript CurrentStats = Performer.parent.GetChild(i).GetComponent<UnitStatsScript>();
				CurrentStats.CurrentUltimateCharge += CurrentStats.UltimateCharge / 10;
				StartCoroutine(CurrentStats.PlayBuffIndicator());
			}
			DidThing = 16;
			PStats.CurrentMagic -= details.magicCost;
		}
		if (action == "TacticalMind")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			actionDisc = PStats.Character + " optimizes strategies, and boosts their team's speed!";
			foreach (Transform unit in Performer.parent)
			{
				unit.GetComponent<UnitStatsScript>().CurrentSpeed += unit.GetComponent<UnitStatsScript>().Speed / 4;
				StartCoroutine(unit.GetComponent<UnitStatsScript>().PlayBuffIndicator());
			}
			foreach (Transform unit in PStats.GetEnemies())
			{
				unit.GetComponent<UnitStatsScript>().CurrentSpeed -= unit.GetComponent<UnitStatsScript>().Speed / 4;
				StartCoroutine(unit.GetComponent<UnitStatsScript>().PlayDebuffIndicator());
			}
			PStats.CurrentMagic -= details.magicCost;
			DidThing = 40;
		}
		if (action == "HealingTouch")
		{
			float MagicUse = details.magicCost;
			if (PStats.CurrentMagic < MagicUse)
			{
				MagicUse = PStats.CurrentMagic;
			}
			VStats.TakeDamage(-MagicUse * 5f, 0);
			PStats.CurrentMagic -= MagicUse;
			DidThing = MagicUse * 6;
			if (MagicUse <= 0)
			{
				return false;
			}
			actionDisc = PStats.Character + " heals " + VStats.Character + " for " + Mathf.Round(MagicUse * 5f) + " HP!";
		}
		if (action == "SpeedyTune")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			PStats.CurrentMagic -= details.magicCost;
			DidThing = 16;
			if (Victim.parent == Performer.parent)
			{
				VStats.CurrentSpeed += VStats.Speed / 10 * 3.5f;
				actionDisc = PStats.Character + " plays a lil' diddy and speeds up " + VStats.Character + "'s turn!";
			}
			else
			{
				VStats.CurrentSpeed = 0;
				actionDisc = PStats.Character + " plays a lil' diddy and distracts " + VStats.Character + "!";
			}
		}
		if (action == "DeathMetal")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			for (int i = 0; i < transform.Find("Player").childCount; i++)
			{
				UnitStatsScript CurrentStats = transform.Find("Player").GetChild(i).GetComponent<UnitStatsScript>();
				if (Performer.parent.name == "Player")
				{
					CurrentStats.CurrentUltimateCharge += CurrentStats.UltimateCharge / 10;
					StartCoroutine(CurrentStats.PlayBuffIndicator());
				}
				else
				{
					CurrentStats.CurrentUltimateCharge -= CurrentStats.UltimateCharge / 10;
					CurrentStats.Penetration += 2;
					StartCoroutine(CurrentStats.PlayDebuffIndicator());
				}
			}
			for (int i = 0; i < transform.Find("Enemy").childCount; i++)
			{
				UnitStatsScript CurrentStats = transform.Find("Enemy").GetChild(i).GetComponent<UnitStatsScript>();
				if (Performer.parent.name == "Enemy")
				{
					CurrentStats.CurrentUltimateCharge += CurrentStats.UltimateCharge / 10;
					StartCoroutine(CurrentStats.PlayBuffIndicator());
				}
				else
				{
					CurrentStats.CurrentUltimateCharge -= CurrentStats.UltimateCharge / 10;
					CurrentStats.Penetration += 2;
					StartCoroutine(CurrentStats.PlayDebuffIndicator());
				}
			}
			PStats.CurrentMagic -= details.magicCost;
			DidThing = 40;
			actionDisc = PStats.Character + " plays some sick riffs buffing their team, and rupturing the enemy's eardrums!";
		}
		if (action == "SelflessAct")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			if (PStats.transform.parent.name == "Player")
			{
				foreach (Transform unit in transform.Find("Player"))
				{
					if (unit != Performer)
					{
						unit.GetComponent<UnitStatsScript>().Shield++;
						StartCoroutine(unit.GetComponent<UnitStatsScript>().PlayBuffIndicator());
					}
				}
			}
			else
			{
				foreach (Transform unit in transform.Find("Enemy"))
				{
					if (unit != Performer)
					{
						unit.GetComponent<UnitStatsScript>().Shield++;
						StartCoroutine(unit.GetComponent<UnitStatsScript>().PlayBuffIndicator());
					}
				}
			}
			PStats.CurrentMagic -= details.magicCost;
			DidThing = 40;
			actionDisc = PStats.Character + " throws himself in front of his allies!";
		}
		if (action == "SoulGrasp")
		{
			if (PStats.CurrentMagic < details.magicCost)
			{
				return false;
			}
			VStats.CurrentMagic -= details.magicCost * 2;
			PStats.CurrentMagic -= details.magicCost;
			DidThing = 20;
			actionDisc = PStats.Character + " strains " + VStats.Character + "'s soul!";
		}

		if (Victim && VStats.Health <= 0)
		{
			DidThing = 50;
			actionDisc = PStats.Character + " knocked out " + VStats.Character + "!";
		}

		// Non player attacks

		if (action == "HoofPunch")
		{
			float damage = PStats.GetDamage(VStats, 1, 1);
			actionDisc = PStats.Character + " hoofed " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			VStats.TakeDamage(damage, 0);
			DidThing = damage / 2;
		}

		if (action == "Ram")
		{
			float damage = PStats.GetDamage(VStats, 0.75f, 0.75f);
			actionDisc = PStats.Character + " rammed " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			if (Random.Range(0, 3) != 0)
			{
				VStats.CurrentSpeed = 0;
				actionDisc += " They are distracted for a turn!";
			}
			VStats.TakeDamage(damage, 0);
			DidThing = damage / 2;
		}

		if (action == "Smash")
		{
			float damage = PStats.GetDamage(VStats, 1, 1);
			actionDisc = PStats.Character + " pounded " + VStats.Character + " for " + Mathf.Round(damage) + " damage!";
			actionDisc = CheckForShield(VStats, PStats, actionDisc);
			actionDisc += " They also suffer 1 penetration damage!";
			VStats.TakeDamage(damage, 0);
			VStats.TakeDamage(1, 1);
			DidThing = damage / 2;
		}

		if (action == "Shake")
		{
			actionDisc = PStats.Character + " shakes the ground around your party, distracting them!";
			foreach(Transform enemy in PStats.GetEnemies())
			{
				enemy.GetComponent<UnitStatsScript>().CurrentSpeed -= enemy.GetComponent<UnitStatsScript>().Speed / 2;
				StartCoroutine(enemy.GetComponent<UnitStatsScript>().PlayDebuffIndicator());
			}
			PStats.CurrentSpeed += PStats.Speed / 4;
			DidThing = 40;
		}

		// Finishing up the method

		needTarget = true;
		currentAction = ""; 
		CheckConfirmButton();
		for (int i = 0; i < battleField.Find("Actions").childCount; i++)
		{
			battleField.Find("Actions").GetChild(i).GetComponent<Image>().color = new Color(0, 0, 0, 1);
		}

		PStats.Noticability += DidThing;
		CalculateAggro(Performer.parent);
		battleField.Find("EventDesc").Find("Text").GetComponent<TextMeshProUGUI>().text = actionDisc;

		foreach (Transform unit in transform.Find("Player"))
		{
			unit.GetComponent<UnitStatsScript>().UpdateStats();
		}
		foreach (Transform unit in transform.Find("Enemy"))
		{
			unit.GetComponent<UnitStatsScript>().UpdateStats();
		}

		details.PlaySound();

		return true;
	}
}
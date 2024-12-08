using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PartyManager : MonoBehaviour
{
	public Transform characterInventory;
	public GameObject _characterPrefab;

	public Sprite unassignedIcon;

	private Transform selected;
	private int slot = -1;

	private Transform main;
	private Transform upgrades;

	private void SetNewMember()
	{
		// returns true if the character was already assigned
		if (characterInventory.GetComponent<CharacterManagerScript>().SetMember(selected, slot))
		{
			SetIconStats(null, slot);
		}
		selected = null;
		slot = -1;
		foreach (Transform unit in characterInventory)
		{
			UpdateCharacter(unit);
		}
	}

	private void SetIconStats(Transform unit, int characterSlot)
	{
		Transform character = transform.Find("Main").Find("Party").Find("PartyMember" + characterSlot).transform;
		if (unit == null)
		{
			character.Find("UnitName").GetComponent<TextMeshProUGUI>().text = "Unassigned";
			character.Find("Mask").Find("Icon").GetComponent<Image>().sprite = unassignedIcon;
			return;
		}
		character.Find("UnitName").GetComponent<TextMeshProUGUI>().text = unit.GetComponent<UnitStatsScript>().Character;
		character.Find("Mask").Find("Icon").GetComponent<Image>().sprite = unit.Find("UnitIcon").GetComponent<Image>().sprite;
	}

	public void ChooseMember(Transform unit)
	{
		selected = unit;
		if (slot != -1)
		{
			SetNewMember();
		}
	}

	public void ChooseSlot(int newSlot)
	{
		slot = newSlot;
		if (selected)
		{
			SetNewMember();
		}
	}

	private void UpdateCharacter(Transform unit)
	{
		Transform reassign = null;
		foreach(Transform character in transform.Find("Main").Find("Members"))
		{
			if (character.Find("UnitName").GetComponent<TextMeshProUGUI>().text == unit.GetComponent<UnitStatsScript>().Character)
			{
				reassign = character;
				break;
			}
		}
		if (!reassign)
		{
			reassign = Instantiate(_characterPrefab, transform.Find("Main").Find("Members")).transform;
		}
		reassign.Find("UnitName").GetComponent<TextMeshProUGUI>().text = unit.GetComponent<UnitStatsScript>().Character;
		reassign.Find("Mask").Find("Icon").GetComponent<Image>().sprite = unit.Find("UnitIcon").GetComponent<Image>().sprite;
		reassign.Find("Shield").GetComponent<TextMeshProUGUI>().text = "Shield: " + unit.GetComponent<UnitStatsScript>().Shield + "/4";
		reassign.Find("Health").GetComponent<TextMeshProUGUI>().text = "Health: " + unit.GetComponent<UnitStatsScript>().Health + "/" + unit.GetComponent<UnitStatsScript>().MaxHealth;
		reassign.GetComponent<Button>().onClick.RemoveAllListeners();
		reassign.GetComponent<Button>().onClick.AddListener(() => ChooseMember(unit));
		if (characterInventory.GetComponent<CharacterManagerScript>().PartyMember1 == unit)
		{
			SetIconStats(unit, 1);
		}
		if (characterInventory.GetComponent<CharacterManagerScript>().PartyMember2 == unit)
		{
			SetIconStats(unit, 2);
		}
		if (characterInventory.GetComponent<CharacterManagerScript>().PartyMember3 == unit)
		{
			SetIconStats(unit, 3);
		}
	}

	private void Awake()
	{
		main = transform.GetChild(1);
		upgrades = transform.GetChild(2);
		foreach (Transform unit in characterInventory)
		{
			UpdateCharacter(unit);
		}
	}

	private void OnEnable()
	{
		foreach (Transform unit in characterInventory)
		{
			UpdateCharacter(unit);
		}
	}

	public void SetTab(int state)
	{
		bool setState = gameObject.activeSelf;
		switch (state)
		{
			case 0:
				setState = false;
				break;
			case 1:
				setState = true;
				break;
			default:
				setState = !gameObject.activeSelf;
				break;
		}
		gameObject.SetActive(setState);
		foreach (Transform unit in characterInventory)
		{
			UpdateCharacter(unit);
		}
	}

	public void OpenCharacters(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			SetTab(2);
		}
	}

	public void AttemptUpgrade(string attribute)
	{
		if (!selected)
		{
			return;
		}
		UnitStatsScript stats = selected.GetComponent<UnitStatsScript>();
		UnitStatsScript original = ((GameObject)Resources.Load("Characters\\Player\\" + stats.Character)).GetComponent<UnitStatsScript>();
		if (stats.StatPoints <= 0)
		{
			return;
		}
		switch(attribute)
		{
			case "Health":
				if (stats.MaxHealth >= original.MaxHealth * 2)
				{
					return;
				}
				stats.MaxHealth += original.MaxHealth / 10;
				break;
			case "Speed":
				if (stats.Speed <= original.Speed - 1)
				{
					return;
				}
				stats.Speed -= 0.1f;
				break;
			case "Ultimate":
				if (stats.UltimateCharge <= original.UltimateCharge - 20)
				{
					return;
				}
				stats.UltimateCharge -= 2;
				break;
			case "Strength":
				if (stats.Strength >= original.Strength * 2)
				{
					return;
				}
				stats.Strength += original.Strength / 10;
				break;
			case "Defense":
				if (stats.Defense >= original.Defense * 2)
				{
					return;
				}
				stats.Defense += original.Defense / 10;
				break;
			case "Magic":
				if (stats.Magic >= original.Magic * 2 || original.Magic <= 0)
				{
					return;
				}
				stats.Magic += original.Magic / 10;
				break;
		}
		Debug.Log("You Upgraded " + attribute);
		stats.StatPoints--;
		OpenUpgrades();
	}

	public void OpenUpgrades()
	{
		//Transform character = characterInventory.Find(selected.Find("UnitName").GetComponent<TextMeshProUGUI>().text);
		if (!selected)
		{
			return;
		}
		UnitStatsScript stats = selected.GetComponent<UnitStatsScript>();
		UnitStatsScript original = ((GameObject)Resources.Load("Characters\\Player\\" + stats.Character)).GetComponent<UnitStatsScript>();

		upgrades.Find("Health").Find("Name").GetComponent<TextMeshProUGUI>().text = "Max Health: " + stats.MaxHealth;
		upgrades.Find("Health").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: +" + original.MaxHealth / 10;
		float doneUpgrade = (stats.MaxHealth / (original.MaxHealth * 2) - 0.5f) * 2;
		upgrades.Find("Health").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;
		upgrades.Find("Speed").Find("Name").GetComponent<TextMeshProUGUI>().text = "Speed: " + stats.Speed + " Seconds";
		upgrades.Find("Speed").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: -0.1 Second";
		doneUpgrade = (original.Speed - stats.Speed) / 10;
		upgrades.Find("Speed").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;
		upgrades.Find("Ultimate").Find("Name").GetComponent<TextMeshProUGUI>().text = "Ultimate Speed: " + stats.UltimateCharge + " Seconds";
		upgrades.Find("Ultimate").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: -2 Seconds";
		doneUpgrade = (original.UltimateCharge - stats.UltimateCharge) / 20;
		upgrades.Find("Ultimate").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;
		upgrades.Find("Strength").Find("Name").GetComponent<TextMeshProUGUI>().text = "Strength: " + stats.Strength;
		upgrades.Find("Strength").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: +" + original.Strength / 10;
		doneUpgrade = (stats.Strength / (original.Strength * 2) - 0.5f) * 2;
		upgrades.Find("Strength").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;
		upgrades.Find("Defense").Find("Name").GetComponent<TextMeshProUGUI>().text = "Defense: " + stats.Defense;
		upgrades.Find("Defense").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: +" + original.Defense / 10;
		doneUpgrade = (stats.Defense / (original.Defense * 2) - 0.5f) * 2;
		upgrades.Find("Defense").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;
		upgrades.Find("Magic").Find("Name").GetComponent<TextMeshProUGUI>().text = "SpecialPoints: " + stats.Magic;
		upgrades.Find("Magic").Find("Amount").GetComponent<TextMeshProUGUI>().text = "Stat Point: +" + original.Magic / 10;
		doneUpgrade = (stats.Magic / (original.Magic * 2) - 0.5f) * 2;
		upgrades.Find("Magic").Find("Fill").GetComponent<Image>().fillAmount = doneUpgrade;

		main.gameObject.SetActive(false);
		upgrades.gameObject.SetActive(true);
	}

	public void CloseUpgrades()
	{
		main.gameObject.SetActive(true);
		upgrades.gameObject.SetActive(false);
		selected = null;
		slot = -1;
	}
}

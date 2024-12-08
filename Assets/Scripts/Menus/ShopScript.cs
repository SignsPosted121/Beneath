using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopScript : MonoBehaviour
{

	private MapScript map;
	private List<UnitStatsScript> selectedUnits = new List<UnitStatsScript>(24);
	private Transform moneyTracker;
	public enum Services {Heal, Armor};
	public Services selectedService = 0;

	public int HealCost = 5;
	public int ArmorCost = 4;

	private Transform shop;
	private Transform memberSelect;

	private void Awake()
	{
		map = GameObject.FindGameObjectWithTag("GameController").GetComponent<MapScript>();
		shop = transform.GetChild(transform.childCount - 1);
		memberSelect = transform.GetChild(transform.childCount - 2);
		moneyTracker = transform.Find("Money").Find("Text");
	}

	private void OnEnable()
	{
		DataManager.SaveGame(false);
		SoundManager.singleton.StopAll(Sound.SoundClass.Music);
		SoundManager.singleton.PlaySound("Shop");
		CharacterManagerScript folder = map.Characters.GetComponent<CharacterManagerScript>();
		shop.Find("Heal").Find("Text").GetComponent<TextMeshProUGUI>().text = "Heal\n$" + HealCost;
		shop.Find("Armor").Find("Text").GetComponent<TextMeshProUGUI>().text = "Repair Armor\n$" + ArmorCost;
		moneyTracker.GetComponent<TextMeshProUGUI>().text = "$" + map.money;
		foreach (Transform oldUnit in memberSelect)
		{
			Destroy(oldUnit.gameObject);
		}
		foreach(Transform unit in folder.transform)
		{
			Transform member = Instantiate(transform.GetChild(0), memberSelect);
			member.name = unit.GetComponent<UnitStatsScript>().Character;
			member.Find("Icon").GetComponent<Image>().sprite = unit.Find("UnitIcon").GetComponent<Image>().sprite;
			member.Find("UnitName").GetComponent<TextMeshProUGUI>().text = member.name;
			member.gameObject.SetActive(true);
			member.GetComponent<Button>().onClick.AddListener(() => SelectUnit(member, unit));
		}
	}

	public void ExitShop()
	{
		if (shop.gameObject.activeSelf)
		{
			SoundManager.singleton.StopAll(Sound.SoundClass.Music);
			switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
			{
				case "Tutorial":
					break;
				default:
					SoundManager.singleton.PlaySound("Forrestia");
					break;
			}
			Destroy(gameObject);
		} else
		{
			shop.gameObject.SetActive(true);
			memberSelect.gameObject.SetActive(false);
			UnselectAllUnits();
		}
	}

	private void SelectUnit(Transform member, Transform unit)
	{
		bool found = false;
		for(int i = 0; i < selectedUnits.Count; i++)
		{
			if (selectedUnits[i] == unit.GetComponent<UnitStatsScript>())
			{
				found = true;
				break;
			}
		}
		if (!found)
		{
			selectedUnits.Add(unit.GetComponent<UnitStatsScript>());
			member.GetComponent<Image>().color = new Color(0.64f, 1, 0.64f);
		} else
		{
			selectedUnits.Remove(unit.GetComponent<UnitStatsScript>());
			member.GetComponent<Image>().color = new Color(1, 1, 1);
		}
		UpdateCostText();
	}

	private void UnselectAllUnits()
	{
		foreach (Transform member in memberSelect)
		{
			UnitStatsScript found = null;
			for (int i = 0; i < selectedUnits.Count; i++)
			{
				if (selectedUnits[i].Character == member.name)
				{
					found = selectedUnits[i];
					break;
				}
			}
			if (found)
			{
				SelectUnit(member, found.transform);
			}
		}
	}

	// General methods and methods using the member select panel

	private void SetPurhcaseText(string newText)
	{
		transform.Find("Purchase").Find("Text").GetComponent<TextMeshProUGUI>().text = newText;
	}

	private void UpdateCostText()
	{
		string text = "";

		switch(selectedService)
		{
			case Services.Heal:
				text = "Heal for $" + CostForHeal();
				break;
			case Services.Armor:
				text = "Repair for $" + CostForArmor();
				break;
		}

		SetPurhcaseText(text);
	}

	private int CostForHeal()
	{
		if (selectedUnits.Count <= 0)
		{
			return 0;
		}
		int total = 0;
		foreach (UnitStatsScript unit in selectedUnits)
		{
			total += (int)((1 - unit.Health / unit.MaxHealth) * HealCost);
		}
		return total;
	}

	private int CostForArmor()
	{
		if (selectedUnits.Count <= 0)
		{
			return 0;
		}
		int total = 0;
		foreach (UnitStatsScript unit in selectedUnits)
		{
			if (unit.Penetration > 0)
			{
				total += ArmorCost;
			}
		}
		return total;
	}

	private bool HealUnit(UnitStatsScript unit)
	{
		if (unit.Health < unit.MaxHealth)
		{
			unit.Health = unit.MaxHealth;
			return true;
		}
		return false;
	}

	private bool ArmorUnit(UnitStatsScript unit)
	{
		if (unit.Penetration > 0)
		{
			unit.Penetration = 0;
			return true;
		}
		return false;
	}

	public void Purchase()
	{
		CharacterManagerScript folder = map.Characters.GetComponent<CharacterManagerScript>();
		int cost = 0;
		bool featureUsed = false;
		switch (selectedService)
		{
			case Services.Heal:
				if (selectedUnits.Count > 0)
				{
					cost = CostForHeal();
					foreach (UnitStatsScript unit in selectedUnits)
					{
						if (HealUnit(unit))
						{
							featureUsed = true;
						}
					}
				}
				break;
			case Services.Armor:
				if (selectedUnits.Count > 0)
				{
					cost = CostForArmor();
					foreach (UnitStatsScript unit in selectedUnits)
					{
						if (ArmorUnit(unit))
						{
							featureUsed = true;
						}
					}
				}
				break;
		}
		if (!featureUsed)
		{
			return;
		}
		SetPurhcaseText("Please Select\nA Service");
		shop.gameObject.SetActive(true);
		memberSelect.gameObject.SetActive(false);
		map.money -= cost;
		moneyTracker.GetComponent<TextMeshProUGUI>().text = "$" + map.money;
		UnselectAllUnits();
	}

	// Methods used by the buttons in the shop

	public void Heal()
	{
		selectedService = Services.Heal;
		UpdateCostText();
		shop.gameObject.SetActive(false);
		memberSelect.gameObject.SetActive(true);
	}

	public void RepairArmor()
	{
		selectedService = Services.Armor;
		UpdateCostText();
		shop.gameObject.SetActive(false);
		memberSelect.gameObject.SetActive(true);
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitStatsScript : MonoBehaviour
{

	public string Character = "Ryab";
	public float Experience = 0;
	public int Level = 1;
	public int StatPoints = 0;
	public string Ultimate = "Full Force";
	public List<string> attacks = new List<string>();

	public float Health = 10;
	public float MaxHealth = 10;
	public int Shield = 0;
	public float Speed = 6;
	public float Strength = 20;
	public float Defense = 5;
	public float Magic = 20;
	public float CurrentMagic = 20;
	public float UltimateCharge = 60;
	public float Aggression = 0;
	public float Noticability = 10;

	public float Penetration = 0;
	public float CurrentSpeed = 0f;
	public float CurrentUltimateCharge = 0f;

	private IEnumerator PlayDamageIndicator()
	{
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = false;
		transform.Find("Health").Find("Masking").Find("Hurt").GetComponent<Image>().enabled = true;
		float deltaTime = 0;
		while (deltaTime < 1)
		{
			transform.Find("Health").Find("Masking").Find("Hurt").GetComponent<Image>().color = new Color(1, deltaTime, deltaTime, 1);
			yield return new WaitForEndOfFrame();
			deltaTime += Time.deltaTime;
		}
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = true;
		transform.Find("Health").Find("Masking").Find("Hurt").GetComponent<Image>().enabled = false;
	}

	private IEnumerator PlayHealIndicator()
	{
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = false;
		transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().enabled = true;
		float deltaTime = 0;
		while (deltaTime < 1)
		{
			transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().color = new Color(deltaTime, 1, deltaTime, 1);
			yield return new WaitForEndOfFrame();
			deltaTime += Time.deltaTime;
		}
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = true;
		transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().enabled = false;
	}

	public IEnumerator PlayBuffIndicator()
	{
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = false;
		transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().enabled = true;
		float deltaTime = 0;
		while (deltaTime < 1)
		{
			transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().color = new Color(1, 0.5f + deltaTime / 2, deltaTime, 1);
			yield return new WaitForEndOfFrame();
			deltaTime += Time.deltaTime;
		}
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = true;
		transform.Find("Health").Find("Masking").Find("Buffed").GetComponent<Image>().enabled = false;
	}

	public IEnumerator PlayDebuffIndicator()
	{
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = false;
		transform.Find("Health").Find("Masking").Find("Debuffed").GetComponent<Image>().enabled = true;
		float deltaTime = 0;
		while (deltaTime < 1)
		{
			transform.Find("Health").Find("Masking").Find("Debuffed").GetComponent<Image>().color = new Color(deltaTime, deltaTime, 1, 1);
			yield return new WaitForEndOfFrame();
			deltaTime += Time.deltaTime;
		}
		transform.Find("Health").Find("Masking").Find("UnitIcon").GetComponent<Image>().enabled = true;
		transform.Find("Health").Find("Masking").Find("Debuffed").GetComponent<Image>().enabled = false;
	}

	public void TakeDamage(float damage, int damType)
	{
		if (damType == 1)
		{
			Penetration += damage;
			Penetration = Mathf.Clamp(Penetration, 0, Defense);
			return;
		}
		if (Shield <= 0 || damage < 0)
		{
			Health -= damage;
			if (Health <= 0)
			{
				StartCoroutine(transform.parent.parent.GetComponent<CombatScript>().Death(transform));
				return;
			}
			if (damage < 0)
			{
				StartCoroutine(PlayHealIndicator());
			}
			else
			{
				StartCoroutine(PlayDamageIndicator());
			}
		} else if (damage > 0)
		{
			int totalDamage = Mathf.Clamp(Mathf.FloorToInt(damage / 10), 1, 4);
			Shield -= totalDamage;
			Shield = Mathf.Clamp(Shield, 0, 4);
			StartCoroutine(PlayDamageIndicator());
		}

		if (Health > MaxHealth)
		{
			Health = MaxHealth;
		}
	}

	public float GetDamage(UnitStatsScript enemy, float low, float high)
	{
		float enemyDefense = GetDefense(enemy);
		return Mathf.Clamp(Random.Range(GetStrength() * low, GetStrength() * high) - enemyDefense, 5, Mathf.Infinity);
	}

	private float GetDefense(UnitStatsScript target)
	{
		return target.Defense - target.Penetration;
	}

	private float GetStrength()
	{
		return Strength * Mathf.Clamp(Health / MaxHealth * 2, 0.5f, 1);
	}

	public Transform GetEnemies()
	{
		if (transform.parent.name == "Player")
		{
			return transform.parent.parent.Find("Enemy");
		} else
		{
			return transform.parent.parent.Find("Player");
		}
	}

	void OnEnable()
	{
		if (!transform.parent || (transform.parent.name != "Player" && transform.parent.name != "Enemy"))
		{
			return;
		}
		gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
		gameObject.GetComponent<Button>().onClick.AddListener(() => transform.parent.parent.GetComponent<CombatScript>().SelectTarget(transform));
		transform.Find("Health").Find("UnitName").GetComponent<TextMeshProUGUI>().text = Character;
		//transform.Find("Ultimate").Find("Attribute").GetComponent<TextMeshProUGUI>().text = "Ultimate: " + Ultimate;
	}

	public void UpdateStats()
	{
		if (Shield > 4)
		{
			Shield = 4;
		}
		transform.Find("Health").Find("Shield").GetComponent<Image>().fillAmount = (float)Shield / 4;
		transform.Find("Health").GetComponent<Image>().fillAmount = Health / MaxHealth;
		transform.Find("Health").GetComponent<Image>().color = new Color(1 - Health / MaxHealth, Health / MaxHealth, 0);
		transform.Find("Speed").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2(CurrentSpeed / Speed, 0.4f);
		transform.Find("Speed").Find("Amount").GetComponent<TextMeshProUGUI>().text = Mathf.Floor(CurrentSpeed / Speed * 100) + "%";
		transform.Find("Damage").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2(Mathf.Clamp(Health / MaxHealth * 2, 0.5f, 1), 0.4f);
		transform.Find("Damage").Find("Amount").GetComponent<TextMeshProUGUI>().text = Strength * Mathf.Clamp(Mathf.Floor(Health / MaxHealth * 100) / 50, 0.5f, 1) + "/" + Strength;
		if (Magic != 0)
		{
			transform.Find("Magic").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2(CurrentMagic / Magic, 0.4f);
			transform.Find("Magic").Find("Amount").GetComponent<TextMeshProUGUI>().text = Mathf.Floor(CurrentMagic) + "/" + Magic;
		}
		else
		{
			transform.Find("Magic").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2(0, 0.4f);
			transform.Find("Magic").Find("Amount").GetComponent<TextMeshProUGUI>().text = "No SP";
		}
		transform.Find("Protection").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2((Defense - Penetration) / Defense, 0.4f);
		transform.Find("Protection").Find("Amount").GetComponent<TextMeshProUGUI>().text = (Defense - Penetration) + "/" + Defense;
		//transform.Find("Ultimate").Find("Bar").GetComponent<Image>().fillAmount = CurrentUltimateCharge / UltimateCharge;
		//transform.Find("Ultimate").Find("Amount").GetComponent<TextMeshProUGUI>().text = Mathf.Floor(CurrentUltimateCharge / UltimateCharge * 100) + "%";
	}

	public void UpdateSpeed()
	{
		transform.Find("Speed").Find("Bar").GetComponent<Image>().rectTransform.anchorMax = new Vector2(CurrentSpeed / Speed, 0.4f);
		transform.Find("Speed").Find("Amount").GetComponent<TextMeshProUGUI>().text = Mathf.Floor(CurrentSpeed / Speed * 100) + "%";
	}
}

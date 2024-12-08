using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Action
{
	public string name;
	public bool needTarget;
	public string desc;
	public enum actionType { None, Damage, Buff, Debuff, Healing };
	public actionType currentType = 0;
	public int magicCost;

	public void PlaySound()
	{
		switch (currentType)
		{
			case actionType.Damage:
				SoundManager.singleton.PlaySound("Attack");
				break;
			case actionType.Healing:
				SoundManager.singleton.PlaySound("Heal");
				break;
			case actionType.Buff:
				SoundManager.singleton.PlaySound("Buff");
				break;
			case actionType.Debuff:
				SoundManager.singleton.PlaySound("Debuff");
				break;
			default:
				break;
		}
	}
}

public class ActionDescription : MonoBehaviour
{
	[SerializeField] public List<Action> actions = new List<Action>();

	public Action FindAction(string action)
	{
		foreach (Action detail in actions)
		{
			if (detail.name == action.Replace(" ", ""))
			{
				return detail;
			}
		}
		return null;
	}
}
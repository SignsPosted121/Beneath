using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManagerScript : MonoBehaviour
{

	[Range(1, 3)] public int PartyNumber = 1;
	public Transform PartyMember1;
	public Transform PartyMember2;
	public Transform PartyMember3;
	public bool isBoss = false;

	private void RecountParty()
	{
		PartyNumber = 0;
		if (PartyMember1)
		{
			PartyNumber++;
		}
		if (PartyMember2)
		{
			PartyNumber++;
		}
		if (PartyMember3)
		{
			PartyNumber++;
		}
	}

	private bool IsAssigned(Transform member)
	{
		if (PartyMember1 == member || PartyMember2 == member || PartyMember3 == member)
		{
			return true;
		}
		return false;
	}

	public bool SetMember(Transform newMember, int replace)
	{
		bool found = false;
		if (IsAssigned(newMember))
		{
			if (replace > 1)
			{
				newMember = null;
				found = true;
			}
		}
		switch (replace)
		{
			default:
				PartyMember1 = newMember;
				break;
			case 2:
				PartyMember2 = newMember;
				break;
			case 3:
				PartyMember3 = newMember;
				break;
		}
		RecountParty();
		return found;
	}

	public void SetMember(string memberName, int replace)
	{
		Transform newMember = null;
		foreach(Transform character in transform)
		{
			if (character.GetComponent<UnitStatsScript>().Character == memberName)
			{
				newMember = character;
				break;
			}
		}
		if (!newMember || IsAssigned(newMember))
		{
			return;
		}
		switch (replace)
		{
			default:
				PartyMember1 = newMember;
				break;
			case 2:
				PartyMember2 = newMember;
				break;
			case 3:
				PartyMember3 = newMember;
				break;
		}
		RecountParty();
	}
}
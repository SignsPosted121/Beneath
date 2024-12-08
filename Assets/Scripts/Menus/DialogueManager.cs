using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class Speech
{

	public string speaker;
	public enum stance { left, leftMinor, right, rightMinor };
	public stance staging = 0;

	public string text;

}

public class DialogueManager : MonoBehaviour
{

	[SerializeField] public List<Speech> lines = new List<Speech>();

	private int currentIndex = 0;
	private bool typingOut = false;

	private PlayerInput input;

	private Transform leftPosition;
	private Transform leftMPosition;
	private Transform rightPosition;
	private Transform rightMPosition;

	private void Awake()
	{
		input = GameObject.FindGameObjectWithTag("GameController").GetComponent<PlayerInput>();
		input.currentActionMap.actionTriggered += Next;
		StartDialogue();
	}

	private void OnDestroy()
	{
		if (input != null)
		{
			input.currentActionMap.actionTriggered -= Next;
		}
	}

	public void StartDialogue()
	{
		GetComponent<Canvas>().enabled = true;
		StartCoroutine(ParseLetters(lines[0]));
	}

	public void Next(InputAction.CallbackContext ctx)
	{
		if (this == null)
		{
			return;
		}
		if (ctx.action.name == "MouseDown" && ctx.performed)
		{
			if (typingOut)
			{
				transform.Find("Prompt").Find("Message").GetComponent<TextMeshProUGUI>().text = lines[currentIndex].text;
				return;
			} else
			{
				if (currentIndex < lines.Count - 1)
				{
					currentIndex++;
					StartCoroutine(ParseLetters(lines[currentIndex]));
				} else
				{
					Destroy(gameObject);
				}
			}
		}
	}

	private void RemoveCharacter(Transform actor)
	{
		for(int i = 0; i < characters.Count; i++)
		{
			if (characters[i] == actor.name)
			{
				characters.RemoveAt(i);
				break;
			}
		}
		Destroy(actor.gameObject);
	}

	private void StageCharacter(Transform character, Speech.stance staging)
	{
		switch (staging)
		{
			default:
				if (leftPosition && leftPosition != character)
				{
					if (leftPosition.name.Contains("?"))
					{
						RemoveCharacter(leftPosition);
					}
					else
					{
						Transform temp = leftPosition;
						leftPosition = character;
						if (leftPosition == leftMPosition)
						{
							leftMPosition = null;
						}
						StageCharacter(temp, Speech.stance.leftMinor);
					}
				}
				leftPosition = character;
				character.GetComponent<RectTransform>().anchorMin = new Vector2(0.1f, 0f);
				character.GetComponent<RectTransform>().anchorMax = new Vector2(0.25f, 1f);
				character.GetComponent<RectTransform>().localScale = Vector3.one;
				break;
			case Speech.stance.leftMinor:
				if (leftMPosition && leftMPosition != character)
				{
					RemoveCharacter(leftMPosition);
				}
				leftMPosition = character;
				character.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
				character.GetComponent<RectTransform>().anchorMax = new Vector2(0.15f, 0.75f);
				character.GetComponent<RectTransform>().localScale = Vector3.one;
				break;
			case Speech.stance.right:
				if (rightPosition && rightPosition != character)
				{
					if (rightPosition.name.Contains("?"))
					{
						RemoveCharacter(rightPosition);
					}
					else
					{
						Transform temp = rightPosition;
						rightPosition = character;
						if (rightPosition == rightMPosition)
						{
							rightMPosition = null;
						}
						StageCharacter(temp, Speech.stance.rightMinor);
					}
				}
				rightPosition = character;
				character.GetComponent<RectTransform>().anchorMin = new Vector2(0.75f, 0f);
				character.GetComponent<RectTransform>().anchorMax = new Vector2(0.9f, 1f);
				character.GetComponent<RectTransform>().localScale = new Vector3(-1, 1, 1);
				break;
			case Speech.stance.rightMinor:
				if (rightMPosition && rightMPosition != character)
				{
					RemoveCharacter(rightMPosition);
				}
				rightMPosition = character;
				character.GetComponent<RectTransform>().anchorMin = new Vector2(0.85f, 0f);
				character.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.75f);
				character.GetComponent<RectTransform>().localScale = new Vector3(-1, 1, 1);
				break;
		}
		character.GetComponent<RectTransform>().offsetMin = Vector2.zero;
		character.GetComponent<RectTransform>().offsetMax = Vector2.zero;
	}

	private void ReplaceSpeaker(string actor)
	{
		transform.Find("Prompt").Find("Actor").GetComponent<Image>().sprite = transform.Find("Characters").Find(actor).GetComponent<Image>().sprite;
	}

	private void NewCharacter(string character, Speech.stance staging)
	{
		Transform actor = Instantiate(transform.Find("Characters").Find(character), transform.Find("Prompt").Find("Actors"));
		actor.name = actor.name.Replace("(Clone)", "");
		characters.Add(character);
		StageCharacter(actor, staging);
	}

	private List<string> characters = new List<string>();

	public IEnumerator ParseLetters(Speech line)
	{
		typingOut = true;
		Transform prompt = transform.Find("Prompt");
		prompt.Find("Speaker").GetComponent<TextMeshProUGUI>().text = line.speaker + ":";
		prompt.Find("Message").GetComponent<TextMeshProUGUI>().text = "";
		if (characters.Contains(line.speaker))
		{
			Transform character = prompt.Find("Actors").Find(line.speaker);
			StageCharacter(character, line.staging);
		} else
		{
			NewCharacter(line.speaker, line.staging);
		}
		ReplaceSpeaker(line.speaker);
		while (prompt.Find("Message").GetComponent<TextMeshProUGUI>().text.Length < line.text.Length)
		{
			if (prompt.Find("Message").GetComponent<TextMeshProUGUI>().text.Length >= line.text.Length - 1)
			{
				prompt.Find("Message").GetComponent<TextMeshProUGUI>().text = line.text;
				break;
			}
			prompt.Find("Message").GetComponent<TextMeshProUGUI>().text = line.text.Remove(prompt.Find("Message").GetComponent<TextMeshProUGUI>().text.Length + 1);
			yield return new WaitForSeconds(0.02f);
		}
		typingOut = false;
	}

}

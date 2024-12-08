using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
	private int ExitingState = 0;

	public void OpenMenu(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			OpenMenu("Main");
			gameObject.SetActive(!gameObject.activeSelf);
		}
	}

	private void CheckSaveButton(Transform button)
	{
		if (CombatScript.singleton.gameObject.activeSelf)
		{
			button.GetComponent<Button>().interactable = false;
		}
		else
		{
			button.GetComponent<Button>().interactable = true;
		}
	}

	public void OpenMenu(string ID)
	{
		foreach(Transform menu in transform)
		{
			if (menu.name != "Background" && menu.name != "Title")
			{
				menu.gameObject.SetActive(false);
			}
		}

		CheckSaveButton(transform.Find("Main").Find("Save"));

		switch (ID)
		{
			case "Exiting":
				transform.Find("Exiting").gameObject.SetActive(true);
				CheckSaveButton(transform.Find("Exiting").Find("Save"));
				break;
			case "Main":
				transform.Find("Main").gameObject.SetActive(true);
				break;
			default:
				transform.Find("Main").gameObject.SetActive(true);
				gameObject.SetActive(!gameObject.activeSelf);
				break;
		}
	}

	public void SaveGame()
	{
		DataManager.SaveGame(false);
	}

	public void Exit(bool save)
	{
		SoundManager.singleton.StopAll();
		if (ExitingState == 0)
		{
			StartCoroutine(QuitToMenu(save));
		}
		else
		{
			QuitGame(save);
		}
	}

	public void SelectExitOption(int newState)
	{
		ExitingState = newState;
		OpenMenu("Exiting");
	}

	private IEnumerator QuitToMenu(bool save)
	{
		if (save) {
			DataManager.SaveGame(false);
		}
		yield return new WaitForEndOfFrame();
		while (DataManager.saving)
		{
			yield return new WaitForEndOfFrame();
		}
		SceneManager.LoadScene(0);
	}

	private  void QuitGame(bool save)
	{
		if (save)
		{
			DataManager.SaveGame(true);
		} else
		{
			Application.Quit();
		}
	}

}
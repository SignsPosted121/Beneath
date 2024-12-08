using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

	public void ContinueGame()
	{
		DataManager.singleton.StartCoroutine(DataManager.singleton.LoadGame(DataManager.LoadData()));
	}

	public void StartTutorial()
	{
		SceneManager.LoadScene(1);
	}

	public void NewGame()
	{
		DataManager.DeleteGame();
		SceneManager.LoadScene(2);
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public static void ReturnToMainMenu()
	{
		SceneManager.LoadScene(0);
	}

}

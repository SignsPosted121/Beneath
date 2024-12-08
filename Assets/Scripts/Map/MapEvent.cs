using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapEvent : MonoBehaviour
{

    public string specificName = "Generic";
    public string[] requiredEvents;
    public enum EventTypes {Dialogue, Cutscene, Town, Quest, Shop, None};
    public EventTypes _event;
    public bool triggered = false;
    public CharacterManagerScript leadInFight;
    public int leadInScene = -1;
    public bool loadDataFromChange = true;
    public Transform destroyBarricade;
    public string addCharacter;

    private Transform openedLog = null;

    private void Awake()
    {
        switch (_event)
        {
            case EventTypes.Dialogue:
                Destroy(transform.Find("Cover").gameObject);
                break;
            case EventTypes.Shop:
                transform.Find("ShopIcon").GetComponent<SpriteRenderer>().enabled = true;
                break;
            default:
                Destroy(transform.Find("Cover").gameObject);
                break;
        }
    }

    private IEnumerator PauseForThings()
	{
        while(openedLog != null)
		{
            yield return new WaitForEndOfFrame();
		}
        if (leadInFight)
		{
            MapScript.singleton.InitiateCombat(leadInFight);
            while (CombatScript.singleton.gameObject.activeSelf)
			{
                yield return new WaitForEndOfFrame();
			}
		}
        if (leadInScene != -1)
        {
            MapScript.singleton.enabled = false;
            if (loadDataFromChange)
            {
                bool saved = DataManager.SaveGame(false);
                while (!saved)
                {
                    yield return new WaitForEndOfFrame();
                }
                DataManager.singleton.StartCoroutine(DataManager.singleton.LoadGame(DataManager.LoadData(), leadInScene));
            } else
			{
                SceneManager.LoadScene(2);
			}
        }
        if (addCharacter != "")
		{
            Transform newCharacter = Instantiate((GameObject)Resources.Load("Characters\\Player\\" + addCharacter), CombatScript.singleton.characterInventory).transform;
            newCharacter.name = newCharacter.name.Replace("(Clone)", "");
            Debug.Log("Player aquired " + addCharacter + "!");
		}
        yield break;
    }

    public void TriggerEvent()
    {
		if (triggered)
		{
			return;
        }

        if (requiredEvents.Length > 0)
        {
            int achieved = 0;

            foreach (string Event in requiredEvents)
            {
                foreach (Transform worldEvent in transform.parent)
                {
                    if (worldEvent.GetComponent<MapEvent>().specificName == Event)
                    {
                        achieved++;
                        if (!worldEvent.GetComponent<MapEvent>().triggered)
						{
                            return;
						}
                    }
                }
                if (achieved >= requiredEvents.Length)
                {
                    break;
                }
            }

            if (achieved < requiredEvents.Length)
            {
                string[] events = DataManager.ReturnTriggeredEventsList();

                if (events.Length <= 0)
                {
                    return;
                }

                foreach (string Event in events)
                {
                    foreach (string required in requiredEvents)
                    {
                        if (required == Event)
                        {
                            achieved++;
                        }
                    }
                    if (achieved >= requiredEvents.Length)
                    {
                        break;
                    }
                }
            }

            if (achieved < requiredEvents.Length)
            {
                return;
            }
        }
        switch(_event)
        {
            case EventTypes.Dialogue:
                openedLog = OpenDialogue();
                triggered = true;
                break;
            case EventTypes.Shop:
                OpenShop();
                break;
            default:
                break;
        }
        if (triggered)
		{
            gameObject.GetComponent<Collider2D>().enabled = false;
		}
        if (destroyBarricade)
		{
            Destroy(destroyBarricade.gameObject);
		}
        StartCoroutine(PauseForThings());
    }

    public void OpenShop()
    {
        Transform shopMenu = Instantiate((GameObject)Resources.Load("Shop"), transform.parent.parent.parent).transform;
    }

    public Transform OpenDialogue()
    {
        Transform dialogue = transform.GetChild(transform.childCount - 1);
        dialogue.SetParent(transform.parent.parent.parent);
        dialogue.gameObject.SetActive(true);
        return dialogue;
    }
}

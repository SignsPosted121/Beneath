using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public class EnemyDataStruct
{
    public string[] characters = new string[1];
    public float[] enemyPos = new float[3];
    public float[] startPos = new float[3];
    public float[] AIstats = new float[3];
    public float[] baseDrops = new float[4];
}

[System.Serializable]
public class PlayerDataStruct
{
    public float[] playerPos;
    public float[][] partyStats;
    public string[][] partyStrings;
    public string[][] partyAttacks;
    public string[] partyComp;

    public bool CreateData()
    {
        playerPos = new float[2];
        playerPos[0] = MapScript.singleton.playerPos.x;
        playerPos[1] = MapScript.singleton.playerPos.y;
        partyStats = new float[MapScript.singleton.Characters.childCount][];
        partyStrings = new string[MapScript.singleton.Characters.childCount][];
        partyAttacks = new string[MapScript.singleton.Characters.childCount][];
        for (int i = 0; i < MapScript.singleton.Characters.childCount; i++)
        {
            partyStats[i] = DataManager.CondenseStats(MapScript.singleton.Characters.GetChild(i).GetComponent<UnitStatsScript>());
            partyStrings[i] = DataManager.CondenseStrings(MapScript.singleton.Characters.GetChild(i).GetComponent<UnitStatsScript>());
            partyAttacks[i] = MapScript.singleton.Characters.GetChild(i).GetComponent<UnitStatsScript>().attacks.ToArray();
        }
        partyComp = new string[MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyNumber];
        for (int i = 0; i < partyComp.Length; i++)
		{
            switch(i)
            {
                default:
                    partyComp[0] = MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember1.GetComponent<UnitStatsScript>().Character;
                    break;
                case 1:
                    partyComp[1] = MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember2.GetComponent<UnitStatsScript>().Character;
                    break;
                case 2:
                    partyComp[2] = MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember3.GetComponent<UnitStatsScript>().Character;
                    break;
            }
		}
        return true;
	}
}

[System.Serializable]
public class SaveFile
{
    public string[] triggeredNames;
    public EnemyDataStruct[] enemies;
    public PlayerDataStruct player;
    public int money;
    public float worldTime;
    public string levelName;
    public byte saveVersion;

    public SaveFile(string[] triggeredData, EnemyDataStruct[] enemyData, PlayerDataStruct playerData, int moneyData, float timeData, string levelNameData, byte saveVersionData)
	{
        enemies = enemyData;
        player = playerData;
        money = moneyData;
        worldTime = timeData;
        levelName = levelNameData;
        saveVersion = saveVersionData;

        string[] preNames = DataManager.ReturnTriggeredEventsList();
        if (preNames == null)
		{
            triggeredNames = triggeredData;
            return;
        }
        foreach(string _event in triggeredData)
		{
            bool found = false;
            foreach(string pre_event in preNames)
			{
                if (_event == pre_event)
				{
                    found = true;
				}
			}
            if (!found)
			{
                string[] newPre = new string[preNames.Length + 1];
                for (int i = 0; i < preNames.Length; i++)
				{
                    newPre[i] = preNames[i];
				}
                newPre[newPre.Length - 1] = _event;
                preNames = newPre;
			}
		}
        triggeredNames = preNames;
    }
}

public class DataManager : MonoBehaviour
{

    public static DataManager singleton;
    public static string saveFile = "MyGame";
    private const byte _SaveVersion = 2;
    public static bool saving = false;

    public static string[] ReturnTriggeredEventsList()
	{
        SaveFile _data = LoadData();
        if (_data == null)
		{
            return null;
		}
        return _data.triggeredNames;
	}

    public static float[] CondenseStats(UnitStatsScript unit)
    {
        float[] concentrate = new float[13];
        concentrate[0] = unit.Experience;
        concentrate[1] = unit.Level;
        concentrate[2] = unit.StatPoints;
        concentrate[3] = unit.Health;
        concentrate[4] = unit.MaxHealth;
        concentrate[5] = unit.Speed;
        concentrate[6] = unit.Strength;
        concentrate[7] = unit.Defense;
        concentrate[8] = unit.Magic;
        concentrate[9] = unit.CurrentMagic;
        concentrate[10] = unit.UltimateCharge;
        concentrate[11] = unit.Penetration;
        concentrate[12] = unit.CurrentUltimateCharge;
        return concentrate;
    }

    public static string[] CondenseStrings(UnitStatsScript unit)
    {
        string[] concentrate = new string[6];
        concentrate[0] = unit.Character;
        concentrate[1] = unit.Ultimate;
        // we have to rebuild the save system to reserialize the UnitStatsScript so we can save the new array of strings
        return concentrate;
    }

    public static bool DeleteGame()
	{
        return DeleteGame(saveFile);
	}

    public static bool DeleteGame(string specificFile)
    {
        string filePath = Application.persistentDataPath + "/Saves/" + specificFile;

        if (!File.Exists(filePath))
        {
            Debug.LogWarning(filePath + " does not exist.");
            return false;
        }

        File.Delete(filePath);

        return true;
    }

    public static bool SaveGame(bool quitAfter)
    {
        //  Save the player position
        //  Save events that have been fired
        //  Save enemy positions and ids
        //  Save player's party stats (probably just make a function to save each one into an array and save said array)
        //  Save anything extra like the time and level
        //  When you save the level, make sure you save the name AND the #
        //  Finally save the save version in case we ever need to flush out the player's data for a save system change
        //  Load all of this into a SAVstructure and serialize the class for simple data organization
        saving = true;

        string[] triggeredData = new string[MapScript.singleton.transform.Find("Events").childCount];
        for (int i = 0; i < triggeredData.Length; i++)
		{
            if (MapScript.singleton.transform.Find("Events").GetChild(i).GetComponent<MapEvent>().triggered)
            {
                triggeredData[i] = MapScript.singleton.transform.Find("Events").GetChild(i).GetComponent<MapEvent>().specificName;
            }
        }
        EnemyDataStruct[] enemyData = new EnemyDataStruct[MapScript.singleton.transform.Find("RoamingEnemies").childCount];
        for (int i = 0; i < enemyData.Length; i++)
        {
            CharacterManagerScript enemy = MapScript.singleton.transform.Find("RoamingEnemies").GetChild(i).Find("Party").GetComponent<CharacterManagerScript>();
            MapAIUnit mapUnit = MapScript.singleton.transform.Find("RoamingEnemies").GetChild(i).GetComponent<MapAIUnit>();
            enemyData[i] = new EnemyDataStruct();
            enemyData[i].characters = new string[enemy.PartyNumber];
            enemyData[i].characters[0] = enemy.PartyMember1.GetComponent<UnitStatsScript>().Character;
            if (enemy.PartyMember2)
            {
                enemyData[i].characters[1] = enemy.PartyMember2.GetComponent<UnitStatsScript>().Character;
            }
            if (enemy.PartyMember3)
            {
                enemyData[i].characters[2] = enemy.PartyMember3.GetComponent<UnitStatsScript>().Character;
            }
            float[] enemyPos = { enemy.transform.parent.position.x, enemy.transform.parent.position.y, enemy.transform.parent.position.z };
            enemyData[i].enemyPos = enemyPos;
            float[] startingPoint = { mapUnit.startingPoint.x, mapUnit.startingPoint.y, mapUnit.startingPoint.z };
            enemyData[i].startPos = startingPoint;
            enemyData[i].AIstats[0] = mapUnit.viewingRange;
            enemyData[i].AIstats[1] = mapUnit.patrolRange;
            enemyData[i].AIstats[2] = mapUnit.movement;
            enemyData[i].baseDrops[0] = mapUnit.GetComponent<LootTable>().coinDrop;
            enemyData[i].baseDrops[1] = mapUnit.GetComponent<LootTable>().coinVary;
            enemyData[i].baseDrops[2] = mapUnit.GetComponent<LootTable>().xpDrop;
            enemyData[i].baseDrops[3] = mapUnit.GetComponent<LootTable>().xpVary;
        }
        int money = MapScript.singleton.money;
        float timeData = MapScript.singleton.DayTime;
        string levelNameData = SceneManager.GetActiveScene().name;
        PlayerDataStruct playerData = new PlayerDataStruct();
        playerData.CreateData();
        SaveFile _data = new SaveFile(triggeredData, enemyData, playerData, money, timeData, levelNameData, _SaveVersion);

        //Yucky shit that serializes and writes the file to the disk

        string filePath = Application.persistentDataPath + "/Saves";

        if (!Directory.Exists(filePath))
		{
            Directory.CreateDirectory(filePath);
        }
        filePath += "/" + saveFile;
        FileStream _stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        BinaryFormatter _bf = new BinaryFormatter();
        _bf.Serialize(_stream, _data);

        _stream.Close();

        saving = false;

        Debug.Log("Game was saved as: " + saveFile);

        if (quitAfter)
		{
            Application.Quit();
        }
        return true;
    }

    public static SaveFile LoadData()
    {
        return LoadData(saveFile);
    }

    public static SaveFile LoadData(string specificFile)
    {
        string filePath = Application.persistentDataPath + "/Saves/" + specificFile;

        if (!File.Exists(filePath))
        {
            Debug.LogWarning(filePath + " does not exist.");
            return null;
        }
        FileStream _stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        BinaryFormatter _bf = new BinaryFormatter();
        SaveFile _data = _bf.Deserialize(_stream) as SaveFile;

        _stream.Close();

        return _data;
    }

    public IEnumerator LoadGame(SaveFile _data)
    {
        StartCoroutine(LoadGame(_data, -1));
        yield return null;
    }

    public IEnumerator LoadGame(SaveFile _data, int _scene)
    {
        //  If the save version is different than the hardcoded one, immediately return and warn the user they will need to revert teir version if they want to play this file
        //  Load the level and time
        //  When you load the level, make sure you try loading by name FIRST and then #
        //  Turn off events that have been fired
        //  Load enemies by their ids and positions
        //  Load the player's party and their stats by an array
        //  Load where the player is
        //  Set camera's position there

        bool wasLastScene = false;
        Debug.Log("Loading data: " + saveFile);

        if (_data.saveVersion != _SaveVersion)
        {
            Debug.LogError("You have attempted opening a file that was created for a different save system. Very unwise.");
            yield return null;
        }

        AsyncOperation sceneLoading = null;
        if (_scene == -1)
        {
            if (SceneManager.GetSceneByName(_data.levelName) != null)
            {
                sceneLoading = SceneManager.LoadSceneAsync(_data.levelName);
                wasLastScene = true;
            }
            else
            {
                Debug.LogError("Couldn't find the scene you claimed to be in. You sly dog, you modified the game didn't you?");
                yield return null;
            }
        } else
		{
            sceneLoading = SceneManager.LoadSceneAsync(_scene);
        }

        while (!sceneLoading.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        SoundManager.singleton.StopAll();

        foreach (Transform _event in MapScript.singleton.transform.Find("Events"))
        {
            bool found = false;
            foreach (string eventName in _data.triggeredNames) {
                if (_event.GetComponent<MapEvent>().specificName == eventName)
				{
                    found = true;
				}
            }
            if (found && _event.GetComponent<MapEvent>().name != "Generic")
			{
                _event.GetComponent<MapEvent>().triggered = true;
                _event.GetComponent<Collider2D>().enabled = false;
                if (_event.GetComponent<MapEvent>().destroyBarricade)
				{
                    Destroy(_event.GetComponent<MapEvent>().destroyBarricade.gameObject);
				}
			}
        }

        MapScript.singleton.money = _data.money;
        MapScript.singleton.DayTime = _data.worldTime;
        PlayerDataStruct playerData = _data.player;

        if (wasLastScene)
        {
            foreach (EnemyDataStruct enemyParty in _data.enemies)
            {
                Vector3 enemyPos = new Vector3(enemyParty.enemyPos[0], enemyParty.enemyPos[1], enemyParty.enemyPos[2]);
                Transform enemy = MapScript.singleton.SpawnEnemy(enemyParty.characters, enemyPos);
                MapAIUnit ai = enemy.GetComponent<MapAIUnit>();
                LootTable table = enemy.GetComponent<LootTable>();
                Vector3 startingPoint = new Vector3(enemyParty.startPos[0], enemyParty.startPos[1], enemyParty.startPos[2]);
                ai.startingPoint = startingPoint;
                ai.viewingRange = enemyParty.AIstats[0];
                ai.patrolRange = enemyParty.AIstats[1];
                ai.movement = enemyParty.AIstats[2];
                table.coinDrop = enemyParty.baseDrops[0];
                table.coinVary = enemyParty.baseDrops[1];
                table.xpDrop = enemyParty.baseDrops[2];
                table.xpVary = enemyParty.baseDrops[3];
            }
        }

        foreach(string[] unitStrings in playerData.partyStrings)
		{
            Transform finding = CombatScript.singleton.characterInventory.Find(unitStrings[0]);
            if (!finding)
            {
                finding = Instantiate((GameObject)Resources.Load("Characters\\Player\\" + unitStrings[0]), MapScript.singleton.Characters).transform;
                finding.name = finding.name.Replace("(Clone)", "");
            }
            UnitStatsScript unit = finding.GetComponent<UnitStatsScript>();
            unit.Ultimate = unitStrings[1];
            // Uncondense somehow
        }

        for (int i = 0; i < playerData.partyStats.Length; i++)
		{
            float[] unitStats = playerData.partyStats[i];
            UnitStatsScript unit = MapScript.singleton.Characters.GetChild(i).GetComponent<UnitStatsScript>();
            unit.Experience = unitStats[0];
            unit.Level = Mathf.RoundToInt(unitStats[1]);
            unit.StatPoints = Mathf.RoundToInt(unitStats[2]);
            unit.Health = Mathf.RoundToInt(unitStats[3]);
            unit.MaxHealth = Mathf.RoundToInt(unitStats[4]);
            unit.Speed = unitStats[5];
            unit.Strength = Mathf.RoundToInt(unitStats[6]);
            unit.Defense = Mathf.RoundToInt(unitStats[7]);
            unit.Magic = Mathf.RoundToInt(unitStats[8]);
            unit.CurrentMagic = Mathf.RoundToInt(unitStats[9]);
            unit.UltimateCharge = unitStats[10];
            unit.Penetration = Mathf.RoundToInt(unitStats[11]);
            unit.CurrentUltimateCharge = unitStats[12];
            for (int a = 0; a < playerData.partyAttacks[i].Length; a++)
			{
                unit.attacks = new List<string>(playerData.partyAttacks[i]);
			}
        }

        MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyNumber = playerData.partyComp.Length;
        for(int i = 0; i < playerData.partyComp.Length; i++)
		{
            Transform foundCharacter = null;
            foreach (Transform character in MapScript.singleton.Characters)
			{
                if (character.GetComponent<UnitStatsScript>().Character == playerData.partyComp[i])
				{
                    foundCharacter = character;
				}
			}
            switch(i)
            {
                default:
                    MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember1 = foundCharacter;
                    break;
                case 1:
                    MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember2 = foundCharacter;
                    break;
                case 2:
                    MapScript.singleton.Characters.GetComponent<CharacterManagerScript>().PartyMember3 = foundCharacter;
                    break;
            }
		}

        MapScript.singleton.playerPos = new Vector3(playerData.playerPos[0], playerData.playerPos[1]);
        MapScript.singleton.transform.Find("Player").position = new Vector3(playerData.playerPos[0], playerData.playerPos[1]);
        Camera.main.transform.position = new Vector3(playerData.playerPos[0], playerData.playerPos[1], -10);

        Debug.Log("Loaded file: " + saveFile);

        yield return null;
    }

    private void Awake()
    {
        if (singleton)
		{
            Debug.LogWarning("Detected 2 Data Managers. Say goodbye to this one: " + gameObject.name);
            Destroy(gameObject);
            return;
		}
        singleton = this;
        DontDestroyOnLoad(gameObject);
        // We need to save values on the Map Ai Unit script
        // We also might want to save XP but that's optional honestly
    }
}

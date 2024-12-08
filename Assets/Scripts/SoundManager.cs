using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{

	public enum SoundClass {Master, Music, Effect, Ambient};

	public string soundId;
	public AudioClip clip;
	public SoundClass level = 0;
	[Range(0, 1)] public float volume = 1;
	[Range(0, 3)] public float pitch = 1;
	public bool loop = false;

	[HideInInspector]
	public AudioSource source;

}

public class SoundManager : MonoBehaviour
{

	public static SoundManager singleton;

	public List<Sound> songs;
	public List<Sound> sounds;

	public void StopAll(Sound.SoundClass level)
	{
		if (level == Sound.SoundClass.Music)
		{
			foreach (Sound song in songs)
			{
				song.source.Stop();
			}
		}
		else
		{
			foreach (Sound sound in sounds)
			{
				if (sound.level == level)
				{
					sound.source.Stop();
				}
			}
		}
	}

	public void StopAll()
	{
		StopAll(Sound.SoundClass.Master);
		StopAll(Sound.SoundClass.Music);
		StopAll(Sound.SoundClass.Effect);
		StopAll(Sound.SoundClass.Ambient);
	}

	public void PlaySound(string soundId)
	{
		foreach (Sound song in songs)
		{
			if (song.soundId == soundId)
			{
				song.source.Play();
			}
		}
		foreach (Sound sound in sounds)
		{
			if (sound.soundId == soundId)
			{
				sound.source.Play();
			}
		}
	}

	public void StopSound(string soundId)
	{
		// You didn't write this code
	}

	private void Awake()
	{
		if (singleton)
		{
			Debug.LogWarning("Detected 2 Sound Managers. Say goodbye to this one: " + gameObject);
			Destroy(gameObject);
			return;
		}
		singleton = this;
		foreach (Sound song in songs)
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.clip = song.clip;
			source.volume = song.volume; // Here we can adjust the volume based off of what the player decides!
			source.pitch = song.pitch;
			source.loop = song.loop;
			source.playOnAwake = false;

			song.source = source;
		}
		foreach (Sound sound in sounds)
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.clip = sound.clip;
			source.volume = sound.volume; // Here we can adjust the volume based off of what the player decides!
			source.pitch = sound.pitch;
			source.loop = sound.loop;
			source.playOnAwake = false;

			sound.source = source;
		}
	}

}

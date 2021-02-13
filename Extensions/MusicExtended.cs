// Music
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Music : MonoBehaviour, ISaveLoad
{
	private const int m_NumMusicTracks = 2;

	[HideInInspector]
	public AudioSource[] m_Source = new AudioSource[2];

	private static Music s_Instance;

	private bool m_Initialized;

	private Dictionary<string, string> m_MusicsMap = new Dictionary<string, string>();

	private Dictionary<int, MusicScheduleData> m_Scheduled = new Dictionary<int, MusicScheduleData>();

	public static Music Get()
	{
		return s_Instance;
	}

	private void Awake()
	{
		s_Instance = this;
		ScriptParser scriptParser = new ScriptParser();
		scriptParser.Parse("MusicsMap.txt");
		for (int i = 0; i < scriptParser.GetKeysCount(); i++)
		{
			Key key = scriptParser.GetKey(i);
			m_MusicsMap[key.GetVariable(0).SValue] = key.GetVariable(1).SValue;
		}
	}

	private void InitSources()
	{
		if (!m_Initialized)
		{
			for (int i = 0; i < 2; i++)
			{
				m_Source[i] = base.gameObject.AddComponent<AudioSource>();
				m_Source[i].outputAudioMixerGroup = GreenHellGame.Instance.GetAudioMixerGroup(AudioMixerGroupGame.Music);
				m_Source[i].spatialBlend = 0f;
			}
			m_Initialized = true;
		}
	}

	public void Play(AudioClip clip, float volume = 1f, bool looped = false, int track = 0)
	{
		if (!m_Source[track])
		{
			InitSources();
		}
		m_Source[track].clip = clip;
		m_Source[track].volume = volume;
		m_Source[track].loop = looped;
		m_Source[track].Play();
	}

	public string GetPath(string name)
	{
		if (!m_MusicsMap.ContainsKey(name.ToLower()))
		{
			return string.Empty;
		}
		string text = m_MusicsMap[name.ToLower()];
		return text.Remove(text.LastIndexOf('.')).Remove(0, 17);
	}

	public void PlayByName(string name, bool looped = false, float volume = 1f, int track = 0)
	{
		string path = GetPath(name);
		AudioClip audioClip = Resources.Load(path) as AudioClip;
		if (!audioClip)
		{
			DebugUtils.Assert("[Music:PlayByName] Can't find music " + path);
			return;
		}
		if (!m_Source[track])
		{
			InitSources();
		}
		m_Source[track].clip = audioClip;
		m_Source[track].loop = looped;
		m_Source[track].volume = volume;
		m_Source[track].Play();
	}

	public void Stop(float fadeout = 0f, int track = 0)
	{
		AudioSource audio_source = m_Source[track];
		StartCoroutine(AudioFadeOut.FadeOut(audio_source, fadeout));
	}

	public bool IsMusicPlaying(int track = 0)
	{
		if (m_Source[track] != null && m_Source[track].isPlaying)
		{
			return m_Source[track].volume > 0f;
		}
		return false;
	}

	public bool IsMusicPlayingAndIsNoPause(int track = 0)
	{
		if (!IsMusicPlaying(track))
		{
			return MainLevel.Instance.IsPause();
		}
		return true;
	}

	public void Save()
	{
		for (int i = 0; i < 2; i++)
		{
			if ((bool)m_Source[i])
			{
				SaveGame.SaveVal("MusicPlaying" + i, m_Source[i].isPlaying);
				if (m_Source[i].isPlaying)
				{
					SaveGame.SaveVal("Music" + i, m_Source[i].clip.name);
				}
			}
			else
			{
				SaveGame.SaveVal("MusicPlaying" + i, val: false);
			}
		}
	}

	public void Load()
	{
		for (int i = 0; i < 2; i++)
		{
			if (SaveGame.LoadBVal("MusicPlaying" + i))
			{
				PlayByName(SaveGame.LoadSVal("Music" + i), looped: false, 1f, i);
			}
		}
	}

	public void FadeOut(float target_volume, float time, int track)
	{
		AudioSource audio_source = m_Source[track];
		StartCoroutine(AudioFadeOut.FadeOut(audio_source, time, target_volume));
	}

	public void FadeIn(float target_volume, float time, int track)
	{
		AudioSource audio_source = m_Source[track];
		StartCoroutine(AudioFadeOut.FadeIn(audio_source, time, target_volume));
	}

	public void Schedule(string clip_name, int track = 0, bool loop = false)
	{
		if (m_Scheduled.ContainsKey(track))
		{
			if (m_Scheduled[track] == null)
			{
				m_Scheduled[track] = new MusicScheduleData();
			}
			m_Scheduled[track].m_ClipName = clip_name;
		}
		else
		{
			MusicScheduleData musicScheduleData = new MusicScheduleData();
			musicScheduleData.m_ClipName = clip_name;
			m_Scheduled.Add(track, musicScheduleData);
		}
		if (m_Source[track].clip != null)
		{
			m_Scheduled[track].m_PlayTime = Time.time + (m_Source[track].clip.length - m_Source[track].time);
		}
		else
		{
			m_Scheduled[track].m_PlayTime = Time.time;
		}
		m_Scheduled[track].m_Loop = loop;
	}

	private void Update()
	{
		int key = -1;
		for (int i = 0; i < 2; i++)
		{
			Dictionary<int, MusicScheduleData>.Enumerator enumerator = m_Scheduled.GetEnumerator();
			while (enumerator.MoveNext())
			{
				int key2 = enumerator.Current.Key;
				string clipName = enumerator.Current.Value.m_ClipName;
				bool loop = enumerator.Current.Value.m_Loop;
				if (clipName.Length > 0 && Time.time >= enumerator.Current.Value.m_PlayTime)
				{
					PlayByName(clipName, loop, 1f, key2);
					key = key2;
					break;
				}
			}
			m_Scheduled.Remove(key);
		}
	}

	public void StopAll()
	{
		m_Scheduled.Clear();
		for (int i = 0; i < m_Source.Count(); i++)
		{
			m_Source[i]?.Stop();
		}
	}

	public void StopAllOnTrack(int track, float time)
	{
		if (m_Scheduled.ContainsKey(track))
		{
			m_Scheduled.Remove(track);
		}
		FadeOut(0f, time, track);
	}
}

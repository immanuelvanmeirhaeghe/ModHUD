// MusicJingle
using UnityEngine;

public class MusicJingle : MonoBehaviour
{
	private const int m_NumJingles = 2;

	[HideInInspector]
	public AudioSource[] m_Source = new AudioSource[2];

	private static MusicJingle s_Instance;

	private bool m_Initialized;

	private void Awake()
	{
		s_Instance = this;
	}

	public static MusicJingle Get()
	{
		return s_Instance;
	}

	private void InitSource()
	{
		if (!m_Initialized)
		{
			for (int i = 0; i < 2; i++)
			{
				m_Source[i] = base.gameObject.AddComponent<AudioSource>();
				m_Source[i].outputAudioMixerGroup = GreenHellGame.Instance.GetAudioMixerGroup(AudioMixerGroupGame.Music);
				m_Source[i].spatialBlend = 0f;
				m_Initialized = true;
			}
		}
	}

	public void Play(AudioClip clip, float volume = 1f, int track = 0)
	{
		if (!m_Source[track])
		{
			InitSource();
		}
		m_Source[track].clip = clip;
		m_Source[track].volume = volume;
		m_Source[track].Play();
	}

	public void PlayByName(string name, bool looped = false, float volume = 1f, int track = 0)
	{
		AudioClip audioClip = Resources.Load(Music.Get().GetPath(name)) as AudioClip;
		if (!audioClip)
		{
			DebugUtils.Assert("[Music:PlayByName] Can't find music " + name);
			return;
		}
		if (!m_Source[track])
		{
			InitSource();
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

	public void FadeOut(float target_volume, float time, int track = 0)
	{
		AudioSource audio_source = m_Source[track];
		StartCoroutine(AudioFadeOut.FadeOut(audio_source, time, target_volume));
	}

	public void FadeIn(float target_volume, float time, int track = 0)
	{
		AudioSource audio_source = m_Source[track];
		StartCoroutine(AudioFadeOut.FadeIn(audio_source, time, target_volume));
	}

	public void StoppAll()
	{
		for (int i = 0; i < m_Source.Length; i++)
		{
			if ((bool)m_Source[i])
			{
				m_Source[i].Stop();
			}
		}
	}
}

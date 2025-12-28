using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Settings")]
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;
    public int poolSize = 30;

    [Header("Registered Sounds")]
    public List<SoundData> soundLibrary;

    private List<AudioSource> sfxPool;
    private Dictionary<SoundType, SoundData> soundMap;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        Initialize();
    }

    private void Initialize()
    {
        soundMap = new Dictionary<SoundType, SoundData>();
        foreach (var s in soundLibrary)
        {
            if (!soundMap.ContainsKey(s.type)) soundMap.Add(s.type, s);
        }

        sfxPool = new List<AudioSource>();
        GameObject root = new GameObject("AudioPool");
        root.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject($"SFX_{i}");
            obj.transform.SetParent(root.transform);
            var src = obj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = sfxGroup;
            src.playOnAwake = false;
            obj.SetActive(false);
            sfxPool.Add(src);
        }
    }

    public void PlayOneShot(SoundType type, Vector3 position)
    {
        if (type == SoundType.None) return;
        if (!soundMap.TryGetValue(type, out SoundData data))
        {
            return;
        }


        AudioSource source = GetFreeSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = data.clip;
        source.volume = data.volume;
        source.pitch = data.pitch;
        source.spatialBlend = data.spatialBlend;
        source.minDistance = data.minDistance;
        source.maxDistance = data.maxDistance;

        source.gameObject.SetActive(true);
        source.Play();

        StartCoroutine(DisableSourceDelayed(source, data.clip.length));
    }

    private AudioSource GetFreeSource()
    {
        foreach (var s in sfxPool)
            if (!s.gameObject.activeInHierarchy) return s;
        return null;
    }

    private System.Collections.IEnumerator DisableSourceDelayed(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);
        src.Stop();
        src.gameObject.SetActive(false);
    }
}
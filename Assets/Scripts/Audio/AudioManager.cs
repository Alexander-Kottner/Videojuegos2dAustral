using UnityEngine;
using UnityEngine.SceneManagement;
using Weapons.Data;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("SFX - Weapons")]
    [SerializeField] private AudioClip[] meleeSlashClips;
    [SerializeField] private AudioClip[] meleeHeavyClips;
    [SerializeField] private AudioClip[] projectileClips;
    [SerializeField] private AudioClip[] magicClips;

    [Header("SFX - Combat")]
    [SerializeField] private AudioClip[] enemyAttackClips;
    [SerializeField] private AudioClip[] enemyDieClips;
    [SerializeField] private AudioClip[] playerHurtClips;

    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        gameObject.AddComponent<AudioListener>();

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.volume = sfxVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (listener.gameObject != gameObject) Destroy(listener);

        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clip = sceneName == "SampleScene" ? gameMusic : menuMusic;
        PlayMusic(clip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public static void PlayWeaponSFX(WeaponSoundCategory category)
    {
        if (Instance == null) return;
        switch (category)
        {
            case WeaponSoundCategory.MeleeLight:  Instance.PlayRandom(Instance.meleeSlashClips);  break;
            case WeaponSoundCategory.MeleeHeavy:  Instance.PlayRandom(Instance.meleeHeavyClips);  break;
            case WeaponSoundCategory.Projectile:  Instance.PlayRandom(Instance.projectileClips);  break;
            case WeaponSoundCategory.Magic:        Instance.PlayRandom(Instance.magicClips);        break;
        }
    }

    public static void PlayEnemyAttack() => Instance?.PlayRandom(Instance.enemyAttackClips);
    public static void PlayEnemyDie()    => Instance?.PlayRandom(Instance.enemyDieClips);
    public static void PlayPlayerHurt()  => Instance?.PlayRandom(Instance.playerHurtClips);

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) _sfxSource.PlayOneShot(clip, sfxVolume);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerHealthComponent = global::Input.PlayerHealth;
using PlayerControllerComponent = global::Input.PlayerController;

[Serializable]
public class SavedGameState
{
    public int version = 1;
    public string savedAtUtc;
    public string sceneName;
    public PlayerSaveState player = new();
    public WaveSaveState wave = new();
}

[Serializable]
public class PlayerSaveState
{
    public int currentHealth = 100;
    public int maxHealth = 100;
    public Vector3 position;
    public List<WeaponSaveState> weapons = new();
}

[Serializable]
public class WeaponSaveState
{
    public string definitionName;
    public int stageIndex;
    public int lastHitCount;
}

[Serializable]
public class WaveSaveState
{
    public int currentWave;
}

public class GameStateManager : MonoBehaviour
{
    private const string SaveFileName = "savegame.json";

    public static GameStateManager Instance { get; private set; }
    public static SavedGameState CurrentState { get; private set; }
    public static bool HasSavedGame => File.Exists(SavePath);

    private static bool _continueRequested;
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null) return;
        GameObject go = new("GameStateManager");
        go.AddComponent<GameStateManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadFromDisk();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_continueRequested)
            StartCoroutine(ApplyStateNextFrame());
    }

    private IEnumerator ApplyStateNextFrame()
    {
        yield return null;
        ConsumeContinueStateIfNeeded();
    }

    public static void StartNewGame(int sceneIndex = 1)
    {
        _continueRequested = false;
        CurrentState = null;
        DeleteSave();
        SceneManager.LoadScene(sceneIndex);
    }

    public static bool ContinueSavedGame(int fallbackSceneIndex = 1)
    {
        if (!LoadFromDisk()) return false;
        _continueRequested = true;
        string sceneName = CurrentState?.sceneName;
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            SceneManager.LoadScene(fallbackSceneIndex);
        return true;
    }

    public static void SaveCurrentState()
    {
        Save(CaptureCurrentState());
    }

    public static void SaveWaveCompleted(int wave)
    {
        SavedGameState state = CaptureCurrentState();
        state.wave.currentWave = wave;
        Save(state);
    }

    public static SavedGameState CaptureCurrentState()
    {
        SavedGameState state = new()
        {
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            sceneName = SceneManager.GetActiveScene().name
        };

        CapturePlayerState(state.player);

        Spawning.SpawnerController spawner = FindFirstObjectByType<Spawning.SpawnerController>();
        if (spawner != null)
            state.wave.currentWave = spawner.CurrentWave;

        CurrentState = state;
        return state;
    }

    public static void Save(SavedGameState state)
    {
        if (state == null) return;
        CurrentState = state;
        File.WriteAllText(SavePath, JsonUtility.ToJson(state, true));
    }

    public static bool LoadFromDisk()
    {
        if (!File.Exists(SavePath)) { CurrentState = null; return false; }
        CurrentState = JsonUtility.FromJson<SavedGameState>(File.ReadAllText(SavePath));
        return CurrentState != null;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        CurrentState = null;
    }

    private static void ConsumeContinueStateIfNeeded()
    {
        if (!_continueRequested || CurrentState == null) return;
        _continueRequested = false;
        ApplyStateToScene(CurrentState);
    }

    private static void ApplyStateToScene(SavedGameState state)
    {
        if (state == null) return;

        PlayerHealthComponent health = FindFirstObjectByType<PlayerHealthComponent>();
        if (health != null)
            health.RestoreState(state.player.currentHealth, state.player.maxHealth);

        PlayerControllerComponent player = FindFirstObjectByType<PlayerControllerComponent>();
        if (player != null && state.player.position != Vector3.zero)
            player.transform.position = state.player.position;

        Weapons.PlayerWeaponController weaponController = FindFirstObjectByType<Weapons.PlayerWeaponController>();
        if (weaponController != null)
            RestoreWeapons(weaponController, state.player.weapons);

        Spawning.SpawnerController spawner = FindFirstObjectByType<Spawning.SpawnerController>();
        if (spawner != null && state.wave.currentWave > 0)
            spawner.RestoreWaveState(state.wave.currentWave);
    }

    private static void CapturePlayerState(PlayerSaveState playerState)
    {
        PlayerHealthComponent health = FindFirstObjectByType<PlayerHealthComponent>();
        if (health != null)
        {
            playerState.currentHealth = health.CurrentHealth;
            playerState.maxHealth = health.MaximumHealth;
        }

        PlayerControllerComponent player = FindFirstObjectByType<PlayerControllerComponent>();
        if (player != null)
            playerState.position = player.transform.position;

        Weapons.PlayerWeaponController weaponController = FindFirstObjectByType<Weapons.PlayerWeaponController>();
        if (weaponController == null) return;

        playerState.weapons.Clear();
        foreach (Weapons.WeaponController weapon in weaponController.EquippedWeapons)
        {
            if (weapon?.Definition == null) continue;
            playerState.weapons.Add(new WeaponSaveState
            {
                definitionName = weapon.Definition.name,
                stageIndex = weapon.StageIndex,
                lastHitCount = weapon.LastHitCount
            });
        }
    }

    private static void RestoreWeapons(Weapons.PlayerWeaponController controller, List<WeaponSaveState> savedWeapons)
    {
        if (savedWeapons == null) return;
        foreach (WeaponSaveState saved in savedWeapons)
        {
            foreach (Weapons.WeaponController weapon in controller.EquippedWeapons)
            {
                if (weapon?.Definition != null && weapon.Definition.name == saved.definitionName)
                {
                    weapon.RestoreState(saved.stageIndex, saved.lastHitCount);
                    break;
                }
            }
        }
    }
}

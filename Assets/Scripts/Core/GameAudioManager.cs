using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameSfxType
{
    ParryNormal,
    ParryPower,
    ParryWind,
    ParryWindPower,
    ParryFlame,
    ParryFlamePower,
    ParryThunder,
    ParryThunderPower,
    ParryFrost,
    ParryFrostPower,
    ParryVoid,
    ParryVoidPower,
    SlashSwing,
    PlayerHit,
    PlayerDeath,
    Dash,
    Jump,
    LightningStrike,
    SkillCloneBall,
    SkillInvisible,
    SkillTornado,
    SkillFreeze,
    SkillForcefield,
    SkillForcefieldDeflect,
    SkillTelekinesis,
    UIClick,
    UICancel,
    UIConfirm,
    CountdownTick,
    MatchStart,
    Victory,
    Defeat,
    RankUp
}

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    const string PrefKeyMuted = "FB_AUDIO_MUTED";
    const float DefaultBgmVolume = 0.42f;
    const float DuckedBgmVolume = 0.20f;
    const float DefaultSfxVolume = 0.82f;
    const int SfxPoolSize = 10;

    AudioSource bgmA;
    AudioSource bgmB;
    bool usingBgmA = true;
    Coroutine bgmFadeRoutine;
    string currentBgmKey = "";
    bool isBgmDucked;

    AudioSource[] sfxPool;
    int sfxCursor;

    readonly Dictionary<string, AudioClip> bgmCache = new Dictionary<string, AudioClip>();
    readonly Dictionary<GameSfxType, AudioClip> sfxCache = new Dictionary<GameSfxType, AudioClip>();
    readonly Dictionary<GameSfxType, float> lastPlayTime = new Dictionary<GameSfxType, float>();
    readonly HashSet<int> boundButtonIds = new HashSet<int>();

    float buttonScanTimer;

    public static bool IsMuted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBootstrap()
    {
        if (Application.isBatchMode)
            return;

        EnsureInstance();
    }

    public static GameAudioManager EnsureInstance()
    {
        if (Application.isBatchMode)
            return null;

        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("GameAudioManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameAudioManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsMuted = PlayerPrefs.GetInt(PrefKeyMuted, 0) == 1;

        bgmA = CreateBgmSource("BGM_A");
        bgmB = CreateBgmSource("BGM_B");

        sfxPool = new AudioSource[SfxPoolSize];
        for (int i = 0; i < SfxPoolSize; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true;
            sfxPool[i] = s;
        }

        PreloadAllClips();
        ApplyMuteState();

        SceneManager.sceneLoaded += OnSceneLoaded;
        HandleSceneAudio(SceneManager.GetActiveScene().buildIndex);
        BindActiveUIButtons();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        buttonScanTimer += Time.unscaledDeltaTime;
        if (buttonScanTimer >= 0.45f)
        {
            buttonScanTimer = 0f;
            BindActiveUIButtons();
        }
    }

    AudioSource CreateBgmSource(string label)
    {
        GameObject child = new GameObject(label);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.priority = 64;
        return src;
    }

    void PreloadAllClips()
    {
        LoadBgm("BGM_Menu");
        LoadBgm("BGM_Map_Volcano");
        LoadBgm("BGM_Map_Cyber");
        LoadBgm("BGM_Map_Storm");

        LoadSfx(GameSfxType.ParryNormal, "SFX_Parry_Normal");
        LoadSfx(GameSfxType.ParryPower, "SFX_Parry_Power");
        LoadSfx(GameSfxType.ParryWind, "SFX_Parry_Wind");
        LoadSfx(GameSfxType.ParryWindPower, "SFX_Parry_Wind_Power");
        LoadSfx(GameSfxType.ParryFlame, "SFX_Parry_Flame");
        LoadSfx(GameSfxType.ParryFlamePower, "SFX_Parry_Flame_Power");
        LoadSfx(GameSfxType.ParryThunder, "SFX_Parry_Thunder");
        LoadSfx(GameSfxType.ParryThunderPower, "SFX_Parry_Thunder_Power");
        LoadSfx(GameSfxType.ParryFrost, "SFX_Parry_Frost");
        LoadSfx(GameSfxType.ParryFrostPower, "SFX_Parry_Frost_Power");
        LoadSfx(GameSfxType.ParryVoid, "SFX_Parry_Void");
        LoadSfx(GameSfxType.ParryVoidPower, "SFX_Parry_Void_Power");
        LoadSfx(GameSfxType.SlashSwing, "SFX_Slash_Swing");
        LoadSfx(GameSfxType.PlayerHit, "SFX_Player_Hit");
        LoadSfx(GameSfxType.PlayerDeath, "SFX_Player_Death");
        LoadSfx(GameSfxType.Dash, "SFX_Dash");
        LoadSfx(GameSfxType.Jump, "SFX_Jump");
        LoadSfx(GameSfxType.LightningStrike, "SFX_Lightning_Strike");
        LoadSfx(GameSfxType.SkillCloneBall, "SFX_Skill_CloneBall");
        LoadSfx(GameSfxType.SkillInvisible, "SFX_Skill_Invisible");
        LoadSfx(GameSfxType.SkillTornado, "SFX_Skill_Tornado");
        LoadSfx(GameSfxType.SkillFreeze, "SFX_Skill_Freeze");
        LoadSfx(GameSfxType.SkillForcefield, "SFX_Skill_Forcefield");
        LoadSfx(GameSfxType.SkillForcefieldDeflect, "SFX_Skill_Forcefield_Deflect");
        LoadSfx(GameSfxType.SkillTelekinesis, "SFX_Skill_Telekinesis");
        LoadSfx(GameSfxType.UIClick, "SFX_UI_Click");
        LoadSfx(GameSfxType.UICancel, "SFX_UI_Cancel");
        LoadSfx(GameSfxType.UIConfirm, "SFX_UI_Confirm");
        LoadSfx(GameSfxType.CountdownTick, "SFX_Countdown_Tick");
        LoadSfx(GameSfxType.MatchStart, "SFX_Match_Start");
        LoadSfx(GameSfxType.Victory, "SFX_Victory");
        LoadSfx(GameSfxType.Defeat, "SFX_Defeat");
        LoadSfx(GameSfxType.RankUp, "SFX_RankUp");
    }

    AudioClip LoadBgm(string clipName)
    {
        if (bgmCache.TryGetValue(clipName, out AudioClip cached) && cached != null)
            return cached;

        AudioClip clip = Resources.Load<AudioClip>("Audio/BGM/" + clipName);
        if (clip != null)
        {
            bgmCache[clipName] = clip;
        }
        return clip;
    }

    void LoadSfx(GameSfxType type, string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
        if (clip != null)
        {
            sfxCache[type] = clip;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        boundButtonIds.Clear();
        EnsureAudioListenerExists();
        HandleSceneAudio(scene.buildIndex);
        BindActiveUIButtons();
    }

    void EnsureAudioListenerExists()
    {
        AudioListener existing = FindFirstObjectByType<AudioListener>();
        if (existing == null && Camera.main != null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }
    }

    void HandleSceneAudio(int sceneIndex)
    {
        if (sceneIndex == 0)
        {
            // Menu Scene
            SetBgmDuckInternal(false);
            CrossfadeToBgm("BGM_Menu", 0.65f);
        }
        else if (sceneIndex == 1)
        {
            // Loading Scene — gently duck music for smooth transition into arena
            SetBgmDuckInternal(true);
        }
        else
        {
            // Gameplay Scene — restore level and play current map track (ArenaMapManager will also override once synced)
            SetBgmDuckInternal(false);
            PlayMapMusicInternal(ArenaMapManager.CurrentMapType);
        }
    }

    // ================= PUBLIC API =================

    public static void ToggleMute()
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;

        IsMuted = !IsMuted;
        PlayerPrefs.SetInt(PrefKeyMuted, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
        mgr.ApplyMuteState();

        if (!IsMuted)
        {
            mgr.PlaySfxInternal(GameSfxType.UIConfirm, 0.75f, 1.0f);
        }
    }

    public static void PlayMenuMusic()
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;
        mgr.SetBgmDuckInternal(false);
        mgr.CrossfadeToBgm("BGM_Menu", 0.65f);
    }

    public static void PlayMapMusic(ArenaMapType mapType)
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;
        mgr.SetBgmDuckInternal(false);
        mgr.PlayMapMusicInternal(mapType);
    }

    public static void SetMusicDuck(bool ducked)
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;
        mgr.SetBgmDuckInternal(ducked);
    }

    public static void PlaySFX(GameSfxType type, float volumeScale = 1f, float pitch = 1f)
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;
        mgr.PlaySfxInternal(type, volumeScale, pitch);
    }

    public static void PlayParry(bool isPower, float normalizedBallSpeed = 0f)
    {
        PlayElementalParry(WeaponType.WindBlade, isPower, normalizedBallSpeed);
    }

    public static void PlayElementalParry(WeaponType weaponType, bool isPower, float normalizedBallSpeed = 0f)
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;

        GameSfxType sfx;
        switch (weaponType)
        {
            case WeaponType.FlameSaber:
                sfx = isPower ? GameSfxType.ParryFlamePower : GameSfxType.ParryFlame;
                break;
            case WeaponType.ThunderSpear:
                sfx = isPower ? GameSfxType.ParryThunderPower : GameSfxType.ParryThunder;
                break;
            case WeaponType.FrostDualBlades:
                sfx = isPower ? GameSfxType.ParryFrostPower : GameSfxType.ParryFrost;
                break;
            case WeaponType.VoidScythe:
                sfx = isPower ? GameSfxType.ParryVoidPower : GameSfxType.ParryVoid;
                break;
            default:
                sfx = isPower ? GameSfxType.ParryWindPower : GameSfxType.ParryWind;
                break;
        }

        // Tăng cao độ rất nhẹ & ấm (0.95 -> 1.11) khi bóng nhanh, hoàn toàn không chói tai!
        float pitch = Mathf.Lerp(0.95f, 1.11f, Mathf.Clamp01(normalizedBallSpeed)) + Random.Range(-0.025f, 0.025f);
        mgr.PlaySfxInternal(sfx, isPower ? 0.96f : 0.86f, pitch);
    }

    public static void PlayWeaponSwing(WeaponType weaponType, bool isPower)
    {
        GameAudioManager mgr = EnsureInstance();
        if (mgr == null) return;

        float basePitch;
        switch (weaponType)
        {
            case WeaponType.FlameSaber: basePitch = 0.86f; break;
            case WeaponType.VoidScythe: basePitch = 0.78f; break;
            case WeaponType.ThunderSpear: basePitch = 1.02f; break;
            case WeaponType.FrostDualBlades: basePitch = 1.08f; break;
            default: basePitch = 0.98f; break;
        }
        if (isPower) basePitch *= 0.92f;

        mgr.PlaySfxInternal(GameSfxType.SlashSwing, isPower ? 0.56f : 0.42f, basePitch);
    }

    // ================= INTERNAL BGM =================

    void PlayMapMusicInternal(ArenaMapType mapType)
    {
        string key;
        switch (mapType)
        {
            case ArenaMapType.CyberTech:
                key = "BGM_Map_Cyber";
                break;
            case ArenaMapType.ThunderStorm:
                key = "BGM_Map_Storm";
                break;
            default:
                key = "BGM_Map_Volcano";
                break;
        }
        CrossfadeToBgm(key, 0.75f);
    }

    void CrossfadeToBgm(string key, float fadeDuration)
    {
        if (string.IsNullOrEmpty(key))
            return;

        AudioClip nextClip = LoadBgm(key);
        if (nextClip == null)
            return;

        AudioSource active = usingBgmA ? bgmA : bgmB;
        if (currentBgmKey == key && active != null && active.isPlaying)
        {
            active.volume = GetTargetBgmVolume();
            return;
        }

        currentBgmKey = key;
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }
        bgmFadeRoutine = StartCoroutine(CrossfadeRoutine(nextClip, fadeDuration));
    }

    IEnumerator CrossfadeRoutine(AudioClip nextClip, float duration)
    {
        AudioSource outgoing = usingBgmA ? bgmA : bgmB;
        AudioSource incoming = usingBgmA ? bgmB : bgmA;
        usingBgmA = !usingBgmA;

        incoming.clip = nextClip;
        incoming.volume = 0f;
        incoming.loop = true;
        incoming.Play();

        float startOutVol = outgoing != null && outgoing.isPlaying ? outgoing.volume : 0f;
        float elapsed = 0f;
        float dur = Mathf.Max(0.05f, duration);

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float targetVol = GetTargetBgmVolume();

            incoming.volume = Mathf.Lerp(0f, targetVol, t);
            if (outgoing != null && outgoing.isPlaying)
            {
                outgoing.volume = Mathf.Lerp(startOutVol, 0f, t);
            }
            yield return null;
        }

        incoming.volume = GetTargetBgmVolume();
        if (outgoing != null)
        {
            outgoing.Stop();
            outgoing.volume = 0f;
        }

        bgmFadeRoutine = null;
    }

    void SetBgmDuckInternal(bool ducked)
    {
        isBgmDucked = ducked;
        AudioSource active = usingBgmA ? bgmA : bgmB;
        if (active != null && active.isPlaying && bgmFadeRoutine == null)
        {
            active.volume = GetTargetBgmVolume();
        }
    }

    float GetTargetBgmVolume()
    {
        if (IsMuted) return 0f;
        return isBgmDucked ? DuckedBgmVolume : DefaultBgmVolume;
    }

    void ApplyMuteState()
    {
        if (bgmA != null) bgmA.mute = IsMuted;
        if (bgmB != null) bgmB.mute = IsMuted;
        AudioSource active = usingBgmA ? bgmA : bgmB;
        if (active != null && active.isPlaying)
        {
            active.volume = GetTargetBgmVolume();
        }
        if (sfxPool != null)
        {
            for (int i = 0; i < sfxPool.Length; i++)
            {
                if (sfxPool[i] != null) sfxPool[i].mute = IsMuted;
            }
        }
    }

    // ================= INTERNAL SFX =================

    void PlaySfxInternal(GameSfxType type, float volumeScale, float pitch)
    {
        if (IsMuted)
            return;

        if (!sfxCache.TryGetValue(type, out AudioClip clip) || clip == null)
            return;

        // Chống spam quá dày trong cùng 1 frame (trừ Parry nguyên tố cho phép đánh liên thanh cực nhanh 0.045s!)
        bool isParry =
            type == GameSfxType.ParryNormal || type == GameSfxType.ParryPower ||
            type == GameSfxType.ParryWind || type == GameSfxType.ParryWindPower ||
            type == GameSfxType.ParryFlame || type == GameSfxType.ParryFlamePower ||
            type == GameSfxType.ParryThunder || type == GameSfxType.ParryThunderPower ||
            type == GameSfxType.ParryFrost || type == GameSfxType.ParryFrostPower ||
            type == GameSfxType.ParryVoid || type == GameSfxType.ParryVoidPower;
        float minGap = isParry ? 0.045f : 0.06f;
        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(type, out float prev) && (now - prev) < minGap)
            return;
        lastPlayTime[type] = now;

        AudioSource src = sfxPool[sfxCursor];
        sfxCursor = (sfxCursor + 1) % sfxPool.Length;

        float jitter = (type == GameSfxType.SlashSwing || type == GameSfxType.UIClick || type == GameSfxType.PlayerHit)
            ? Random.Range(-0.04f, 0.04f)
            : 0f;

        src.pitch = Mathf.Clamp(pitch + jitter, 0.65f, 1.55f);
        src.volume = Mathf.Clamp01(DefaultSfxVolume * volumeScale);
        src.PlayOneShot(clip, 1f);
    }

    // ================= GLOBAL UI BUTTON SFX AUTO-BINDER =================

    void BindActiveUIButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            if (btn == null) continue;

            int id = btn.GetInstanceID();
            if (!boundButtonIds.Add(id))
                continue;

            // Bỏ qua các nút chiến đấu trong HUD (Parry/Dash/Jump/Skill đã có âm thanh chiến đấu riêng!)
            if (
                btn.GetComponent<DashButtonUI>() != null ||
                btn.GetComponent<JumpButtonUI>() != null ||
                btn.GetComponent<SkillButtonUI>() != null ||
                btn.GetComponent<ParryChange>() != null
            )
            {
                continue;
            }

            string lower = btn.gameObject.name.ToLowerInvariant();
            if (lower.Contains("parry") || lower.Contains("dash") || lower.Contains("jump") || lower.Contains("audio"))
                continue;

            GameSfxType sfxType = ClassifyButtonSfx(lower);
            btn.onClick.AddListener(() => PlaySFX(sfxType, 0.80f));
        }
    }

    static GameSfxType ClassifyButtonSfx(string lowerName)
    {
        if (
            lowerName.Contains("cancel") ||
            lowerName.Contains("close") ||
            lowerName.Contains("exit") ||
            lowerName.Contains("logout") ||
            lowerName.Contains("back") ||
            lowerName.Contains("leave")
        )
        {
            return GameSfxType.UICancel;
        }

        if (
            lowerName.Contains("play") ||
            lowerName.Contains("solo") ||
            lowerName.Contains("rank") ||
            lowerName.Contains("train") ||
            lowerName.Contains("buy") ||
            lowerName.Contains("claim") ||
            lowerName.Contains("equip") ||
            lowerName.Contains("start") ||
            lowerName.Contains("confirm") ||
            lowerName.Contains("login") ||
            lowerName.Contains("register") ||
            lowerName.Contains("create") ||
            lowerName.Contains("join") ||
            lowerName.Contains("action")
        )
        {
            return GameSfxType.UIConfirm;
        }

        return GameSfxType.UIClick;
    }
}

using System.Collections.Generic;
using UnityEngine;

public enum GameMatchMode
{
    Rank,
    Solo,
    Training,
    Custom
}

public struct MatchResultSummary
{
    public GameMatchMode mode;
    public bool isVictory;
    public int placement;
    public int totalPlayers;
    public int soloWins;
    public int soloLosses;
    public int oldElo;
    public int newElo;
    public int eloDelta;
    public int oldTier;
    public int newTier;
    public bool isRankUp;
    public bool isRankDown;
    public string rankTitle;
    public string nextRankTitle;
    public int nextRankElo;
    public int goldEarned;
    public int totalGold;
    public bool newbieMailJustUnlocked;
}

public static class PlayerStatsManager
{
    const string KEY_PLAYER_NAME = "FlashBall_PlayerName";
    const string KEY_TUTORIAL_DONE = "FlashBall_TutorialDone_V1";
    const string KEY_SOLO_WINS = "FlashBall_SoloWins";
    const string KEY_SOLO_LOSSES = "FlashBall_SoloLosses";
    const string KEY_PLAYER_ELO = "FlashBall_PlayerElo_V2";
    const string KEY_PLAYER_GOLD = "FlashBall_PlayerGold";
    const string KEY_EQUIPPED_WEAPON = "FlashBall_EquippedWeapon";
    const string KEY_OWNED_WEAPON_PREFIX = "FlashBall_OwnedWeapon_";
    const string KEY_EQUIPPED_SKILL = "FlashBall_EquippedSkill";
    const string KEY_OWNED_SKILL_PREFIX = "FlashBall_OwnedSkill_";
    const string KEY_EQUIPPED_SKIN = "FlashBall_EquippedSkin_V1";
    const string KEY_OWNED_SKIN_PREFIX = "FlashBall_OwnedSkin_";
    const string KEY_MATCHES_PLAYED = "FlashBall_MatchesPlayed_V1";
    const string KEY_NEWBIE_MAIL_UNLOCKED = "FlashBall_NewbieMailUnlocked_V1";
    const string KEY_NEWBIE_MAIL_CLAIMED = "FlashBall_NewbieMailClaimed_V1";

    const int DEFAULT_ELO = 100;

    static readonly int[] RankThresholds = new int[] { 0, 250, 500, 800, 1150 };
    static readonly string[] RankNames = new string[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond" };

    static bool suppressCloudAutoSave;

    static void NotifyChanged()
    {
        if (!suppressCloudAutoSave)
        {
            AccountDatabaseService.SaveCurrentProfile();
        }
    }

    // ================= PROFILE & ONBOARDING =================

    public static bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(KEY_PLAYER_NAME, ""));

    public static string PlayerName
    {
        get
        {
            string saved = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
            return string.IsNullOrWhiteSpace(saved) ? "Player" : saved;
        }
        set
        {
            string clean = SanitizeName(value);
            if (!string.IsNullOrEmpty(clean))
            {
                PlayerPrefs.SetString(KEY_PLAYER_NAME, clean);
                PlayerPrefs.Save();
                NotifyChanged();
            }
        }
    }

    public static string SanitizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string trimmed = raw.Trim();
        if (trimmed.Length > 14) trimmed = trimmed.Substring(0, 14);
        return trimmed;
    }

    public static bool TutorialCompleted
    {
        get => PlayerPrefs.GetInt(KEY_TUTORIAL_DONE, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_TUTORIAL_DONE, value ? 1 : 0);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    // ================= MAILBOX & NEWBIE GIFT =================

    public static int MatchesPlayed
    {
        get => PlayerPrefs.GetInt(KEY_MATCHES_PLAYED, 0);
        set
        {
            PlayerPrefs.SetInt(KEY_MATCHES_PLAYED, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static bool NewbieMailUnlocked
    {
        get => PlayerPrefs.GetInt(KEY_NEWBIE_MAIL_UNLOCKED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_UNLOCKED, value ? 1 : 0);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static bool NewbieMailClaimed
    {
        get => PlayerPrefs.GetInt(KEY_NEWBIE_MAIL_CLAIMED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_CLAIMED, value ? 1 : 0);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static bool HasUnreadMail => NewbieMailUnlocked && !NewbieMailClaimed;

    public static bool ClaimNewbieGiftMail()
    {
        if (!NewbieMailUnlocked || NewbieMailClaimed)
            return false;

        suppressCloudAutoSave = true;
        PlayerGold += 500;
        UnlockSkillDirect(SkillType.Freeze);
        PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_CLAIMED, 1);
        PlayerPrefs.Save();
        suppressCloudAutoSave = false;

        NotifyChanged();
        return true;
    }

    // ================= STATS =================

    public static int SoloWins
    {
        get => PlayerPrefs.GetInt(KEY_SOLO_WINS, 0);
        set
        {
            PlayerPrefs.SetInt(KEY_SOLO_WINS, Mathf.Max(0, value));
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static int SoloLosses
    {
        get => PlayerPrefs.GetInt(KEY_SOLO_LOSSES, 0);
        set
        {
            PlayerPrefs.SetInt(KEY_SOLO_LOSSES, Mathf.Max(0, value));
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static int PlayerElo
    {
        get => PlayerPrefs.GetInt(KEY_PLAYER_ELO, DEFAULT_ELO);
        set
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(KEY_PLAYER_ELO, clamped);
            PlayerPrefs.Save();

            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.playerElo = clamped;
            }
            NotifyChanged();
        }
    }

    public static int PlayerGold
    {
        get => PlayerPrefs.GetInt(KEY_PLAYER_GOLD, 0);
        set
        {
            PlayerPrefs.SetInt(KEY_PLAYER_GOLD, Mathf.Max(0, value));
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;
        PlayerGold += amount;
    }

    // ================= WEAPONS =================

    public static WeaponType EquippedWeapon
    {
        get
        {
            int raw = PlayerPrefs.GetInt(KEY_EQUIPPED_WEAPON, (int)WeaponType.WindBlade);
            if (raw < 0 || raw >= WeaponCatalog.All.Length)
                return WeaponType.WindBlade;

            WeaponType wt = (WeaponType)raw;
            return IsWeaponOwned(wt) ? wt : WeaponType.WindBlade;
        }
        set
        {
            if (!IsWeaponOwned(value))
                return;

            PlayerPrefs.SetInt(KEY_EQUIPPED_WEAPON, (int)value);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static bool IsWeaponOwned(WeaponType type)
    {
        if (type == WeaponType.WindBlade)
            return true;

        return PlayerPrefs.GetInt(KEY_OWNED_WEAPON_PREFIX + (int)type, 0) == 1;
    }

    public static bool TryBuyWeapon(WeaponType type)
    {
        if (IsWeaponOwned(type))
        {
            EquippedWeapon = type;
            return true;
        }

        WeaponDefinition def = WeaponCatalog.Get(type);
        if (PlayerGold < def.price)
            return false;

        suppressCloudAutoSave = true;
        PlayerGold -= def.price;
        PlayerPrefs.SetInt(KEY_OWNED_WEAPON_PREFIX + (int)type, 1);
        PlayerPrefs.SetInt(KEY_EQUIPPED_WEAPON, (int)type);
        PlayerPrefs.Save();
        suppressCloudAutoSave = false;
        NotifyChanged();
        return true;
    }

    public static void EquipWeapon(WeaponType type)
    {
        if (IsWeaponOwned(type))
        {
            EquippedWeapon = type;
        }
    }

    // ================= SKILLS =================

    public static SkillType EquippedSkill
    {
        get
        {
            int raw = PlayerPrefs.GetInt(KEY_EQUIPPED_SKILL, (int)SkillType.CloneBall);
            SkillType st = (SkillType)raw;
            if (st == SkillType.None || !IsSkillOwned(st))
                return SkillType.CloneBall;
            return st;
        }
        set
        {
            if (value == SkillType.None || !IsSkillOwned(value))
                return;

            PlayerPrefs.SetInt(KEY_EQUIPPED_SKILL, (int)value);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static bool IsSkillOwned(SkillType type)
    {
        if (type == SkillType.None)
            return false;

        SkillDefinition def = SkillCatalog.Get(type);
        if (def != null && def.price <= 0)
            return true;

        return PlayerPrefs.GetInt(KEY_OWNED_SKILL_PREFIX + (int)type, 0) == 1;
    }

    public static void UnlockSkillDirect(SkillType type)
    {
        if (type == SkillType.None) return;
        PlayerPrefs.SetInt(KEY_OWNED_SKILL_PREFIX + (int)type, 1);
        PlayerPrefs.Save();
        NotifyChanged();
    }

    public static bool TryBuySkill(SkillType type)
    {
        if (IsSkillOwned(type))
        {
            EquippedSkill = type;
            return true;
        }

        SkillDefinition def = SkillCatalog.Get(type);
        if (def == null || PlayerGold < def.price)
            return false;

        suppressCloudAutoSave = true;
        PlayerGold -= def.price;
        PlayerPrefs.SetInt(KEY_OWNED_SKILL_PREFIX + (int)type, 1);
        PlayerPrefs.SetInt(KEY_EQUIPPED_SKILL, (int)type);
        PlayerPrefs.Save();
        suppressCloudAutoSave = false;
        NotifyChanged();
        return true;
    }

    public static void EquipSkill(SkillType type)
    {
        if (IsSkillOwned(type))
        {
            EquippedSkill = type;
        }
    }

    // ================= CHARACTER SKINS (NGOAI TRANG) =================

    public static SkinType EquippedSkin
    {
        get
        {
            int raw = PlayerPrefs.GetInt(KEY_EQUIPPED_SKIN, (int)SkinType.DefaultStriker);
            if (raw < 0 || raw >= SkinCatalog.All.Length)
                return SkinType.DefaultStriker;

            SkinType st = (SkinType)raw;
            return IsSkinOwned(st) ? st : SkinType.DefaultStriker;
        }
        set
        {
            if (!IsSkinOwned(value))
                return;

            PlayerPrefs.SetInt(KEY_EQUIPPED_SKIN, (int)value);
            PlayerPrefs.Save();
            NotifyChanged();
        }
    }

    public static bool IsSkinOwned(SkinType type)
    {
        if (type == SkinType.DefaultStriker)
            return true;

        return PlayerPrefs.GetInt(KEY_OWNED_SKIN_PREFIX + (int)type, 0) == 1;
    }

    public static bool TryBuySkin(SkinType type)
    {
        if (IsSkinOwned(type))
        {
            EquippedSkin = type;
            return true;
        }

        SkinDefinition def = SkinCatalog.Get(type);
        if (def == null || PlayerGold < def.price)
            return false;

        suppressCloudAutoSave = true;
        PlayerGold -= def.price;
        PlayerPrefs.SetInt(KEY_OWNED_SKIN_PREFIX + (int)type, 1);
        PlayerPrefs.SetInt(KEY_EQUIPPED_SKIN, (int)type);
        PlayerPrefs.Save();
        suppressCloudAutoSave = false;
        NotifyChanged();
        return true;
    }

    public static void EquipSkin(SkinType type)
    {
        if (IsSkinOwned(type))
        {
            EquippedSkin = type;
        }
    }

    // ================= ACCOUNT IMPORT / EXPORT =================

    public static void ImportFromAccount(AccountRecord rec)
    {
        if (rec == null) return;
        suppressCloudAutoSave = true;

        PlayerPrefs.SetString(KEY_PLAYER_NAME, SanitizeName(rec.displayName));
        PlayerPrefs.SetInt(KEY_PLAYER_ELO, Mathf.Max(0, rec.elo));
        PlayerPrefs.SetInt(KEY_PLAYER_GOLD, Mathf.Max(0, rec.gold));
        PlayerPrefs.SetInt(KEY_SOLO_WINS, Mathf.Max(0, rec.soloWins));
        PlayerPrefs.SetInt(KEY_SOLO_LOSSES, Mathf.Max(0, rec.soloLosses));
        PlayerPrefs.SetInt(KEY_MATCHES_PLAYED, Mathf.Max(0, rec.matchesPlayed));
        PlayerPrefs.SetInt(KEY_TUTORIAL_DONE, rec.tutorialCompleted ? 1 : 0);
        PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_UNLOCKED, rec.newbieMailUnlocked ? 1 : 0);
        PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_CLAIMED, rec.newbieMailClaimed ? 1 : 0);

        foreach (WeaponDefinition w in WeaponCatalog.All)
        {
            bool owned = w.type == WeaponType.WindBlade || (rec.ownedWeapons != null && rec.ownedWeapons.Contains((int)w.type));
            PlayerPrefs.SetInt(KEY_OWNED_WEAPON_PREFIX + (int)w.type, owned ? 1 : 0);
        }
        PlayerPrefs.SetInt(KEY_EQUIPPED_WEAPON, rec.equippedWeapon);

        foreach (SkillDefinition s in SkillCatalog.All)
        {
            bool owned = s.price <= 0 || (rec.ownedSkills != null && rec.ownedSkills.Contains((int)s.type));
            PlayerPrefs.SetInt(KEY_OWNED_SKILL_PREFIX + (int)s.type, owned ? 1 : 0);
        }
        PlayerPrefs.SetInt(KEY_EQUIPPED_SKILL, rec.equippedSkill);

        foreach (SkinDefinition sk in SkinCatalog.All)
        {
            bool owned = sk.type == SkinType.DefaultStriker || (rec.ownedSkins != null && rec.ownedSkins.Contains((int)sk.type));
            PlayerPrefs.SetInt(KEY_OWNED_SKIN_PREFIX + (int)sk.type, owned ? 1 : 0);
        }
        PlayerPrefs.SetInt(KEY_EQUIPPED_SKIN, rec.equippedSkin);

        PlayerPrefs.Save();
        suppressCloudAutoSave = false;

        if (MatchmakingManager.Instance != null)
        {
            MatchmakingManager.Instance.playerElo = PlayerElo;
        }
    }

    public static void ExportToAccount(AccountRecord rec)
    {
        if (rec == null) return;

        rec.displayName = PlayerName;
        rec.elo = PlayerElo;
        rec.gold = PlayerGold;
        rec.soloWins = SoloWins;
        rec.soloLosses = SoloLosses;
        rec.matchesPlayed = MatchesPlayed;
        rec.tutorialCompleted = TutorialCompleted;
        rec.newbieMailUnlocked = NewbieMailUnlocked;
        rec.newbieMailClaimed = NewbieMailClaimed;
        rec.equippedWeapon = (int)EquippedWeapon;
        rec.equippedSkill = (int)EquippedSkill;
        rec.equippedSkin = (int)EquippedSkin;

        rec.ownedWeapons = new List<int>();
        foreach (WeaponDefinition w in WeaponCatalog.All)
        {
            if (IsWeaponOwned(w.type)) rec.ownedWeapons.Add((int)w.type);
        }

        rec.ownedSkills = new List<int>();
        foreach (SkillDefinition s in SkillCatalog.All)
        {
            if (IsSkillOwned(s.type)) rec.ownedSkills.Add((int)s.type);
        }

        rec.ownedSkins = new List<int>();
        foreach (SkinDefinition sk in SkinCatalog.All)
        {
            if (IsSkinOwned(sk.type)) rec.ownedSkins.Add((int)sk.type);
        }
    }

    // ================= REBALANCED RANK TIERS =================

    public static int GetRankTierIndex(int elo)
    {
        for (int i = RankThresholds.Length - 1; i >= 0; i--)
        {
            if (elo >= RankThresholds[i])
                return i;
        }
        return 0;
    }

    public static string GetRankTitle(int elo)
    {
        return RankNames[GetRankTierIndex(elo)];
    }

    public static string GetRankTitleByTier(int tier)
    {
        return RankNames[Mathf.Clamp(tier, 0, RankNames.Length - 1)];
    }

    public static int GetRankMinElo(int tier)
    {
        return RankThresholds[Mathf.Clamp(tier, 0, RankThresholds.Length - 1)];
    }

    public static int GetNextRankElo(int elo)
    {
        int tier = GetRankTierIndex(elo);
        if (tier >= RankThresholds.Length - 1)
            return RankThresholds[RankThresholds.Length - 1];
        return RankThresholds[tier + 1];
    }

    public static string GetNextRankTitle(int elo)
    {
        int tier = GetRankTierIndex(elo);
        if (tier >= RankNames.Length - 1)
            return "MAX";
        return RankNames[tier + 1];
    }

    public static float GetRankProgressNormalized(int elo)
    {
        int tier = GetRankTierIndex(elo);
        if (tier >= RankThresholds.Length - 1)
            return 1f;

        int minElo = RankThresholds[tier];
        int nextElo = RankThresholds[tier + 1];
        return Mathf.Clamp01((float)(elo - minElo) / Mathf.Max(1, nextElo - minElo));
    }

    public static string FormatNextRankProgress(int elo)
    {
        int tier = GetRankTierIndex(elo);
        if (tier >= RankThresholds.Length - 1)
            return $"{elo} ELO  •  MAX RANK";

        int nextElo = RankThresholds[tier + 1];
        string nextName = RankNames[tier + 1].ToUpper();
        return $"{elo} / {nextElo} -> {nextName}";
    }

    public static Color GetRankColor(int elo)
    {
        return GetRankColorByTier(GetRankTierIndex(elo));
    }

    public static Color GetRankColorByTier(int tier)
    {
        switch (Mathf.Clamp(tier, 0, 4))
        {
            case 0: return new Color(0.86f, 0.56f, 0.30f, 1f);
            case 1: return new Color(0.80f, 0.87f, 0.94f, 1f);
            case 2: return new Color(1.00f, 0.82f, 0.24f, 1f);
            case 3: return new Color(0.28f, 0.92f, 0.88f, 1f);
            default: return new Color(0.66f, 0.52f, 1.00f, 1f);
        }
    }

    public static string GetRankBucket(int elo)
    {
        int tier = GetRankTierIndex(elo);
        if (tier <= 0) return "bronze";
        if (tier <= 1) return "silver";
        return "gold";
    }

    // ================= RECORD MATCH =================

    public static MatchResultSummary RecordMatchResult(
        GameMatchMode mode,
        bool isVictory,
        int placement,
        int totalPlayers,
        bool creditGoldImmediately = false
    )
    {
        suppressCloudAutoSave = true;

        int safeTotal = Mathf.Max(2, totalPlayers);
        int safePlacement = Mathf.Clamp(placement, 1, safeTotal);

        int oldElo = PlayerElo;
        int newElo = oldElo;
        int eloDelta = 0;

        int wins = SoloWins;
        int losses = SoloLosses;

        int goldEarned = 0;

        if (mode == GameMatchMode.Solo)
        {
            if (isVictory)
            {
                wins += 1;
                SoloWins = wins;
                goldEarned = 90;
            }
            else
            {
                losses += 1;
                SoloLosses = losses;
                goldEarned = 35;
            }
        }
        else if (mode == GameMatchMode.Rank)
        {
            if (safePlacement == 1 || isVictory)
            {
                eloDelta = 45;
                goldEarned = 120;
            }
            else if (safePlacement == 2)
            {
                eloDelta = safeTotal >= 3 ? 20 : -20;
                goldEarned = 75;
            }
            else if (safePlacement == 3)
            {
                eloDelta = -15;
                goldEarned = 50;
            }
            else
            {
                eloDelta = -25;
                goldEarned = 35;
            }

            newElo = Mathf.Max(0, oldElo + eloDelta);
            PlayerElo = newElo;
        }
        else if (mode == GameMatchMode.Custom)
        {
            goldEarned = isVictory ? 50 : 20;
        }
        else
        {
            goldEarned = 30;
        }

        // Chơi xong ván đầu tiên -> tự động gửi THƯ QUÀ TÂN THỦ (500 vàng + Chiêu Đóng Băng)!
        MatchesPlayed = MatchesPlayed + 1;
        bool unlockedNewbieNow = false;
        if (!NewbieMailUnlocked)
        {
            PlayerPrefs.SetInt(KEY_NEWBIE_MAIL_UNLOCKED, 1);
            unlockedNewbieNow = true;
        }

        int totalGold = PlayerGold;
        if (creditGoldImmediately && goldEarned > 0)
        {
            totalGold += goldEarned;
            PlayerGold = totalGold;
        }

        PlayerPrefs.Save();
        suppressCloudAutoSave = false;

        LeaderboardManager.RecordLocalPlayerSnapshot();
        NotifyChanged();

        int oldTier = GetRankTierIndex(oldElo);
        int newTier = GetRankTierIndex(newElo);

        return new MatchResultSummary
        {
            mode = mode,
            isVictory = isVictory,
            placement = safePlacement,
            totalPlayers = safeTotal,
            soloWins = wins,
            soloLosses = losses,
            oldElo = oldElo,
            newElo = newElo,
            eloDelta = eloDelta,
            oldTier = oldTier,
            newTier = newTier,
            isRankUp = mode == GameMatchMode.Rank && newTier > oldTier,
            isRankDown = mode == GameMatchMode.Rank && newTier < oldTier,
            rankTitle = GetRankTitle(newElo),
            nextRankTitle = GetNextRankTitle(newElo),
            nextRankElo = GetNextRankElo(newElo),
            goldEarned = goldEarned,
            totalGold = totalGold + (creditGoldImmediately ? 0 : goldEarned),
            newbieMailJustUnlocked = unlockedNewbieNow
        };
    }

    public static string FormatMenuStatsLine()
    {
        int elo = PlayerElo;
        string tier = GetRankTitle(elo);
        return $"{PlayerName}  |  {tier.ToUpper()} ({elo})  |  SOLO: {SoloWins}W-{SoloLosses}L  |  {PlayerGold}";
    }
}

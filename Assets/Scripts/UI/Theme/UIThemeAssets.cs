using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIThemeAssets
{
    // ================= UNIFIED CYBER-DARK THEME PALETTE =================
    public static readonly Color PanelBackdrop = new Color(0.03f, 0.05f, 0.10f, 0.84f);
    public static readonly Color CardBorder = new Color(0.22f, 0.64f, 0.98f, 0.95f);
    public static readonly Color CardBody = new Color(0.07f, 0.10f, 0.17f, 0.96f);
    public static readonly Color SubCardBg = new Color(0.11f, 0.15f, 0.24f, 0.96f);
    public static readonly Color PrimaryCyan = new Color(0.18f, 0.56f, 0.94f, 1.00f);
    public static readonly Color GoldAccent = new Color(1.00f, 0.82f, 0.22f, 1.00f);
    public static readonly Color DangerRed = new Color(0.76f, 0.22f, 0.26f, 1.00f);
    public static readonly Color SuccessGreen = new Color(0.14f, 0.66f, 0.38f, 1.00f);
    public static readonly Color CooldownMask = new Color(0.03f, 0.05f, 0.10f, 0.72f);

    static Sprite heartFull;
    static Sprite heartBroken;
    static Sprite btnCircleFrame;
    static Sprite btnChargeRing;
    static Sprite iconParry;
    static Sprite iconDash;
    static Sprite iconJump;
    static Sprite iconCloneBall;
    static Sprite iconInvisible;
    static Sprite iconTornado;
    static Sprite iconFreeze;
    static Sprite iconForcefield;
    static Sprite iconTelekinesis;
    static Sprite iconLock;
    static Sprite iconGold;
    static Sprite iconRankBronze;
    static Sprite iconRankSilver;
    static Sprite iconRankGold;
    static Sprite iconRankPlatinum;
    static Sprite iconRankDiamond;
    static Sprite iconLeaderboard;
    static Sprite iconGuide;
    static Sprite iconMail;
    static Sprite iconPlus;
    static Sprite iconGoogle;
    static Sprite iconLogout;
    static Sprite iconGiftBox;
    static Sprite bgCardRank;
    static Sprite bgCardSolo;
    static Sprite bgMenuArena;
    static Sprite iconSkinShop;
    static readonly Dictionary<WeaponType, Sprite> weaponIconCache = new Dictionary<WeaponType, Sprite>();
    static readonly Dictionary<SkillType, Sprite> skillIconCache = new Dictionary<SkillType, Sprite>();
    static readonly Dictionary<SkinType, Sprite> skinIconCache = new Dictionary<SkinType, Sprite>();

    static Sprite LoadIcon(ref Sprite cache, string resourceName)
    {
        if (cache == null)
        {
            cache = Resources.Load<Sprite>("UI/" + resourceName);
            if (cache == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("UI/" + resourceName);
                if (tex != null)
                {
                    cache = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }
            }
        }
        return cache;
    }

    public static Sprite HeartFull => LoadIcon(ref heartFull, "Heart_Full");
    public static Sprite HeartBroken => LoadIcon(ref heartBroken, "Heart_Broken");
    public static Sprite BtnCircleFrame => LoadIcon(ref btnCircleFrame, "Btn_Circle_Frame");
    public static Sprite BtnChargeRing => LoadIcon(ref btnChargeRing, "Btn_Charge_Ring");
    public static Sprite IconParry => LoadIcon(ref iconParry, "Icon_Parry");
    public static Sprite IconDash => LoadIcon(ref iconDash, "Icon_Dash");
    public static Sprite IconJump => LoadIcon(ref iconJump, "Icon_Jump");
    public static Sprite IconCloneBall => LoadIcon(ref iconCloneBall, "Icon_CloneBall");
    public static Sprite IconInvisible => LoadIcon(ref iconInvisible, "Icon_Invisible");
    public static Sprite IconTornado => LoadIcon(ref iconTornado, "Icon_Tornado");
    public static Sprite IconFreeze => LoadIcon(ref iconFreeze, "Icon_Freeze");
    public static Sprite IconForcefield => LoadIcon(ref iconForcefield, "Icon_Forcefield");
    public static Sprite IconTelekinesis => LoadIcon(ref iconTelekinesis, "Icon_Telekinesis");
    public static Sprite IconLock => LoadIcon(ref iconLock, "Icon_Lock");
    public static Sprite IconGold => LoadIcon(ref iconGold, "Icon_Gold");
    public static Sprite IconRankBronze => LoadIcon(ref iconRankBronze, "Icon_Rank_Bronze");
    public static Sprite IconRankSilver => LoadIcon(ref iconRankSilver, "Icon_Rank_Silver");
    public static Sprite IconRankGold => LoadIcon(ref iconRankGold, "Icon_Rank_Gold");
    public static Sprite IconRankPlatinum => LoadIcon(ref iconRankPlatinum, "Icon_Rank_Platinum");
    public static Sprite IconRankDiamond => LoadIcon(ref iconRankDiamond, "Icon_Rank_Diamond");
    public static Sprite IconLeaderboard => LoadIcon(ref iconLeaderboard, "Icon_Leaderboard");
    public static Sprite IconGuide => LoadIcon(ref iconGuide, "Icon_Guide");
    public static Sprite IconMail => LoadIcon(ref iconMail, "Icon_Mail");
    public static Sprite IconPlus => LoadIcon(ref iconPlus, "Icon_Plus");
    public static Sprite IconGoogle => LoadIcon(ref iconGoogle, "Icon_Google");
    public static Sprite IconLogout => LoadIcon(ref iconLogout, "Icon_Logout");
    public static Sprite IconGiftBox => LoadIcon(ref iconGiftBox, "Icon_GiftBox");
    public static Sprite BgCardRank => LoadIcon(ref bgCardRank, "Bg_Card_Rank");
    public static Sprite BgCardSolo => LoadIcon(ref bgCardSolo, "Bg_Card_Solo");
    public static Sprite BgMenuArena => LoadIcon(ref bgMenuArena, "Bg_Menu_Arena");
    public static Sprite IconSkinShop => LoadIcon(ref iconSkinShop, "Icon_SkinShop");

    public static void SetRuntimeSkinIcon(SkinType type, Sprite sprite)
    {
        if (sprite != null)
        {
            skinIconCache[type] = sprite;
        }
    }

    public static Sprite GetSkinIcon(SkinType type)
    {
        if (skinIconCache.TryGetValue(type, out Sprite cached) && cached != null)
            return cached;

        SkinDefinition def = SkinCatalog.Get(type);
        Sprite sp = null;
        if (def != null && !string.IsNullOrEmpty(def.iconResource))
        {
            LoadIcon(ref sp, def.iconResource);
        }
        if (sp == null) sp = IconSkinShop;
        if (sp != null) skinIconCache[type] = sp;
        return sp;
    }

    public static Sprite GetRankIcon(int elo)
    {
        return GetRankIconByTier(PlayerStatsManager.GetRankTierIndex(elo));
    }

    public static Sprite GetRankIconByTier(int tier)
    {
        switch (Mathf.Clamp(tier, 0, 4))
        {
            case 0: return IconRankBronze;
            case 1: return IconRankSilver;
            case 2: return IconRankGold;
            case 3: return IconRankPlatinum;
            default: return IconRankDiamond;
        }
    }

    public static Sprite GetWeaponIcon(WeaponType type)
    {
        if (weaponIconCache.TryGetValue(type, out Sprite cached) && cached != null)
            return cached;

        WeaponDefinition def = WeaponCatalog.Get(type);
        Sprite sp = null;
        LoadIcon(ref sp, def.iconResource);
        if (sp != null) weaponIconCache[type] = sp;
        return sp;
    }

    public static Sprite GetSkillIcon(SkillType skill)
    {
        if (skillIconCache.TryGetValue(skill, out Sprite cached) && cached != null)
            return cached;

        SkillDefinition def = SkillCatalog.Get(skill);
        Sprite sp = null;
        if (def != null && !string.IsNullOrEmpty(def.iconResource))
        {
            LoadIcon(ref sp, def.iconResource);
        }
        if (sp == null) sp = IconCloneBall;
        if (sp != null) skillIconCache[skill] = sp;
        return sp;
    }

    public static Color GetSkillAccentColor(SkillType skill)
    {
        SkillDefinition def = SkillCatalog.Get(skill);
        return def != null ? def.accentColor : PrimaryCyan;
    }

    public static Image ApplyUnifiedHudCircleButton(
        RectTransform btnRt,
        RectTransform cooldownRt,
        Sprite iconSprite,
        string shortLabel,
        Vector2 anchoredPos,
        float diameter,
        TMP_FontAsset font
    )
    {
        if (btnRt == null)
            return null;

        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.localScale = Vector3.one;
        btnRt.sizeDelta = new Vector2(diameter, diameter);
        btnRt.anchoredPosition = anchoredPos;

        Image bg = btnRt.GetComponent<Image>();
        if (bg == null) bg = btnRt.gameObject.AddComponent<Image>();
        if (BtnCircleFrame != null)
        {
            bg.sprite = BtnCircleFrame;
            bg.preserveAspect = true;
        }
        bg.color = Color.white;
        bg.raycastTarget = true;

        Button btn = btnRt.GetComponent<Button>();
        if (btn != null)
        {
            btn.targetGraphic = bg;
        }

        // Ẩn các text/child cũ bị lệch/méo (giữ lại cooldownRt và các child thống nhất)
        for (int i = 0; i < btnRt.childCount; i++)
        {
            Transform ch = btnRt.GetChild(i);
            if (ch == null) continue;
            if (cooldownRt != null && ch == cooldownRt) continue;
            if (ch.name == "UnifiedIcon" || ch.name == "UnifiedLabel" || ch.name == "Charge Circle") continue;
            ch.gameObject.SetActive(false);
        }

        // Icon chính giữa nút
        Transform iconTf = btnRt.Find("UnifiedIcon");
        Image iconImg;
        if (iconTf == null)
        {
            GameObject iconGo = new GameObject("UnifiedIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(btnRt, false);
            iconTf = iconGo.transform;
            iconImg = iconGo.GetComponent<Image>();
        }
        else
        {
            iconImg = iconTf.GetComponent<Image>();
        }

        RectTransform irt = iconTf as RectTransform;
        irt.anchorMin = new Vector2(0.18f, 0.25f);
        irt.anchorMax = new Vector2(0.82f, 0.87f);
        irt.localScale = Vector3.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        iconImg.enabled = iconSprite != null;

        // Nhãn gọn dưới icon
        if (!string.IsNullOrEmpty(shortLabel))
        {
            Transform lblTf = btnRt.Find("UnifiedLabel");
            TextMeshProUGUI lblTxt;
            if (lblTf == null)
            {
                GameObject lblGo = new GameObject("UnifiedLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                lblGo.transform.SetParent(btnRt, false);
                lblTf = lblGo.transform;
                lblTxt = lblGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                lblTxt = lblTf.GetComponent<TextMeshProUGUI>();
            }

            RectTransform lrt = lblTf as RectTransform;
            lrt.anchorMin = new Vector2(0.10f, 0.08f);
            lrt.anchorMax = new Vector2(0.90f, 0.28f);
            lrt.localScale = Vector3.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            if (font != null) lblTxt.font = font;
            lblTxt.text = shortLabel;
            lblTxt.enableAutoSizing = true;
            lblTxt.fontSizeMin = 12f;
            lblTxt.fontSizeMax = 24f;
            lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.alignment = TextAlignmentOptions.Center;
            lblTxt.color = new Color(0.82f, 0.92f, 1.00f, 0.95f);
            lblTxt.raycastTarget = false;
        }

        // Căn chỉnh vòng cooldown vừa khít bên trong khung nút
        if (cooldownRt != null)
        {
            cooldownRt.SetAsLastSibling();
            cooldownRt.anchorMin = Vector2.zero;
            cooldownRt.anchorMax = Vector2.one;
            cooldownRt.pivot = new Vector2(0.5f, 0.5f);
            cooldownRt.localScale = Vector3.one;
            cooldownRt.offsetMin = new Vector2(6f, 6f);
            cooldownRt.offsetMax = new Vector2(-6f, -6f);

            Image cdImg = cooldownRt.GetComponent<Image>();
            if (cdImg != null)
            {
                if (BtnCircleFrame != null) cdImg.sprite = BtnCircleFrame;
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = false;
                cdImg.color = CooldownMask;
                cdImg.raycastTarget = false;
            }
        }

        return iconImg;
    }
}

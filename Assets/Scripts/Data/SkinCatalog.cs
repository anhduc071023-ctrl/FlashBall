using UnityEngine;

public enum SkinType
{
    DefaultStriker = 0, // Bo 1: Nam - Tan Binh mac do don gian
    CyberRunner = 1,    // Bo 2: Nam - Cyber Runner co doi canh Cyber
    CrimsonSamurai = 2, // Bo 3: Nam - Huyet Ronin
    StormValkyrie = 3,  // Bo 4: Nu - Bao To Valkyrie mac quan ngan + vang sang cuc dep
    SolarPaladin = 4,   // Bo 5: Nam - Thanh Hiep Anh Kim (canh thien than dung chieu!)
    VoidReaper = 5      // Bo 6: Nu - Nu Tu Than (toc dai + vuong mien Gothic + ao choang mem!)
}

public enum SkinArchetypeStyle
{
    StarterSimpleMale,
    CyberWingedMale,
    RoninKasaMale,
    ValkyrieShortsFemale,
    PaladinWingsMale,
    ReaperCloakFemale
}

public class SkinDefinition
{
    public SkinType type;
    public SkinArchetypeStyle style;
    public bool isFemale;
    public bool hasShortPants;
    public string displayName;
    public string rarityLabel;
    public int price;
    public string description;
    public string iconResource;
    public string headModelResource;
    public string bodyModelResource;
    public string waistModelResource;
    public string leftWingResource;
    public string rightWingResource;
    public Color suitBaseColor;
    public Color suitSecondaryColor;
    public Color suitAccentColor;
    public Color emissionColor;
    public Color skinToneColor;
    public float suitMetallic;
    public float suitSmoothness;
    public bool hasAmbientParticles;
}

public static class SkinCatalog
{
    static readonly SkinDefinition[] skins = new SkinDefinition[]
    {
        new SkinDefinition
        {
            type = SkinType.DefaultStriker,
            style = SkinArchetypeStyle.StarterSimpleMale,
            isFemale = false,
            hasShortPants = false,
            displayName = "TAN BINH",
            rarityLabel = "NAM • CO BAN",
            price = 0,
            description = "Nhan vat Nam: Trang phuc the thao don gian gon gang (ao khoac xanh-trang, quan dai toi mau & bang do).",
            iconResource = "Icon_Skin_Default",
            headModelResource = "Skin_StarterHead",
            bodyModelResource = "Skin_StarterTop",
            waistModelResource = "Skin_StarterWaist",
            leftWingResource = "",
            rightWingResource = "",
            suitBaseColor = new Color(0.14f, 0.22f, 0.36f, 1f),
            suitSecondaryColor = new Color(0.10f, 0.12f, 0.18f, 1f),
            suitAccentColor = new Color(0.24f, 0.76f, 1.00f, 1f),
            emissionColor = new Color(0.06f, 0.24f, 0.42f, 1f),
            skinToneColor = new Color(0.92f, 0.76f, 0.64f, 1f),
            suitMetallic = 0.22f,
            suitSmoothness = 0.50f,
            hasAmbientParticles = false
        },
        new SkinDefinition
        {
            type = SkinType.CyberRunner,
            style = SkinArchetypeStyle.CyberWingedMale,
            isFemale = false,
            hasShortPants = false,
            displayName = "CYBER RUNNER",
            rarityLabel = "NAM • CONG NGHE",
            price = 300,
            description = "Nhan vat Nam: Giap carbon cong nghe, kinh Visor neon & DOI CANH CYBER ANH SANG huong ra sau!",
            iconResource = "Icon_Skin_CyberRunner",
            headModelResource = "Skin_CyberHelmet",
            bodyModelResource = "Skin_CyberJetpack",
            waistModelResource = "Skin_CyberWaist",
            leftWingResource = "Skin_CyberWingL",
            rightWingResource = "Skin_CyberWingR",
            suitBaseColor = new Color(0.06f, 0.09f, 0.15f, 1f),
            suitSecondaryColor = new Color(0.14f, 0.22f, 0.34f, 1f),
            suitAccentColor = new Color(0.08f, 0.96f, 1.00f, 1f),
            emissionColor = new Color(0.06f, 0.68f, 0.88f, 1f),
            skinToneColor = new Color(0.90f, 0.74f, 0.62f, 1f),
            suitMetallic = 0.80f,
            suitSmoothness = 0.90f,
            hasAmbientParticles = true
        },
        new SkinDefinition
        {
            type = SkinType.CrimsonSamurai,
            style = SkinArchetypeStyle.RoninKasaMale,
            isFemale = false,
            hasShortPants = false,
            displayName = "HUYET RONIN",
            rarityLabel = "NAM • HIEM",
            price = 550,
            description = "Nhan vat Nam: Non la Kasa vo si dao, mat na Oni, khan lua do bay mem mai & song kiem ben hong!",
            iconResource = "Icon_Skin_CrimsonSamurai",
            headModelResource = "Skin_SamuraiKabuto",
            bodyModelResource = "",
            waistModelResource = "Skin_SamuraiWaist",
            leftWingResource = "",
            rightWingResource = "",
            suitBaseColor = new Color(0.36f, 0.04f, 0.06f, 1f),
            suitSecondaryColor = new Color(0.10f, 0.06f, 0.07f, 1f),
            suitAccentColor = new Color(1.00f, 0.78f, 0.18f, 1f),
            emissionColor = new Color(0.58f, 0.10f, 0.04f, 1f),
            skinToneColor = new Color(0.92f, 0.76f, 0.64f, 1f),
            suitMetallic = 0.36f,
            suitSmoothness = 0.56f,
            hasAmbientParticles = true
        },
        new SkinDefinition
        {
            type = SkinType.StormValkyrie,
            style = SkinArchetypeStyle.ValkyrieShortsFemale,
            isFemale = true,
            hasShortPants = true,
            displayName = "BAO TO VALKYRIE",
            rarityLabel = "NU • SU THI",
            price = 800,
            description = "Nhan vat Nu: Toc duoi ngua bac xanh, ao giap nu, QUAN NGAN chien dau & VANG HAO QUANG LOI THAN ruc ro sau lung!",
            iconResource = "Icon_Skin_StormValkyrie",
            headModelResource = "Skin_ValkyrieHelm",
            bodyModelResource = "Skin_ValkyrieTop",
            waistModelResource = "Skin_ValkyrieShorts",
            leftWingResource = "",
            rightWingResource = "",
            suitBaseColor = new Color(0.10f, 0.26f, 0.62f, 1f),
            suitSecondaryColor = new Color(0.84f, 0.90f, 1.00f, 1f),
            suitAccentColor = new Color(0.32f, 0.92f, 1.00f, 1f),
            emissionColor = new Color(0.18f, 0.62f, 1.00f, 1f),
            skinToneColor = new Color(0.96f, 0.82f, 0.73f, 1f),
            suitMetallic = 0.76f,
            suitSmoothness = 0.90f,
            hasAmbientParticles = true
        },
        new SkinDefinition
        {
            type = SkinType.SolarPaladin,
            style = SkinArchetypeStyle.PaladinWingsMale,
            isFemale = false,
            hasShortPants = false,
            displayName = "THANH HIEP ANH KIM",
            rarityLabel = "NAM • HUYEN THOAI",
            price = 1100,
            description = "Nhan vat Nam: Vong hao quang tren dau, giap trang ma vang & DOI CANH THIEN THAN 3D uon cong dep mat vo canh thuc!",
            iconResource = "Icon_Skin_SolarPaladin",
            headModelResource = "Skin_PaladinCrown",
            bodyModelResource = "",
            waistModelResource = "",
            leftWingResource = "Skin_PaladinWingL",
            rightWingResource = "Skin_PaladinWingR",
            suitBaseColor = new Color(0.95f, 0.96f, 1.00f, 1f),
            suitSecondaryColor = new Color(0.88f, 0.72f, 0.22f, 1f),
            suitAccentColor = new Color(1.00f, 0.84f, 0.22f, 1f),
            emissionColor = new Color(0.82f, 0.62f, 0.14f, 1f),
            skinToneColor = new Color(0.92f, 0.77f, 0.65f, 1f),
            suitMetallic = 0.78f,
            suitSmoothness = 0.94f,
            hasAmbientParticles = true
        },
        new SkinDefinition
        {
            type = SkinType.VoidReaper,
            style = SkinArchetypeStyle.ReaperCloakFemale,
            isFemale = true,
            hasShortPants = false,
            displayName = "NU TU THAN",
            rarityLabel = "NU • TOI THUONG",
            price = 1500,
            description = "Nu Tu Than Bóng Đêm: Mu trum nhon vien vang co dien + toc bac trang, corset tim than xe cao lo doi chan dai trang & ao choang rong vien vang bay gio!",
            iconResource = "Icon_Skin_VoidReaper",
            headModelResource = "Skin_ReaperHood",
            bodyModelResource = "Skin_ReaperArmor",
            waistModelResource = "Skin_ReaperWaist",
            leftWingResource = "",
            rightWingResource = "",
            suitBaseColor = new Color(0.12f, 0.055f, 0.18f, 1f),
            suitSecondaryColor = new Color(0.85f, 0.70f, 0.36f, 1f),
            suitAccentColor = new Color(0.86f, 0.32f, 1.00f, 1f),
            emissionColor = new Color(0.62f, 0.16f, 0.96f, 1f),
            skinToneColor = new Color(0.97f, 0.89f, 0.86f, 1f),
            suitMetallic = 0.20f,
            suitSmoothness = 0.42f,
            hasAmbientParticles = true
        }
    };

    public static SkinDefinition[] All => skins;

    public static SkinDefinition Get(SkinType type)
    {
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i].type == type)
                return skins[i];
        }
        return skins[0];
    }
}

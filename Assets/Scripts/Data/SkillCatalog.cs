using UnityEngine;

public class SkillDefinition
{
    public SkillType type;
    public int powerRank;        // 1 (Yeu nhat) -> 6 (Manh nhat)
    public string tierLabel;     // CO BAN -> TOI THUONG
    public string displayName;
    public string shortName;
    public string tagLabel;
    public float cooldown;
    public int price;
    public string statsSummary;
    public string description;
    public string iconResource;
    public Color tierColor;      // Mau khung tu Xanh La -> Xanh Ngoc -> Xanh Duong -> Cham -> Tim Lam -> Tim Dam
    public Color accentColor;
}

public static class SkillCatalog
{
    // Sap xep tu YEU DEN MANH (da test can bang qua 3,000 tran gia lap),
    // Mau khung chuyen muot tu XANH LA -> XANH NGOC -> XANH DUONG -> XANH CHAM -> TIM LAM -> TIM DAM
    // Gia tien tang dan tuong ung suc manh!
    static readonly SkillDefinition[] skills = new SkillDefinition[]
    {
        new SkillDefinition
        {
            type = SkillType.CloneBall,
            powerRank = 1,
            tierLabel = "CO BAN",
            displayName = "CLONE BALL",
            shortName = "CLONE",
            tagLabel = "GAY NHIEU",
            cooldown = 10f,
            price = 0,
            statsSummary = "CD: 10s  •  1 Bong Gia",
            description = "Tao qua bong gia lao ve phia doi thu de lua parry. Bong gia vo tan khi cham hoac bi chem.",
            iconResource = "Icon_CloneBall",
            tierColor = new Color(0.18f, 0.88f, 0.46f, 1f),   // Xanh La (Common / Starter)
            accentColor = new Color(0.18f, 0.88f, 0.46f, 1f)
        },
        new SkillDefinition
        {
            type = SkillType.Freeze,
            powerRank = 2,
            tierLabel = "SO CAP",
            displayName = "FREEZE",
            shortName = "DONG BANG",
            tagLabel = "LAM CHAM",
            cooldown = 16f,
            price = 250,
            statsSummary = "CD: 16s  •  Lam cham 2.8s  •  De Parry",
            description = "Dong bang boc tinh the bang quanh bong, lam bong bay CUC CHAM ve phia ban trong 2.8 giay de de dang can nhip Parry!",
            iconResource = "Icon_Freeze",
            tierColor = new Color(0.10f, 0.86f, 0.78f, 1f),   // Xanh Ngoc (Uncommon)
            accentColor = new Color(0.18f, 0.88f, 0.96f, 1f)
        },
        new SkillDefinition
        {
            type = SkillType.Telekinesis,
            powerRank = 3,
            tierLabel = "TRUNG CAP",
            displayName = "TELEKINESIS",
            shortName = "NIEM LUC",
            tagLabel = "PHAN CONG",
            cooldown = 15f,
            price = 450,
            statsSummary = "CD: 15s  •  Bat 0.42s  •  Phan nguoc 1.95x",
            description = "Dung niem luc bat dung bong (0.42s) va PHAN NGUOC THANG ve phia ke dich vua parry voi toc do sieu nhanh 1.95x!",
            iconResource = "Icon_Telekinesis",
            tierColor = new Color(0.18f, 0.58f, 1.00f, 1f),   // Xanh Duong (Rare)
            accentColor = new Color(0.24f, 0.66f, 1.00f, 1f)
        },
        new SkillDefinition
        {
            type = SkillType.Forcefield,
            powerRank = 4,
            tierLabel = "CAO CAP",
            displayName = "FORCEFIELD",
            shortName = "KHIEN LUC",
            tagLabel = "PHONG THU",
            cooldown = 22f,
            price = 650,
            statsSummary = "CD: 22s  •  Khien 2.6s  •  Tu Dong Parry",
            description = "Bat vom khien nang luong trong 2.6 giay, TU DONG phan don (Power Parry) moi qua bong lao vao ban!",
            iconResource = "Icon_Forcefield",
            tierColor = new Color(0.38f, 0.44f, 1.00f, 1f),   // Xanh Cham (Epic-Low)
            accentColor = new Color(0.42f, 0.50f, 1.00f, 1f)
        },
        new SkillDefinition
        {
            type = SkillType.Invisible,
            powerRank = 5,
            tierLabel = "SIEU CAP",
            displayName = "INVISIBLE",
            shortName = "TANG HINH",
            tagLabel = "AN THAN",
            cooldown = 20f,
            price = 850,
            statsSummary = "CD: 20s  •  Mo 4.2s  •  +55% Toc Chay",
            description = "Lam mo nhan vat & vu khi trong 4.2 giay, tang 55% toc do di chuyen va khien bong hoan toan bo qua ban!",
            iconResource = "Icon_Invisible",
            tierColor = new Color(0.60f, 0.30f, 1.00f, 1f),   // Tim Lam (Epic-High)
            accentColor = new Color(0.66f, 0.36f, 1.00f, 1f)
        },
        new SkillDefinition
        {
            type = SkillType.Tornado,
            powerRank = 6,
            tierLabel = "TOI THUONG",
            displayName = "TORNADO",
            shortName = "TORNADO",
            tagLabel = "HUY DIET",
            cooldown = 25f,
            price = 1200,
            statsSummary = "CD: 25s  •  Loc 2.0s  •  Bo Thang 62 m/s",
            description = "Ky nang manh nhat! Cuon bong bay xoan oc len cao tranh sat thuong, roi BO THANG cuc nhanh 62 m/s ha guc doi thu!",
            iconResource = "Icon_Tornado",
            tierColor = new Color(0.84f, 0.18f, 1.00f, 1f),   // Tim Dam Hoang Gia (Mythic)
            accentColor = new Color(0.86f, 0.24f, 1.00f, 1f)
        }
    };

    public static SkillDefinition[] All => skills;

    public static SkillDefinition Get(SkillType type)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].type == type)
                return skills[i];
        }
        return skills[0];
    }
}

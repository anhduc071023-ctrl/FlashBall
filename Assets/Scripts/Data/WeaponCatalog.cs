using UnityEngine;

public enum WeaponType
{
    WindBlade = 0,
    FlameSaber = 1,
    ThunderSpear = 2,
    FrostDualBlades = 3,
    VoidScythe = 4
}

public class WeaponDefinition
{
    public WeaponType type;
    public string displayName;
    public string categoryLabel;
    public string elementLabel;
    public int price;
    public string description;
    public string iconResource;
    public string modelResource;
    public Color primaryColor;
    public Color coreColor;
    public Color secondaryColor;
}

public static class WeaponCatalog
{
    static readonly WeaponDefinition[] weapons = new WeaponDefinition[]
    {
        new WeaponDefinition
        {
            type = WeaponType.WindBlade,
            displayName = "PHONG KIEM",
            categoryLabel = "KIEM",
            elementLabel = "NGUYEN TO GIO",
            price = 0,
            description = "Truong kiem phong bao thanh thoat.\nVet chem luoi liem gio xanh ngoc sac lem.",
            iconResource = "Icon_Weapon_WindBlade",
            modelResource = "Weapon_WindBlade",
            primaryColor = new Color(0.10f, 0.88f, 0.72f, 1f),
            coreColor = new Color(0.88f, 1.00f, 0.96f, 1f),
            secondaryColor = new Color(0.05f, 0.56f, 0.58f, 1f)
        },
        new WeaponDefinition
        {
            type = WeaponType.FlameSaber,
            displayName = "HOA DAO",
            categoryLabel = "DAO CONG",
            elementLabel = "NGUYEN TO LUA",
            price = 250,
            description = "Dai dao cong ruc lua chay bong.\nVet chem vong lua do cam + mua tan lua bay len.",
            iconResource = "Icon_Weapon_FlameSaber",
            modelResource = "Weapon_FlameSaber",
            primaryColor = new Color(1.00f, 0.34f, 0.05f, 1f),
            coreColor = new Color(1.00f, 0.92f, 0.45f, 1f),
            secondaryColor = new Color(0.82f, 0.10f, 0.02f, 1f)
        },
        new WeaponDefinition
        {
            type = WeaponType.ThunderSpear,
            displayName = "LOI THUONG",
            categoryLabel = "TRUONG THUONG",
            elementLabel = "NGUYEN TO SAM SET",
            price = 450,
            description = "Truong thuong mui set dai uy luc.\nVet chem dien xanh-vang + phong tia set zig-zag.",
            iconResource = "Icon_Weapon_ThunderSpear",
            modelResource = "Weapon_ThunderSpear",
            primaryColor = new Color(0.24f, 0.62f, 1.00f, 1f),
            coreColor = new Color(1.00f, 0.96f, 0.52f, 1f),
            secondaryColor = new Color(0.45f, 0.25f, 1.00f, 1f)
        },
        new WeaponDefinition
        {
            type = WeaponType.FrostDualBlades,
            displayName = "BANG SONG KIEM",
            categoryLabel = "SONG KIEM",
            elementLabel = "NGUYEN TO BANG",
            price = 700,
            description = "Cap song kiem pha le bang cam 2 tay.\nChem doi luoi bang gia + manh tuyet vo lap lanh.",
            iconResource = "Icon_Weapon_FrostDual",
            modelResource = "Weapon_FrostDual",
            primaryColor = new Color(0.18f, 0.78f, 1.00f, 1f),
            coreColor = new Color(0.92f, 0.99f, 1.00f, 1f),
            secondaryColor = new Color(0.08f, 0.38f, 0.88f, 1f)
        },
        new WeaponDefinition
        {
            type = WeaponType.VoidScythe,
            displayName = "HU KHONG HAI",
            categoryLabel = "LUOI HAI",
            elementLabel = "NGUYEN TO HU KHONG",
            price = 1000,
            description = "Luoi hai tu than trang khuyet khong lo.\nVet chem tim-hong neon cuc dai + xoay hu khong.",
            iconResource = "Icon_Weapon_VoidScythe",
            modelResource = "Weapon_VoidScythe",
            primaryColor = new Color(0.76f, 0.20f, 1.00f, 1f),
            coreColor = new Color(1.00f, 0.68f, 0.98f, 1f),
            secondaryColor = new Color(0.32f, 0.05f, 0.58f, 1f)
        }
    };

    public static WeaponDefinition[] All => weapons;

    public static WeaponDefinition Get(WeaponType type)
    {
        int idx = (int)type;
        if (idx >= 0 && idx < weapons.Length)
            return weapons[idx];
        return weapons[0];
    }
}

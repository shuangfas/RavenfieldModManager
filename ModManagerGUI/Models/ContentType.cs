using Ardalis.SmartEnum;
using ModManagerGUI.Models.Interfaces;

namespace ModManagerGUI.Models;
//todo 合并一下SmartEnum<ContentType, string>,ITranslatableKey,ISerializableData
public sealed class ContentType : SmartEnum<ContentType, string>,ITranslatableKey,ISpecifiedTypeSerializing {
    public static readonly ContentType WEAPON_CONTENT = new(nameof(WEAPON_CONTENT), "WeaponContentMod","Right.Content.Weapon");
    public static readonly ContentType VEHICLE_CONTENT = new(nameof(VEHICLE_CONTENT), "VehicleContentMod","Right.Content.Vehicle");

    private readonly string _translatableKey;
    private ContentType(string name, string keyName,string translatableKey) : base(name, keyName) {
        _translatableKey = translatableKey;
    }

    public string GetTranslatableKey() {
        return _translatableKey;
    }

    public string GetJsonKey() {
        return Name;
    }

    public string GetSerializedType() {
        return nameof(ContentType);
    }
}

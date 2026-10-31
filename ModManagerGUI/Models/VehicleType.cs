using Ardalis.SmartEnum;
using ModManagerGUI.Models.Interfaces;

namespace ModManagerGUI.Models;

public sealed class VehicleType : SmartEnum<VehicleType, string>,ITranslatableKey,ISpecifiedTypeSerializing{
    public static readonly VehicleType JEEP = new(nameof(JEEP), "jeep", "Right.Content.Vehicle.Jeep");
    public static readonly VehicleType JEEP_MACHINE_GUN = new(nameof(JEEP_MACHINE_GUN), "jeepMachineGun", "Right.Content.Vehicle.JeepMachineGun");
    public static readonly VehicleType QUAD = new(nameof(QUAD), "quad", "Right.Content.Vehicle.Quad");
    public static readonly VehicleType TANK = new(nameof(TANK), "tank", "Right.Content.Vehicle.Tank");
    public static readonly VehicleType APC = new(nameof(APC), "apc", "Right.Content.Vehicle.APC");
    public static readonly VehicleType ATTACK_BOAT = new(nameof(ATTACK_BOAT), "attackBoat", "Right.Content.Vehicle.AttackBoat");
    public static readonly VehicleType RHIB = new(nameof(RHIB), "rhib", "Right.Content.Vehicle.RHIB");
    public static readonly VehicleType ATTACK_HELICOPTER = new(nameof(ATTACK_HELICOPTER), "attackHelicopter", "Right.Content.Vehicle.AttackHelicopter");
    public static readonly VehicleType TRANSPORT_HELICOPTER = new(nameof(TRANSPORT_HELICOPTER), "transportHelicopter", "Right.Content.Vehicle.TransportHelicopter");
    public static readonly VehicleType ATTACK_PLANE = new(nameof(ATTACK_PLANE), "attackPlane", "Right.Content.Vehicle.AttackPlane");
    public static readonly VehicleType BOMB_PLANE = new(nameof(BOMB_PLANE), "bombPlane", "Right.Content.Vehicle.BombPlane");
    public static readonly VehicleType TURRET_MACHINE_GUN = new(nameof(TURRET_MACHINE_GUN), "turretMachineGun", "Right.Content.Vehicle.TurretMachineGun");
    public static readonly VehicleType TURRET_ANTI_TANK = new(nameof(TURRET_ANTI_TANK), "turretAntiTank", "Right.Content.Vehicle.TurretAntiTank");
    public static readonly VehicleType TURRET_ANTI_AIR = new(nameof(TURRET_ANTI_AIR), "turretAntiAir", "Right.Content.Vehicle.TurretAntiAir");
    
    private readonly string _translatableKey;

    private VehicleType(string name, string value,string translatableKey) : base(name, value) {
        _translatableKey = translatableKey;
    }
    public string GetTranslatableKey() {
        return _translatableKey;
    }

    public string GetSerializedType() {
        return nameof(VehicleType);
    }

    public string GetJsonKey() {
        return Name;
    }
}

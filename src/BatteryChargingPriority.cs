// Copyright (c) 2024 Serhiy Lakas
// https://sabatex.github.io


namespace InverterAnenji;
public enum BatteryChargingPriority{
    UtilityPriority=0,
    PVPriority = 1,
    PVIsAtTheSameLevelAsTheUtility = 2,
    OnlyPVChargingIsAllowed = 3    

}
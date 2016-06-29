//------------------------------------------------------------------------------------------
// loadouts.cs
// Source file declaring usable loadouts for the bots and mapping them to their most
// appropriate tasks.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

$DXAI::Loadouts[0, "Name"] = "Light Scout";
$DXAI::Loadouts[0, "Weapon", 0] = Chaingun;
$DXAI::Loadouts[0, "Weapon", 1] = Disc;
$DXAI::Loadouts[0, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[0, "Pack"] = EnergyPack;
$DXAI::Loadouts[0, "WeaponCount"] = 3;
$DXAI::Loadouts[0, "Armor"] = "Light";

$DXAI::Loadouts[1, "Name"] = "Light Defender";
$DXAI::Loadouts[1, "Weapon", 0] = Blaster;
$DXAI::Loadouts[1, "Weapon", 1] = Disc;
$DXAI::Loadouts[1, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[1, "Pack"] = EnergyPack;
$DXAI::Loadouts[1, "WeaponCount"] = 3;
$DXAI::Loadouts[1, "Armor"] = "Light";

$DXAI::Loadouts[2, "Name"] = "Medium Defender";
$DXAI::Loadouts[2, "Weapon", 0] = ChainGun;
$DXAI::Loadouts[2, "Weapon", 1] = Disc;
$DXAI::Loadouts[2, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[2, "Weapon", 3] = Plasma;
$DXAI::Loadouts[2, "Pack"] = AmmoPack;
$DXAI::Loadouts[2, "WeaponCount"] = 4;
$DXAI::Loadouts[2, "Armor"] = "Medium";

$DXAI::Loadouts[3, "Name"] = "Heavy Defender";
$DXAI::Loadouts[3, "Weapon", 0] = ChainGun;
$DXAI::Loadouts[3, "Weapon", 1] = Disc;
$DXAI::Loadouts[3, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[3, "Weapon", 3] = Mortar;
$DXAI::Loadouts[3, "Weapon", 4] = Plasma;
$DXAI::Loadouts[3, "Pack"] = AmmoPack;
$DXAI::Loadouts[3, "WeaponCount"] = 5;
$DXAI::Loadouts[3, "Armor"] = "Heavy";

$DXAI::Loadouts[4, "Name"] = "Hardened Defender";
$DXAI::Loadouts[4, "Weapon", 0] = ChainGun;
$DXAI::Loadouts[4, "Weapon", 1] = Disc;
$DXAI::Loadouts[4, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[4, "Weapon", 3] = Mortar;
$DXAI::Loadouts[4, "Weapon", 4] = Plasma;
$DXAI::Loadouts[4, "Pack"] = ShieldPack;
$DXAI::Loadouts[4, "WeaponCount"] = 5;
$DXAI::Loadouts[4, "Armor"] = "Heavy";

$DXAI::Loadouts[5, "Name"] = "Cloaked Scout";
$DXAI::Loadouts[5, "Weapon", 0] = Chaingun;
$DXAI::Loadouts[5, "Weapon", 1] = Disc;
$DXAI::Loadouts[5, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[5, "Pack"] = CloakingPack;
$DXAI::Loadouts[5, "WeaponCount"] = 3;
$DXAI::Loadouts[5, "Armor"] = "Light";

$DXAI::OptimalLoadouts["AIEnhancedFlagCaptureTask"] = "0";
$DXAI::OptimalLoadouts["AIEnhancedScoutLocation"] = "0 5";
$DXAI::OptimalLoadouts["AIEnhancedDefendLocation"] = "2 3 4";

// A default loadout to use when the bot has no objective.
$DXAI::DefaultLoadout = 0;

$DXAI::Loadouts::Count = 6;
$DXAI::Loadouts::Default = 0;

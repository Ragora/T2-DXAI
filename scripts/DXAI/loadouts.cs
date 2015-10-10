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
$DXAI::Loadouts[0, "Weapon", 0] = ChainGun;
$DXAI::Loadouts[0, "Weapon", 1] = Disc;
$DXAI::Loadouts[0, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[0, "Pack"] = EnergyPack;
$DXAI::Loadouts[0, "WeaponCount"] = 3;
$DXAI::Loadouts[0, "Armor"] = "Light";

$DXAI::Loadouts[1, "Name"] = "Defender";
$DXAI::Loadouts[1, "Weapon", 0] = ChainGun;
$DXAI::Loadouts[1, "Weapon", 1] = Disc;
$DXAI::Loadouts[1, "Weapon", 2] = GrenadeLauncher;
$DXAI::Loadouts[1, "Weapon", 3] = GrenadeLauncher;
$DXAI::Loadouts[1, "Pack"] = AmmoPack;
$DXAI::Loadouts[1, "WeaponCount"] = 4;
$DXAI::Loadouts[1, "Armor"] = "Medium";

$DXAI::OptimalLoadouts["AIEnhancedDefendLocation"] = "1";

$DXAI::Loadouts::Count = 2;
$DXAI::Loadouts::Default = 0;
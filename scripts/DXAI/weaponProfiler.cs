//------------------------------------------------------------------------------------------
// weaponProfiler.cs
// Source file dedicated to the weapon profiling system.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. 
// Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: The weapon profiler loops over the active game datablocks, trying to 
// run solely on weapons and precompute useful usage information for the artificial
// intelligence to use during weapon selection & firing.
// Param %printResults: A boolean representing whether or not the results should be 
// printed to the console, useful for debugging.
//------------------------------------------------------------------------------------------
function WeaponProfiler::run(%printResults)
{
    if (!isObject(DatablockGroup))
    {
        error("DXAI: Cannot run weapons profiler, no DatablockGroup exists!");
        return;
    }
    
    // Perform an initial scan for player, turret and vehicle datablocks first
    %armorTypes = new SimSet();
    %vehicleTypes = new SimSet();
    %turretTypes = new SimSet();
    for (%iteration = 0; %iteration < DataBlockGroup.getCount(); %iteration++)
    {
        %currentDB = DataBlockGroup.getObject(%iteration);
        
        %classType = %currentDB.getClassName();
        if (%classType $= "PlayerData")
            %armorTypes.add(%currentDB);
        else if (%classType $= "TurretData")
            %turretTypes.add(%currentDB);
        else if (%classType $= "HoverVehicleData" || %classType $= "FlyingVehicleData" || %classType $= "WheeledVehicleData")
            %vehicleTypes.add(%currentDB);
    }
    
    // Now we run the actual calculations
    for (%iteration = 0; %iteration < DataBlockGroup.getCount(); %iteration++)
    {
        %currentItem = DataBlockGroup.getObject(%iteration);
        
        if (%currentItem.getClassName() $= "ItemData")
        {
            %currentImage = %currentItem.image;
            
            if (isObject(%currentImage))
            {
                %currentProjectile = %currentImage.Projectile;
                
                %ammoDB = %currentImage.ammo;
                %usesAmmo = isObject(%ammoDB);
                %usesEnergy = %currentImage.usesEnergy;
                %firingEnergy = %currentImage.minEnergy;
                %spread = %currentImage.projectileSpread;
                
                if (%currentImage.isSeeker)
                {
                    %dryEffectiveRange = %currentImage.seekRadius;
                    %wetEffectiveRange = %currentImage.seekRadius;
                    %dryAccurateRange = %currentImage.seekRadius;
                    %wetAccurateRange = %currentImage.seekRadius;
                }
                else if (isObject(%currentProjectile) && %currentProjectile.getClassName() $= "SniperProjectileData")
                {
                    %dryEffectiveRange = %currentProjectile.maxRifleRange;
                    %wetEffectiveRange = %currentProjectile.maxRifleRange;
                    %dryAccurateRange = %currentProjectile.maxRifleRange;
                    %wetAccurateRange = %currentProjectile.maxRifleRange;
                }
                else
                {
                    %dryEffectiveRange = (%currentProjectile.lifetimeMS / 1000) * %currentProjectile.dryVelocity;
                    %wetEffectiveRange = (%currentProjectile.lifetimeMS / 1000) * %currentProjectile.wetVelocity;
                    %dryAccurateRange = %dryEffectiveRange - (%currentImage.projectileSpread * 8);
                    %wetAccurateRange = %wetEffectiveRange - (%currentImage.projectileSpread * 8);
                }
                
                WeaponProfiler::_processImageStates(%currentItem);
                
                // Perform the assignments and we're done ... probably
                %currentItem.firingEnergy = %firingEnergy;
                %currentItem.dryEffectiveRange = %dryEffectiveRange;
                %currentItem.wetEffectiveRange = %wetEffectiveRange;
                %currentItem.dryAccurateRange = %dryAccurateRange;
                %currentItem.wetAccurateRange = %wetAccurateRange;
                %currentItem.usesAmmo = %usesAmmo;
                %currentItem.usesEnergy = %usesEnergy;
                %currentItem.firingEnergy = %firingEnergy;
                %currentItem.ammoDB = %ammoDB;
                %currentItem.spread = %spread;
                
                WeaponProfiler::_processArmorEffectiveness(%currentItem, %armorTypes);
                WeaponProfiler::_processVehicleEffectiveness(%currentItem, %vehicleTypes);
                WeaponProfiler::_processTurretEffectiveness(%currentItem, %turretTypes);
                
                if (%printResults)
                {
                    error(%currentItem.getName());
                    error("Dry Range: " @ %dryEffectiveRange);
                    error("Wet Range: " @ %wetEffectiveRange);
                    error("Dry Accurate Range: " @ %dryAccurateRange);
                    error("Wet Accurate Range: " @ %wetAccurateRange);
                    error("Fires Wet: " @ %currentItem.firesWet);
                    error("Fires Dry: " @ %currentItem.firesDry);
                    
                    if (!isObject(%currentProjectile))
                    {
                        error("*** COULD NOT FIND PROJECTILE ***");
                        
                        %currentItem.dryEffectiveRange = 300;
                        %currentItem.wetEffectiveRange = 300;
                        %currentItem.dryAccurateRange = 300;
                        %currentItem.wetAccurateRange = 300;
                    }
                    error("--------------------------------------");
                }
            }
        }
    }
    
    %armorTypes.delete();
    %turretTypes.delete();
    %vehicleTypes.delete();
}

function WeaponProfiler::_processArmorEffectiveness(%itemDB, %armorTypes)
{
    %projectileType = %itemDB.image.Projectile;
    
    if (!isObject(%projectileType))
        return;
        
    for (%iteration = 0; %iteration < %armorTypes.getCount(); %iteration++)
    {
        %currentArmor = %armorTypes.getObject(%iteration);
        
        
    }
}

function WeaponProfiler::_processTurretEffectiveness(%itemDB, %turretTypes)
{
    %projectileType = %itemDB.image.Projectile;
    
    if (!isObject(%projectileType))
        return;
}

function WeaponProfiler::_processVehicleEffectiveness(%itemDB, %vehicleTypes)
{
    %projectileType = %itemDB.image.Projectile;
    
    if (!isObject(%projectileType))
        return;
}

function WeaponProfiler::_processImageStates(%itemDB)
{
    // We want to know if this thing fires underwater: We start at the initial state and look for something
    // that prohibits underwater usage.
    %firesWet = true;
    %firesDry = true;

    // First, we map out state names
    %stateCount = -1;
    %targetState = -1;
    while (%currentImage.stateName[%stateCount++] !$= "")
    {
        %stateMapping[%currentImage.stateName[%stateCount]] = %stateCount;

        if (%currentImage.stateFire[%stateCount])
            %targetStateID = %stateCount;
    }

    // Start at the Ready state and go
    %currentState = %stateMapping["Ready"];

    %stateIteration = -1;
    while (%stateIteration++ <= 10)
    {
        if (%currentImage.stateTransitionOnTriggerDown[%currentState] !$= "")
            %currentState = %stateMapping[%currentImage.stateTransitionOnTriggerDown[%currentState]];
        else if (%currentImage.stateTransitionOnWet[%currentState] $= "DryFire")
        {
            %firesWet = false;

            // Check if it fires dry here as well
            %firesDry = %currentImage.stateTransitionOnNotWet[%currentState] !$= "DryFire" ? true : false;
            break;
         }
    }
    
    %itemDB.firesWet = %firesWet;
    %itemDB.firesDry = %firesDry;

    if (%stateIteration == 10)
        error("DXAI: State analysis timed out on " @ %currentItem.getName() @ "!");
}
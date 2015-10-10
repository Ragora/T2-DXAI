//------------------------------------------------------------------------------------------
// aiconnection.cs
// Source file declaring the custom AIConnection methods used by the DXAI experimental
// AI enhancement project. 
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. 
// Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: This initializes some basic values on the given AIConnection object such
// as the fieldOfView and the viewDistance. It isn't supposed to do anything else.
//------------------------------------------------------------------------------------------
function AIConnection::initialize(%this)
{
    %this.fieldOfView = $DXAI::Bot::DefaultFieldOfView;
    %this.viewDistance = $DXAI::Bot::DefaultViewDistance;
}

//------------------------------------------------------------------------------------------
// Description: An update function that is called by the commander code itself once every
// 32 milliseconds. It is what controls the bot's legs (movement) as well as the aiming
// and firing logic.
//------------------------------------------------------------------------------------------
function AIConnection::update(%this)
{
    if (isObject(%this.player) && %this.player.getState() $= "Move")
    {
        %this.updateLegs();
        %this.updateWeapons();
    }
}

//------------------------------------------------------------------------------------------
// Description: Called by the main system when a hostile projectile impacts near the bot.
// This ideally is supposed to trigger some search logic instead of instantly knowing 
// where the shooter is like the original AI did.
//
// NOTE: This is automatically called by the main system and therefore should not be called
// directly.
//------------------------------------------------------------------------------------------
function AIConnection::notifyProjectileImpact(%this, %data, %proj, %position)
{
    if (!isObject(%proj.sourceObject) || %proj.sourceObject.client.team == %this.team)
        return;
}

//------------------------------------------------------------------------------------------
// Description: Returns whether or not the given AIConnection is considered by be 'idle'.
// This is determined by checking whether or not the AIConnection is in their associated
// commander's idle bot list. If the AIConnection has no commander, then true is always
// returned.
//------------------------------------------------------------------------------------------
function AIConnection::isIdle(%this)
{
    if (!isObject(%this.commander))
        return true;
    
    return %this.commander.idleBotList.isMember(%this);
}

//------------------------------------------------------------------------------------------
// Description: Basically resets the entire state of the given AIConnection. It does not
// unassign tasks, but it does reset the bot's current movement state.
//------------------------------------------------------------------------------------------
function AIConnection::reset(%this)
{
  //  AIUnassignClient(%this);
    
    %this.stop();
   // %this.clearTasks();
    %this.clearStep();
    %this.lastDamageClient = -1;
    %this.lastDamageTurret = -1;
    %this.shouldEngage = -1;
    %this.setEngageTarget(-1);
    %this.setTargetObject(-1);
    %this.pilotVehicle = false;
    %this.defaultTasksAdded = false;
    
    if (isObject(%this.controlByHuman))
        aiReleaseHumanControl(%this.controlByHuman, %this);
}

//------------------------------------------------------------------------------------------
// Description: Tells the AIConnection to move to a given position. They will automatically
// plot a path and attempt to navigate there. 
// Param %position: The target location to move to. If this is simply -1, then all current
// moves will be cancelled.
//
// NOTE: This should only be called by the bot's current active task. If this is called
// outside of the AI task system, then the move order is very liable to be overwritten by
// the current running task in it's next monitor call.
//------------------------------------------------------------------------------------------
function AIConnection::setMoveTarget(%this, %position)
{
    if (%position == -1)
    {
        %this.reset();
        %this.isMovingToTarget = false;
        %this.isFollowingTarget = false;
        return;
    }
    
    %this.moveTarget = %position;
    %this.isMovingToTarget = true;
    %this.isFollowingTarget = false;
    %this.setPath(%position);
    %this.stepMove(%position);
    
    %this.minimumPathDistance = 9999;
    %this.maximumPathDistance = -9999;
}

//------------------------------------------------------------------------------------------
// Description: Tells the AIConnection to follow a given target object.
// Param %target: The ID of the target object to be following. If the target does not exist,
// nothing happens. If the target is -1, then all current moves will be cancelled.
// Param %minDistance: The minimum following distance that the bot should enforce.
// Param %maxDistance: The maximum following dinstance that the bot should enforce.
// Param %hostile: A boolean representing whether or not the bot should perform evasion
// while maintaining a follow distance between %minDistance and %maxDistance.
//
// NOTE: This should only be called by the bot's current active task. If this is called
// outside of the AI task system, then the move order is very liable to be overwritten by
// the current running task in it's next monitor call.
// TODO: Implement custom follow logic to respect %minDistance, %maxDistance and %hostile.
// Perhaps a specific combination of these values will trigger the default escort logic:
// A min distance of 10 or less, a max distance of 20 or less and not hostile?
//------------------------------------------------------------------------------------------
function AIConnection::setFollowTarget(%this, %target, %minDistance, %maxDistance, %hostile)
{
    if (%target == -1)
    {
        %this.reset();
        %this.isMovingToTarget = false;
        %this.isFollowingTarget = false;
    }
    
    if (!isObject(%target))
        return;
        
    %this.followTarget = %target;
    %this.isFollowingTarget = true;
    %this.followMinDistance = %minDistance;
    %this.followMaxDistance = %maxDistance;
    %this.followHostile = %hostile;
    
    %this.stepEscort(%target);
}

//------------------------------------------------------------------------------------------
// Description: A function that is used to determine whether or not the given AIConnection
// appears to be stuck somewhere. Currently, it works by tracking how far along the current
// path a given bot is once every 5 seconds. If there appears to have been no good progress
// between calls, then the bot is marked as stuck.
//
// NOTE: This is called automatically on its own scheduled tick and shouldn't be called
// directly.
//------------------------------------------------------------------------------------------
function AIConnection::stuckCheck(%this)
{
    if (isEventPending(%this.stuckCheckTick))
        cancel(%this.stuckCheckTick);
    
    %targetDistance = %this.pathDistRemaining(9000);
    if (!%this.isMovingToTarget || !isObject(%this.player) || %this.player.getState() !$= "Move" || %targetDistance <= 5)
    {
        %this.stuckCheckTick = %this.schedule(5000, "stuckCheck");
        return;
    }
        
    if (!%this.isPathCorrecting && %targetDistance >= %this.minimumPathDistance && %this.minimumPathDistance != 9999)
        %this.isPathCorrecting = true;           
   
    if (%targetDistance > %this.maximumPathDistance)
        %this.maximumPathDistance = %targetDistance;
    if (%targetDistance < %this.minimumPathDistance)
        %this.minimumPathDistance = %targetDistance;
    
    %this.stuckCheckTick = %this.schedule(5000, "stuckCheck");
}

//------------------------------------------------------------------------------------------
// Description: A function called by the ::update function of the AIConnection that is
// called once every 32ms by the commander AI logic to update the bot's current move
// logic.
//
// NOTE: This is automatically called by the commander AI and therefore should not be
// called directly.
//------------------------------------------------------------------------------------------
function AIConnection::updateLegs(%this)
{
    %now = getSimTime();
    %delta = %now - %this.lastUpdateLegs;
    %this.lastUpdateLegs = %now;
    
    if (%this.isMovingToTarget)
    {
        if (%this.aimAtLocation)
            %this.aimAt(%this.moveTarget);
        else if(%this.manualAim)
            %this.aimAt(%this.moveTarget);
    }
    else if (%this.isFollowingTarget)
    {
        
    }
    else
    {
        %this.stop();
        %this.clearStep();
    }
}

//------------------------------------------------------------------------------------------
// Description: A function called by the ::update function of the AIConnection that is
// called once every 32ms by the commander AI logic to update the bot's current aiming &
// engagement logic.
//
// NOTE: This is automatically called by the commander AI and therefore should not be
// called directly.
//------------------------------------------------------------------------------------------
function AIConnection::updateWeapons(%this)
{
    if (isObject(%this.engageTarget))
    {
        %player = %this.player;
        %targetDistance = vectorDist(%player.getPosition(), %this.engageTarget.getPosition());
            
        // Firstly, just aim at them for now
        %this.aimAt(%this.engageTarget.getWorldBoxCenter());
            
        // What is our current best weapon? Right now we just check target distance and weapon spread.
        %bestWeapon = 0;
            
        for (%iteration = 0; %iteration < %player.weaponSlotCount; %iteration++)
        {
        // Weapons with a decent bit of spread should be used <= 20m
        }
            
        %player.selectWeaponSlot(%bestWeapon);
    }
}

//------------------------------------------------------------------------------------------
// Description: A function called randomly on time periods between 
// $DXAI::Bot::MinimumVisualAcuityTime and $DXAI::Bot::MaximumVisualAcuityTime which
// attempts to simulate Human eyesight using a complex view cone algorithm implemented
// entirely in Torque Script.
// Param %bot.enableVisualDebug: A boolean assigned to an individual bot that is used to
// enable or disable the visual debug feature. This feature, when enabled, will draw the
// bot's view cone using waypoints placed at the individual points of the view cone and is
// updated once per tick of this function.
//
// NOTE: This is called automatically using its own scheduled ticks and therefore should
// not be called directly.
//------------------------------------------------------------------------------------------
function AIConnection::updateVisualAcuity(%this)
{
    if (isEventPending(%this.visualAcuityTick))
        cancel(%this.visualAcuityTick);
    
    // If we can't even see or if we're downright dead, don't do anything.
    if (%this.visibleDistance = 0 || !isObject(%this.player) || %this.player.getState() !$= "Move")
    {
        %this.visualAcuityTick = %this.schedule(getRandom($DXAI::Bot::MinimumVisualAcuityTime, $DXAI::Bot::MaximumVisualAcuityTime,), "updateVisualAcuity");
        return;
    }
    
    // The visual debug feature is a system in which we can use waypoints to view the bot's calculated viewcone per tick.
    if (%this.enableVisualDebug)
    {
        if (!isObject(%this.originMarker))
        {
            %this.originMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %this.team; name = %this.namebase SPC " Origin"; };
            %this.clockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %this.team; name = %this.namebase SPC " Clockwise"; };
            %this.counterClockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %this.team; name = %this.namebase SPC " Counter Clockwise"; };
            %this.upperMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %this.team; name = %this.namebase SPC " Upper"; };
            %this.lowerMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %this.team; name = %this.namebase SPC " Lower"; };
        }
            
        %viewCone = %this.calculateViewCone();
        %coneOrigin = getWords(%viewCone, 0, 2);
        %viewConeClockwiseVector = getWords(%viewCone, 3, 5);
        %viewConeCounterClockwiseVector = getWords(%viewCone, 6, 8);
        
        %viewConeUpperVector = getWords(%viewCone, 9, 11);
        %viewConeLowerVector = getWords(%viewCone, 12, 14);
        
        // Update all the markers
        %this.clockwiseMarker.setPosition(%viewConeClockwiseVector);
        %this.counterClockwiseMarker.setPosition(%viewConeCounterClockwiseVector);
        %this.upperMarker.setPosition(%viewConeUpperVector);
        %this.lowerMarker.setPosition(%viewConeLowerVector);
        %this.originMarker.setPosition(%coneOrigin);
    }
    else if (isObject(%this.originMarker))
    {
        %this.originMarker.delete();
        %this.clockwiseMarker.delete();
        %this.counterClockwiseMarker.delete();
        %this.upperMarker.delete();
        %this.lowerMarker.delete();
    }
   
    %now = getSimTime();
    %deltaTime = %now - %this.lastVisualAcuityUpdate;
    %this.lastVisualAcuityUpdate = %now;
    
    %visibleObjects = %this.getObjectsInViewcone($TypeMasks::ProjectileObjectType | $TypeMasks::PlayerObjectType, %this.viewDistance, true);

    for (%iteration = 0; %iteration < %visibleObjects.getCount(); %iteration++)
    {
        %current = %visibleObjects.getObject(%iteration);
            
        %this.awarenessTime[%current] += %deltaTime;
        
        // Did we "notice" the object yet?
        %noticeTime = getRandom(700, 1200);
        if (%this.awarenessTime[%current] < %noticeTime)
            continue;
        
        if (%current.getType() & $TypeMasks::ProjectileObjectType)
        {               
            %className = %current.getClassName();
            
            // LinearFlareProjectile and LinearProjectile have linear trajectories, so we can easily determine if a dodge is necessary
            if (%className $= "LinearFlareProjectile" || %className $= "LinearProjectile")
            {
                //%this.setDangerLocation(%current.getPosition(), 20);
                
                // Perform a raycast to determine a hitpoint
                %currentPosition = %current.getPosition();
                %rayCast = containerRayCast(%currentPosition, vectorAdd(%currentPosition, vectorScale(%current.initialDirection, 200)), -1, 0);     
                %hitObject = getWord(%raycast, 0);
                                
                // We're set for a direct hit on us!
                if (%hitObject == %this.player)
                {
                    %this.setDangerLocation(%current.getPosition(), 30);
                    continue;
                }
                
                // If there is no radius damage, don't worry about it now
                if (!%current.getDatablock().hasDamageRadius)
                    continue;
                
                // How close is the hit loc?
                %hitLocation = getWords(%rayCast, 1, 3);
                %hitDistance = vectorDist(%this.player.getPosition(), %hitLocation);
                
                // Is it within the radius damage of this thing?
                if (%hitDistance <= %current.getDatablock().damageRadius)
                    %this.setDangerLocation(%current.getPosition(), 30);
            }
            // A little bit harder to detect.
            else if (%className $= "GrenadeProjectile")
            {
                
            }
        }
        // See a player?
        else if (%current.getType() & $TypeMasks::PlayerObjectType && %current.client.team != %this.team)
        {
            %this.visibleHostiles.add(%current);
            //%this.clientDetected(%current);
           // %this.clientDetected(%current.client);
            
            // ... if the moron is right there in our LOS then we probably should see them
           // %start = %this.player.getPosition();
           // %end = vectorAdd(%start, vectorScale(%this.player.getEyeVector(), %this.viewDistance));
            
           // %rayCast = containerRayCast(%start, %end, -1, %this.player);     
           // %hitObject = getWord(%raycast, 0);

           // if (%hitObject == %current)
           // {
               // %this.clientDetected(%current);
            //    %this.stepEngage(%current);
           // }
        }
    }
    
    // Now we run some logic on some things that we no longer can see.
    for (%iteration = 0; %iteration < %this.visibleHostiles.getCount(); %iteration++)
    {
        %current = %this.visibleHostiles.getObject(%iteration);
        
        if (%this.visibleHostiles.isMember(%current) && !%visibleObjects.isMember(%current))
        {
            %this.awarenessTime[%current] -= %deltaTime;
            if (%this.awarenessTime[%current] < 200)
            {
                %this.visibleHostiles.remove(%current);
                continue;
            }
        }
    }
        
    %visibleObjects.delete();
    %this.visualAcuityTick = %this.schedule(getRandom($DXAI::Bot::MinimumVisualAcuityTime, $DXAI::Bot::MaximumVisualAcuityTime), "updateVisualAcuity");
}
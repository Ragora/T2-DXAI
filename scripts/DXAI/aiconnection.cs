//------------------------------------------------------------------------------------------
// aiconnection.cs
// Source file declaring the custom AIConnection methods used by the DXAI experimental
// AI enhancement project.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. 
// Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

function AIConnection::initialize(%this)
{
    %this.fieldOfView = 3.14 / 2; // 90* View cone
    %this.viewDistance = 300;
}

function AIConnection::update(%this)
{
    if (isObject(%this.player) && %this.player.getState() $= "Move")
    {
        %this.updateLegs();
        %this.updateWeapons();
    }
}

function AIConnection::notifyProjectileImpact(%this, %data, %proj, %position)
{
    if (!isObject(%proj.sourceObject) || %proj.sourceObject.client.team == %this.team)
        return;
}

function AIConnection::isIdle(%this)
{
    if (!isObject(%this.commander))
        return true;
    
    return %this.commander.idleBotList.isMember(%this);
}

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

function AIConnection::setFollowTarget(%this, %target, %minDistance, %maxDistance, %hostile)
{
    if (!isObject(%target))
    {
        %this.reset();
        %this.isMovingToTarget = false;
        %this.isFollowingTarget = false;
        return;
    }
        
    %this.followTarget = %target;
    %this.isFollowingTarget = true;
    %this.followMinDistance = %minDistance;
    %this.followMaxDistance = %maxDistance;
    %this.followHostile = %hostile;
    %this.stepEscort(%target);
}

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
            
        %targetDistance = %this.pathDistRemaining(9000);
        
        if (%targetDistance > %this.maximumPathDistance)
            %this.maximumPathDistance = %targetDistance;
         if (%targetDistance < %this.minimumPathDistance)
            %this.minimumPathDistance = %targetDistance;
    
        // Bots follow a set of lines drawn between nodes to slowly decrement the path distance,
        // so bots that are stuck usually get their remaining distance stuck in some range of
        // arbitrary values, so we monitor the minimum and maximum values over a period of 5 seconds
            
        // Test...
        %pathDistance = %this.getPathDistance(%this.moveTarget);
        if(%pathDistance > 10 && %this.moveTravelTime < 10000)
            %this.moveTravelTime += %delta;
        else if (%pathDistance < 10)
            %this.moveTravelTime = 0;
        else if (%this.moveTravelTime >= 10000)
        {
            // We appear to be stuck, so pick a random nearby node and try to run to it
            %this.moveTravelTime = 0;
            %this.isPathCorrecting = true;
            
            if (isObject(NavGraph))
            {
                %randomNode = NavGraph.randNode(%this.player.getPosition(), 200, true, true);
                if (%randomNode != -1)
                    %this.setMoveTarget(NavGraph.nodeLoc(%randomNode));
            }
        }
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

function AIConnection::updateVisualAcuity(%this)
{
    if (isEventPending(%this.visualAcuityTick))
        cancel(%this.visualAcuityTick);
        
    if (%this.visibleDistance = 0 || !isObject(%this.player) || %this.player.getState() !$= "Move")
    {
        %this.visualAcuityTick = %this.schedule(getRandom(230, 400), "updateVisualAcuity");
        return;
    }
    
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
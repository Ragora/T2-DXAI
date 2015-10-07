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

function AIConnection::initialize(%this, %aiClient)
{
    %this.fieldOfView = 3.14 / 2; // 90* View cone
    %this.viewDistance = 300;
    
    if (!isObject(%aiClient))
        error("AIPlayer: Attempted to initialize with bad AI client connection!");
        
    %this.client = %aiClient;
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

function AIConnection::updateLegs(%this)
{
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

function AIConnection::updateWeapons(%this)
{
  
}

function AIConnection::updateVisualAcuity(%this)
{
    if (isEventPending(%this.visualAcuityTick))
        cancel(%this.visualAcuityTick);
        
    if (!isObject(%this.player) || %this.player.getState() !$= "Move")
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
    
    %result = %this.getObjectsInViewcone($TypeMasks::ProjectileObjectType | $TypeMasks::PlayerObjectType, %this.viewDistance, true);
    
    // What can we see?
    for (%i = 0; %i < %result.getCount(); %i++)
    {
        %current = %result.getObject(%i);
        %this.awarenessTicks[%current]++;
        
        if (%current.getType() & $TypeMasks::ProjectileObjectType)
        {   
            // Did we "notice" the object yet?
            // We pick a random notice time between 700ms and 1200 ms
            // Obviously this timer runs on a 32ms tick, but it should help provide a little unpredictability
            %noticeTime = getRandom(700, 1200);
            if (%this.awarenessTicks[%current] < (%noticeTime / 32))
                continue;
            
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
        else if (%current.getType() & $TypeMasks::PlayerObjectType)
        {
            %this.clientDetected(%current);
            %this.clientDetected(%current.client);
            
            // ... if the moron is right there in our LOS then we probably should see them
            %start = %this.player.getPosition();
            %end = vectorAdd(%start, vectorScale(%this.player.getEyeVector(), %this.viewDistance));
            
            %rayCast = containerRayCast(%start, %end, -1, %this.player);     
            %hitObject = getWord(%raycast, 0);

            if (%hitObject == %current)
            {
                %this.clientDetected(%current);
                %this.stepEngage(%current);
            }
        }
    }
    
    %result.delete();
    %this.visualAcuityTick = %this.schedule(getRandom(230, 400), "updateVisualAcuity");
}
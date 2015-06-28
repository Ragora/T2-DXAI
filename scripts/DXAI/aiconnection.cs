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
        %this.updateVisualAcuity();
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

function AIConnection::updateLegs(%this)
{
    if (%this.isMoving && %this.getTaskID() != 0)
    {
        %this.setPath(%this.moveLocation);
        %this.stepMove(%this.moveLocation);
        
        if (%this.aimAtLocation)
            %this.aimAt(%this.moveLocation);
        else if(%this.manualAim)
            %this.aimAt(%this.aimLocation);
    }
    else
    {
        %this.stop();
        %this.clearStep();
    }
}

function AIConnection::updateVisualAcuity(%this)
{
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
            
            // LinearFlareProjectile and LinearProjectile have linear properties, so we can easily determine if a dodge is necessary
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
}
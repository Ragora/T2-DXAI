// DXAI_Objectives.cs
// Objectives for the AI system
// Copyright (c) 2014 Robert MacGregor

//----------------------------------------------------------------------
// The AIVisualAcuity task is a complementary task for the AI grunt systems
// to perform better at recognizing things visually with reasonably
// Human perception capabilities.
// ---------------------------------------------------------------------

function AIVisualAcuity::initFromObjective(%task, %objective, %client)
{
    // Called to initialize from an objective object
}

function AIVisualAcuity::assume(%task, %client)
{
    // Called when the bot starts the task
    %task.setMonitorFreq(32);
}

function AIVisualAcuity::retire(%task, %client)
{
    // Called when the bot stops the task
}

function AIVisualAcuity::weight(%task, %client)
{
    %task.setWeight(999);
}

function AIVisualAcuity::monitor(%task, %client)
{
 // Called when the bot is performing the task
    if (%client.enableVisualDebug)
    {
        if (!isObject(%client.originMarker))
        {
            %client.originMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Origin"; };
            %client.clockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Clockwise"; };
            %client.counterClockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Counter Clockwise"; };
            %client.upperMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Upper"; };
            %client.lowerMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Lower"; };
        }
            
        %viewCone = %client.calculateViewCone();
        %coneOrigin = getWords(%viewCone, 0, 2);
        %viewConeClockwiseVector = getWords(%viewCone, 3, 5);
        %viewConeCounterClockwiseVector = getWords(%viewCone, 6, 8);
        
        %viewConeUpperVector = getWords(%viewCone, 9, 11);
        %viewConeLowerVector = getWords(%viewCone, 12, 14);
        
        // Update all the markers
        %client.clockwiseMarker.setPosition(%viewConeClockwiseVector);
        %client.counterClockwiseMarker.setPosition(%viewConeCounterClockwiseVector);
        %client.upperMarker.setPosition(%viewConeUpperVector);
        %client.lowerMarker.setPosition(%viewConeLowerVector);
        %client.originMarker.setPosition(%coneOrigin);
    }
    else if (isObject(%client.originMarker))
    {
        %client.originMarker.delete();
        %client.clockwiseMarker.delete();
        %client.counterClockwiseMarker.delete();
        %client.upperMarker.delete();
        %client.lowerMarker.delete();
    }
    
    %result = %client.getObjectsInViewcone($TypeMasks::ProjectileObjectType | $TypeMasks::PlayerObjectType, %client.viewDistance, true);
    
    // What can we see?
    for (%i = 0; %i < %result.getCount(); %i++)
    {
        %current = %result.getObject(%i);
        %client.awarenessTicks[%current]++;
        
        if (%current.getType() & $TypeMasks::ProjectileObjectType)
        {   
            // Did we "notice" the object yet?
            // We pick a random notice time between 700ms and 1200 ms
            // Obviously this timer runs on a 32ms tick, but it should help provide a little unpredictability
            %noticeTime = getRandom(700, 1200);
            if (%client.awarenessTicks[%current] < (%noticeTime / 32))
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
                if (%hitObject == %client.player)
                {
                    %client.setDangerLocation(%current.getPosition(), 30);
                    continue;
                }
                
                // If there is no radius damage, don't worry about it now
                if (!%current.getDatablock().hasDamageRadius)
                    continue;
                
                // How close is the hit loc?
                %hitLocation = getWords(%rayCast, 1, 3);
                %hitDistance = vectorDist(%client.player.getPosition(), %hitLocation);
                
                // Is it within the radius damage of this thing?
                if (%hitDistance <= %current.getDatablock().damageRadius)
                    %client.setDangerLocation(%current.getPosition(), 30);
            }
            // A little bit harder to detect.
            else if (%className $= "GrenadeProjectile")
            {
                
            }
        }
        // See a player?
        else if (%current.getType() & $TypeMasks::PlayerObjectType)
        {
            // ... if the moron is right there in our LOS then we probably should see them
            %start = %client.player.getPosition();
            %end = vectorAdd(%start, vectorScale(%client.player.getEyeVector(), %client.viewDistance));
            
            %rayCast = containerRayCast(%start, %end, -1, %client.player);     
            %hitObject = getWord(%raycast, 0);
            
            echo(%hitObject);
            echo(%current);
            if (%hitObject == %current)
                %client.stepEngage(%current);
        }
    }
    
    %result.delete();
}

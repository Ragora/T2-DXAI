// DXAI_Objectives.cs
// Objectives for the AI system
// Copyright (c) 2014 Robert MacGregor

//----------------------------------------------------------------------
// The AIVisualAcuity task is a complementary task for the AI grunt systems
// to perform better at recognizing things visually with reasonably
// Human perception capabilities.
// ---------------------------------------------------------------------

function AIConnection::updateLegs(%this)
{
    if (%this.isMoving && %this.getTaskID() != 0)
    {
        %this.setPath(%this.moveLocation);
        %this.stepMove(%this.moveLocation);
        
        if (%this.aimAtLocation)
            %this.aimAt(%this.moveLocation);
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
            
          //  echo(%hitObject);
           // echo(%current);
            if (%hitObject == %current)
            {
                %this.clientDetected(%current);
                %this.stepEngage(%current);
            }
        }
    }
    
    %result.delete();
}

function AIConnection::enhancedLogicLoop(%this)
{
    cancel(%this.enhancedLogicHandle);
    
    if (isObject(%this.player))
    {
        %this.updateVisualAcuity();
        %this.updateLegs();
    }
    
    %this.enhancedLogicHandle = %this.schedule(32, "enhancedLogicLoop");
}

//-------------------------------------------------------------
function AIEnhancedDefendLocation::initFromObjective(%task, %objective, %client)
{
    // Called to initialize from an objective object
}

function AIEnhancedDefendLocation::assume(%task, %client)
{
    // Called when the bot starts the task
    %task.setMonitorFreq(1);
}

function AIEnhancedDefendLocation::retire(%task, %client)
{
    // Called when the bot stops the task
}

function AIEnhancedDefendLocation::weight(%task, %client)
{
    %task.setWeight(1000);
}

function AIEnhancedDefendLocation::monitor(%task, %client)
{   
   // echo(%task.getMonitorFreq());
    if (%client.getPathDistance(%client.defendLocation) <= 40)
    {
        // Pick a random time to move to a nearby location
        if (%client.defendTime == -1)
        {
            %client.nextDefendRotation = getRandom(5000, 10000);
            %client.isMoving = false;
        }
        
        // If we're near our random point, just don't move
        if (%client.getPathDistance(%client.moveLocation) <= 10)
            %client.isMoving = false;
            
        %client.defendTime += 32;
        if (%client.defendTime >= %client.nextDefendRotation)
        {
            %client.defendTime = 0;
            %client.nextDefendRotation = getRandom(5000, 10000);
            
            // TODO: Replace with something that detects interiors as well
            %randomPosition = getRandomPosition(%client.defendLocation, 40);
            %randomPosition = getWords(%randomPosition, 0, 1) SPC getTerrainHeight(%randomPosition);
                        
            %client.moveLocation = %randomPosition;
            %client.isMoving = true;
        }
    }
    else
    {
        %client.defendTime = -1;
        %client.moveLocation = %client.defendLocation;
        %client.isMoving = true;
    }
}

//-------------------------------------------------------------
function AIEnhancedScoutLocation::initFromObjective(%task, %objective, %client)
{
    // Called to initialize from an objective object
}

function AIEnhancedScoutLocation::assume(%task, %client)
{
    // Called when the bot starts the task
    %task.setMonitorFreq(1);
    
    %client.currentNode = -1;
}

function AIEnhancedScoutLocation::retire(%task, %client)
{
    // Called when the bot stops the task
}

function AIEnhancedScoutLocation::weight(%task, %client)
{
    %task.setWeight(1000);
}

function AIEnhancedScoutLocation::monitor(%task, %client)
{   
    // We can't really work without a NavGraph
    if (!isObject(NavGraph))
        return;
    
    // We just received the task, so find a node within distance of our scout location
    if (%client.currentNode == -1)
    {
        %client.currentNode = NavGraph.randNode(%client.scoutLocation, %client.scoutDistance, true, true);
        
        if (%client.currentNode != -1)
        {
            %client.moveLocation = NavGraph.nodeLoc(%client.currentNode);
            %client.isMoving = true;
        }
    }
    // We're moving, or are near enough to our target
    else
    {
        // Don't move if we're close enough to our next node
        if (%client.getPathDistance(%client.moveLocation) <= 40)
        {
            %client.isMoving = false;
            %client.nextScoutRotation = getRandom(5000, 10000);
            %client.scoutTime += 32;
        }
        else
        {
            %client.isMoving = true;
            %client.scoutTime += 0;
        }
        
        // Wait a little bit at each node
        if (%client.scoutTime >= %client.nextScoutRotation)
        {
            %client.scoutTime = 0;
            %client.nextScoutRotation = getRandom(5000, 10000);

            // Pick a new node
            %client.currentNode = NavGraph.randNode(%client.scoutLocation, %client.scoutDistance, true, true);
            
            // Ensure that we found a node.
            if (%client.currentNode != -1)
            {
                %client.moveLocation = NavGraph.nodeLoc(%client.currentNode);
                %client.isMoving = true;
            }
        }
    }
    
}

//-------------------------------------------------------------
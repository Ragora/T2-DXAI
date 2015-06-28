//------------------------------------------------------------------------------------------
// main.cs
// Source file for the DXAI enhanced objective implementations. 
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
//      +Param %bot.escortTarget: The ID of the object to escort. This can be literally
// any object that exists in the game world.
//      +Description: The AIEnhancedDefendLocation does exactly as the name implies. The
// behavior a bot will exhibit with this code is that the bot will attempt to first to
// the location desiginated by %bot.defendLocation. Once the bot is in range, it will 
// idly step about near the defense location, performing a sort of short range scouting.
// If the bot were to be knocked too far away, then this logic will simply start all over
// again.
//------------------------------------------------------------------------------------------
function AIEnhancedEscort::initFromObjective(%task, %objective, %client) { }
function AIEnhancedEscort::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedEscort::retire(%task, %client) { %client.isMoving = false; %client.manualAim = false; }
function AIEnhancedEscort::weight(%task, %client) { %task.setWeight(1000); }

function AIEnhancedEscort::monitor(%task, %client)
{   
    // Is our escort object even a thing?
    if (!isObject(%client.escortTarget))
        return;
    
    %escortLocation = %client.escortTarget.getPosition();
    
    // Pick a location near the target
    // FIXME: Only update randomly every so often, or perhaps update using the target's move direction & velocity?
    // TODO: Keep a minimum distance from the escort target, prevents crowding and accidental vehicular death.
    %client.isMoving = true;
    %client.manualAim = true;
    %client.aimLocation = %escortLocation;
    
    %client.moveLocation = getRandomPositionOnTerrain(%escortLocation, 40);
}

//------------------------------------------------------------------------------------------
//      +Param %bot.defendLocation: The X Y Z coordinates of the location that this bot
// must attempt to defend.
//      +Description: The AIEnhancedDefendLocation does exactly as the name implies. The
// behavior a bot will exhibit with this code is that the bot will attempt to first to
// the location desiginated by %bot.defendLocation. Once the bot is in range, it will 
// idly step about near the defense location, performing a sort of short range scouting.
// If the bot were to be knocked too far away, then this logic will simply start all over
// again.
//------------------------------------------------------------------------------------------
function AIEnhancedDefendLocation::initFromObjective(%task, %objective, %client) { }
function AIEnhancedDefendLocation::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedDefendLocation::retire(%task, %client) { %client.isMoving = false; }
function AIEnhancedDefendLocation::weight(%task, %client) { %task.setWeight(1000); }

function AIEnhancedDefendLocation::monitor(%task, %client)
{   
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
            %client.moveLocation = getRandomPositionOnTerrain(%client.defendLocation, 40);
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

//------------------------------------------------------------------------------------------
//      +Param %bot.scoutLocation: The X Y Z coordinates of the location that this bot
// must attempt to scout.
//      +Param %bot.scoutDistance: The maximum distance that this bot will attempt to scout
// out around %bot.scoutLocation.
//      +Description: The AIEnhancedScoutLocation does exactly as the name implies. The
// behavior a bot will exhibit with this code is that the bot will pick random nodes from
// the navigation graph that is within %bot.scoutDistance of %bot.scoutLocation and head
// to that chosen node. This produces a bot that will wander around the given location,
// including into and through interiors & other noded obstructions.
//------------------------------------------------------------------------------------------
function AIEnhancedScoutLocation::initFromObjective(%task, %objective, %client) { }
function AIEnhancedScoutLocation::assume(%task, %client) { %task.setMonitorFreq(1); %client.currentNode = -1; }
function AIEnhancedScoutLocation::retire(%task, %client) { }
function AIEnhancedScoutLocation::weight(%task, %client) { %task.setWeight(1000); }

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
        %pathDistance = %client.getPathDistance(%client.moveLocation);
        // Don't move if we're close enough to our next node
        if (%pathDistance <= 40 && %client.isMoving)
        {
            %client.isMoving = false;
            %client.nextScoutRotation = getRandom(5000, 10000);
            %client.scoutTime += 32;
        }
        else if(%client.getPathDistance(%client.moveLocation) > 40)
        {
            %client.isMoving = true;
            %client.scoutTime = 0;
        }
        else
            %client.scoutTime += 32;
        
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
//------------------------------------------------------------------------------------------
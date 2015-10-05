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
function AIEnhancedEscort::weight(%task, %client) { %task.setWeight(500); }

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
function AIEnhancedDefendLocation::weight(%task, %client) { %task.setWeight(500); }

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
function AIEnhancedScoutLocation::weight(%task, %client) { %task.setWeight(500); }

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

//------------------------------------------------------------------------------------------
//      +Param %bot.engangeDistance: The maximum distance at which the bot will go out to
// attack a hostile.
//      +Param %bot.engageTarget: A manually assigned engage target to go after.
//      +Description: The AIEnhancedEngageTarget is a better implementation of the base
// AI engage logic.
//------------------------------------------------------------------------------------------`
function AIEnhancedEngageTarget::initFromObjective(%task, %objective, %client) { }
function AIEnhancedEngageTarget::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedEngageTarget::retire(%task, %client) { }

function AIEnhancedEngageTarget::weight(%task, %client) 
{
  if (!isObject(%client.engageTarget))
  {
    %visibleObjects = %client.getObjectsInViewcone($TypeMasks::PlayerObjectType, %client.viewDistance, true);
    
    // Choose the closest target
    // TODO: Choose on more advanced metrics like HP
    %chosenTarget = -1;
    %chosenTargetDistance = 9999;
    for (%iteration = 0; %iteration < %visibleObjects.getCount(); %iteration++)
    {
      %potentialTarget = %visibleObjects.getObject(%iteration);
      
      %potentialTargetDistance = vectorDist(%potentialTarget.getPosition(), %client.player.getPosition());
      if (%potentialTarget.client.team != %client.team && %potentialTargetDistance < %chosenTargetDistance)
      {
        %chosenTargetDistance = %potentialTargetDistance;
        %chosenTarget = %potentialTarget;       
      }
    }
    
    %visibleObjects.delete();
    %client.engageTarget = %chosenTarget;
  }
  else
  {
    // Can we still see them?
    %rayCast = containerRayCast(%client.player.getWorldBoxCenter(), %client.engageTarget.getWorldBoxCenter(), -1, %client.player);           
    %hitObject = getWord(%raycast, 0);

    // TODO: Go to the last known position.
    if (%hitObject != %client.engageTarget)
      %client.engageTarget = -1;
  }

  if (!isObject(%client.engageTarget) && %client.engageTargetLastPosition $= "")
    %task.setWeight(0);
  else
    %task.setWeight(1000);
}

function AIEnhancedEngageTarget::monitor(%task, %client)
{       
  if (isObject(%client.engageTarget))
  {    
    %player = %client.player;
    %targetDistance = vectorDist(%player.getPosition(), %client.engageTarget.getPosition());
    
    // Firstly, just aim at them for now
    %client.aimAt(%client.engageTarget.getWorldBoxCenter());
    
    // What is our current best weapon? Right now we just check target distance and weapon spread.
    %bestWeapon = 0;
    
    for (%iteration = 0; %iteration < %player.weaponSlotCount; %iteration++)
    {
      // Weapons with a decent bit of spread should be used <= 20m
    }
    
    %player.selectWeaponSlot(%bestWeapon);
    %client.engageTargetLastPosition = %client.engageTarget.getWorldBoxCenter();
    
    %client.isMoving = true;
    %client.moveLocation = getRandomPositionOnTerrain(%client.engageTargetLastPosition, 40);
    
    %client.pressFire();
  }
  else if (%client.engageTargetLastPosition !$= "")
  {
    %client.isMoving = true;
    %client.moveLocation = %client.engageTargetLastPosition;

    if (vectorDist(%client.player.getPosition(), %client.engageTargetLastPosition) <= 10)
    {
      %client.engageTargetLastPosition = "";
      %client.isMoving = false; 
    }
  }
}
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
//      +Param %bot.shouldRearm: A boolean representing whether or not this bot should go
// and rearm.
//      +Param %bot.targetInventory: The ID of the inventory station to rearm at.
//------------------------------------------------------------------------------------------`
function AIEnhancedRearmTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedRearmTask::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedRearmTask::retire(%task, %client) { }

function AIEnhancedRearmTask::weight(%task, %client) 
{
  if (%client.shouldRearm)
    %task.setWeight(600);
  else
    %task.setWeight(0);
}

function AIEnhancedRearmTask::monitor(%task, %client)
{       
  if (!isObject(%client.targetInventory))
    %client.targetInventory = %client.getClosestInventory();
  
  if (isObject(%client.targetInventory))
  {
    // Politely wait if someone is already on it.
    if (vectorDist(%client.targetInventory.getPosition(), %client.player.getPosition()) <= 7 && isObject(%client.targetInventory.triggeredBy))
      %client.isMoving = false;
    else
    {
      %client.isMoving = true;
      %client.moveLocation = %client.targetInventory.getPosition();
    }
  }
  else
    %client.shouldRearm = false; // No inventories?
}
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: A task that actually makes the bots return a flag that's nearby.
//------------------------------------------------------------------------------------------`
function AIEnhancedReturnFlagTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedReturnFlagTask::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedReturnFlagTask::retire(%task, %client) { }

function AIEnhancedReturnFlagTask::weight(%task, %client) 
{
  %flag = nameToID("Team" @ %client.team @ "Flag1");
  if (!isObject(%flag) || %flag.isHome)
  {
    %task.setWeight(0);
    %client.targetFlag = -1;
    %client.isMoving = false;
  }
  else
  {
    // TODO: For now, all the bots go after it! Make this check if the bot is range.
    %task.setWeight(700);
    
    %client.targetFlag = %flag;
  }
}

function AIEnhancedReturnFlagTask::monitor(%task, %client)
{       
  if (!isObject(%client.targetFlag))
    return;
  
  // TODO: Make the bot engage the flag runner if its currently held.
  %client.isMoving = true;
  %client.moveLocation = %client.targetFlag.getPosition();
}
//------------------------------------------------------------------------------------------

function ObjectiveNameToVoice(%objective)
{
  %result = "avo.grunt";
  switch$(%objective)
  {
    case "AIEnhancedReturnFlagTask":
      %result = "slf.def.flag";
    case "AIEnhancedRearmTask":
      %result = "avo.grunt";
    case "AIEnhancedEngageTarget":
      %result = "slf.att.attack";
    case "AIEnhancedScoutLocation":
      %result = "slf.def.defend";
    case "AIEnhancedEscort":
      %result = "slf.tsk.cover";
  }
  
  return %result;
}
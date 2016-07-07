//------------------------------------------------------------------------------------------
// main.cs
// Source file for the DXAI enhanced objective implementations.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

$DXAI::Task::NoPriority = 0;
$DXAI::Task::LowPriority = 100;
$DXAI::Task::MediumPriority = 200;
$DXAI::Task::HighPriority = 500;
$DXAI::Task::VeryHighPriority = 1000;
$DXAI::Task::ReservedPriority = 5000;

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
function AIEnhancedEscort::weight(%task, %client) { %task.setWeight($DXAI::Task::MediumPriority); }

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

    %client.setMoveTarget(getRandomPositionOnTerrain(%escortLocation, 40));
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
function AIEnhancedDefendLocation::assume(%task, %client) { %task.setMonitorFreq(32); }
function AIEnhancedDefendLocation::retire(%task, %client) { %client.isMoving = false; }
function AIEnhancedDefendLocation::weight(%task, %client) { %task.setWeight($DXAI::Task::MediumPriority); }

function AIEnhancedDefendLocation::monitor(%task, %client)
{
    if (%client.getPathDistance(%client.defendTargetLocation) <= 40)
    {
        // Pick a random time to move to a nearby location
        if (%client.defendTime == -1)
        {
            %client.nextDefendRotation = getRandom(5000, 10000);
            %client.setMoveTarget(-1);
        }

        // If we're near our random point, just don't move
        if (%client.getPathDistance(%client.moveLocation) <= 10)
            %client.setMoveTarget(-1);

        %client.defendTime += %task.getMonitorTimeMS();
        if (%client.defendTime >= %client.nextDefendRotation)
        {
            %client.defendTime = 0;
            %client.nextDefendRotation = getRandom(5000, 10000);

            %nextPosition = NavGraph.nodeLoc(NavGraph.randNode(%client.player.getPosition(), 40, true, true));

            // If it isn't far enough, just pass on moving. This will help prevent bots jumping up in the air randomly.
            if (vectorDist(%client.player.getPosition(), %nextPosition) > 5)
                %client.setMoveTarget(%nextPosition);
        }
    }
    else
    {
        %client.defendTime = -1;
        %client.setMoveTarget(%client.defendTargetLocation);
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
function AIEnhancedScoutLocation::assume(%task, %client) { %task.setMonitorFreq(32); %client.currentNode = -1; }
function AIEnhancedScoutLocation::retire(%task, %client) { }
function AIEnhancedScoutLocation::weight(%task, %client) { %task.setWeight($DXAI::Task::MediumPriority); }

function AIEnhancedScoutLocation::monitor(%task, %client)
{
    if (%client.engageTarget)
        return AIEnhancedScoutLocation::monitorEngage(%task, %client);

    // We can't really work without a NavGraph
    if (!isObject(NavGraph))
        return;

    // We just received the task, so find a node within distance of our scout location
    if (%client.currentNode == -1)
    {
        %client.currentNode = NavGraph.randNode(%client.scoutTargetLocation, %client.scoutDistance, true, true);

        if (%client.currentNode != -1)
            %client.setMoveTarget(NavGraph.nodeLoc(%client.currentNode));
    }
    // We're moving, or are near enough to our target
    else
    {
        %pathDistance = %client.getPathDistance(%client.moveTarget);

        // Don't move if we're close enough to our next node
        if (%pathDistance <= 40 && %client.isMovingToTarget)
        {
            %client.setMoveTarget(-1);
            %client.nextScoutRotation = getRandom(5000, 10000);
            %client.scoutTime += %task.getMonitorTimeMS();
        }
        else if(%client.getPathDistance(%client.moveTarget) > 40)
        {
          //  %client.setMoveTarget(%client.moveTarget);
            %client.scoutTime = 0;
        }
        else
            %client.scoutTime += %task.getMonitorTimeMS();

        // Wait a little bit at each node
        if (%client.scoutTime >= %client.nextScoutRotation)
        {
            %client.scoutTime = 0;
            %client.nextScoutRotation = getRandom(5000, 10000);

            // Pick a new node
            %client.currentNode = NavGraph.randNode(%client.scoutTargetLocation, %client.scoutDistance, true, true);

            // Ensure that we found a node.
            if (%client.currentNode != -1)
               %client.setMoveTarget(NavGraph.nodeLoc(%client.currentNode));
        }
    }
}

function AIEnhancedScoutLocation::monitorEngage(%task, %client)
{
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
    // Blow through seen targets
    %chosenTarget = -1;
    %chosenTargetDistance = 9999;

    %botPosition = %client.player.getPosition();
    for (%iteration = 0; %iteration < %client.visibleHostiles.getCount(); %iteration++)
    {
        %current = %client.visibleHostiles.getObject(%iteration);

        %targetDistance = vectorDist(%current.getPosition(), %botPosition);

        // FIXME: We immediately forget about the asshole here
        if (%targetDistance < %chosenTargetDistance && %client.player.canSeeObject(%current, %client.viewDistance, %client.fieldOfView))
        {
            %chosenTargetDistance = %targetDistance;
            %chosenTarget = %current;
        }
    }

    %client.engageTarget = %chosenTarget;
    if (!isObject(%client.engageTarget) && %client.engageTargetLastPosition $= "")
    {
        %task.setWeight($DXAI::Task::NoPriority);

        // Make sure we disable the pack
        if (%client.player.getPackName() $= "ShieldPack")
        {
            %client.player.setImageTrigger(2, false);
            %client.rechargingEnergy = false;
        }
    }
    else
    {
        %task.setWeight($DXAI::Task::VeryHighPriority);

        // If we have a shield pack on, use it
        if (%client.player.getPackName() $= "ShieldPack" && %client.player.getEnergyLevel() >= 30 && !%client.rechargingEnergy)
            %client.player.setImageTrigger(2, true);
        else if (%client.player.getPackName() $= "ShieldPack" && %client.player.getEnergyLevel() >= 70)
        {
            %client.player.setImageTrigger(2, true);
            %client.rechargingEnergy = false;
        }
        else if (%client.player.getPackName() $= "ShieldPack")
        {
            // We ran out of energy, let the pack recharge for a bit
            %client.player.setImageTrigger(2, false);
            %client.rechargingEnergy = true;
        }
    }
}

function AIEnhancedEngageTarget::monitor(%task, %client)
{
    if (isObject(%client.engageTarget))
    {
        if (%client.engageTarget.getState() !$= "Move")
        {
            %client.engageTarget = -1;
            %client.engageTargetLastPosition = "";
            return;
        }

        // Calculate the T between the target and this bot
        %normal = vectorNormalize(vectorSub(%client.engageTarget.getWorldBoxCenter(), %client.player.getWorldBoxCenter()));

        %forwardAngle = mAtan(getWord(%normal, 1), getWord(%normal, 0));
        %horizontalMaxAngle = %forwardAngle + mDegToRad(90);
        %horizontalMinAngle = %forwardAngle - mDegToRad(90);

        %randomAngle = getRandomFloat(%horizontalMaxAngle, %horizontalMinAngle);

        // FIXME: Maintain weapon distance
        %minDist = 20;
        %maxDist = 30;

        %distance = getRandom(%minDist, %maxDist);

        // Calculate a final point
        %normal = mSin(%randomAngle) SPC mCos(%randomAngle);

        %targetPoint = vectorAdd(%client.engageTarget.getWorldBoxCenter(), vectorScale(%normal, %distance));
        %targetPoint = setWord(%targetPoint, 2, getTerrainHeight(%targetPoint));

        %targetPoint = vectorAdd(%targetPoint, "0 0 2");
        %client.engageTargetLastPosition = %client.engageTarget.getWorldBoxCenter();

        if (%client.engageJetTiming $= "")
            %client.engageJetTiming = getRandom(500, 10000);
        else
            %client.engageJetTime += %task.getMonitorTimeMS();

        if (%client.engageJetTime >= %client.engageJetTiming && %client.player.getEnergyPercent() >= 0.5)
        {
            %client.combatJetTiming = getRandom(1000, 1500) + %client.engageJetTiming;
            %client.runJets(%client.combatJetTiming);

            %client.engageJetTiming = "";
            %client.engageJetTime = 0;

            %client.isCombatJetting = true;

            %client.setMoveTarget(vectorScale(%client.engageTarget.getBackwardsVector(), 20));
        }

        if (%client.isCombatJetting && %client.combatJetTime < %client.combatJetTiming && %client.player.getEnergyPercent() >= 0.2)
        {
            %client.combatJetTime += %task.getMonitorTimeMS();
            %client.setMoveTarget(vectorScale(%client.engageTarget.getBackwardsVector(), 20));
        }
        else
        {
            %client.setMoveTarget(%targetPoint);

            %client.combatJetTime = 0;
            %client.isCombatJetting = false;
        }
    }
    else if (%client.engageTargetLastPosition !$= "")
    {
        %client.setMoveTarget(%client.engageTargetLastPosition);

        if (vectorDist(%client.player.getPosition(), %client.engageTargetLastPosition) <= 10)
        {
            %client.engageTargetLastPosition = "";
            %client.setMoveTarget(-1);
        }
    }
}

//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
//      +Param %bot.shouldRearm: A boolean representing whether or not this bot should go
// and rearm.
//      +Param %bot.rearmTarget: The ID of the inventory station to rearm at.
//------------------------------------------------------------------------------------------`
function AIEnhancedRearmTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedRearmTask::assume(%task, %client) { %task.setMonitorFreq(32); }
function AIEnhancedRearmTask::retire(%task, %client) { }

function AIEnhancedRearmTask::weight(%task, %client)
{
    if (%client.shouldRearm)
        %task.setWeight($DXAI::Task::HighPriority);
    else
        %task.setWeight($DXAI::Task::NoPriority);

    %task.setMonitorFreq(getRandom(10, 32));
}

function AIEnhancedRearmTask::monitor(%task, %client)
{
    if (!isObject(%client.rearmTarget))
        %client.rearmTarget = %client.getClosestInventory();

    if (isObject(%client.rearmTarget))
    {
        // Politely wait if someone is already on it.
        if (vectorDist(%client.rearmTarget.getPosition(), %client.player.getPosition()) <= 7 && isObject(%client.rearmTarget.triggeredBy))
            %client.setMoveTarget(-1);
        else
            %client.setMoveTarget(%client.rearmTarget.getPosition());
    }
    else
        %client.shouldRearm = false; // No inventories?
}
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: A task that actually makes the bots return a flag that's nearby.
//------------------------------------------------------------------------------------------`
function AIEnhancedReturnFlagTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedReturnFlagTask::assume(%task, %client) { %task.setMonitorFreq(32); }
function AIEnhancedReturnFlagTask::retire(%task, %client) { }

function AIEnhancedReturnFlagTask::weight(%task, %client)
{
    %flag = nameToID("Team" @ %client.team @ "Flag1");
    if (!isObject(%flag) || %flag.isHome)
    {
      //  %client.setMoveTarget(-1);
        %task.setWeight($DXAI::Task::NoPriority);
    }
    else
    {
        // TODO: For now, all the bots go after it! Make this check if the bot is range.
        %task.setWeight($DXAI::Task::HighPriority);
        %client.returnFlagTarget = %flag;
    }
}

function AIEnhancedReturnFlagTask::monitor(%task, %client)
{
    if (!isObject(%client.returnFlagTarget))
        return;

    if (isObject(%client.engageTarget) && %client.engageTarget.getState() $= "Move")
        AIEnhancedReturnFlagTask::monitorEngage(%task, %client);
    else
        %client.setMoveTarget(%client.returnFlagTarget.getPosition());
}

function AIEnhancedReturnFlagTask::monitorEngage(%task, %client)
{
}

//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: A task that performs path correction.
//------------------------------------------------------------------------------------------`
function AIEnhancedPathCorrectionTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedPathCorrectionTask::assume(%task, %client) { %task.setMonitorFreq(2); }
function AIEnhancedPathCorrectionTask::retire(%task, %client) { }

function AIEnhancedPathCorrectionTask::weight(%task, %client)
{
    if (%client.isPathCorrecting)
        %task.setWeight($DXAI::Task::VeryHighPriority);
    else
        %task.setWeight($DXAI::Task::NoPriority);
}

function AIEnhancedPathCorrectionTask::monitor(%task, %client)
{
    if (%client.isPathCorrecting)
    {
        if (%client.player.getEnergyPercent() >= 1)
            %client.isPathCorrecting = false;
        else
            %client.setMoveTarget(-1);
    }

}
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: A task that triggers bots to grab & run the enemy flag.
//------------------------------------------------------------------------------------------
function AIEnhancedFlagCaptureTask::initFromObjective(%task, %objective, %client) { }
function AIEnhancedFlagCaptureTask::assume(%task, %client) { %task.setMonitorFreq(1); }
function AIEnhancedFlagCaptureTask::retire(%task, %client) { }

function AIEnhancedFlagCaptureTask::weight(%task, %client)
{
    if (%client.shouldRunFlag)
    {
        // First, is the enemy flag home?
        %enemyTeam = %client.team == 1 ? 2 : 1;
        %enemyFlag = nameToID("Team" @ %enemyTeam @ "Flag1");

        if (isObject(%enemyFlag) && %enemyFlag.isHome)
        {
            %client.targetCaptureFlag = %enemyFlag;
            %task.setWeight($DXAI::Task::MediumPriority);
        }
    }
    else
        %task.setWeight($DXAI::Task::NoPriority);
}

function AIEnhancedFlagCaptureTask::monitor(%task, %client)
{
    if (!isObject(%client.targetCaptureFlag) && !%client.hasFlag)
      return;

    if (!%client.hasFlag)
        %client.setMoveTarget(%client.targetCaptureFlag.getPosition());
    else
        %client.setMoveTarget(nameToID("Team" @ %client.team @ "Flag1").getPosition());
}
//------------------------------------------------------------------------------------------

function ObjectiveNameToVoice(%bot)
{
    %objective = %bot.getTaskName();

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
      case "AIEnhancedDefendLocation":
        switch$(%bot.defenseDescription)
        {
            case "flag":
                %result = "slf.def.flag";
            case "generator":
                %result = "slf.def.generator";
            default:
                %result = "slf.def.defend";
        }
      case "AIEnhancedEscort":
        %result = "slf.tsk.cover";
    }

    return %result;
}

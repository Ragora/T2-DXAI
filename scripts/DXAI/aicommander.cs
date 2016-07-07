//------------------------------------------------------------------------------------------
// aicommander.cs
// Source file for the DXAI commander AI implementation.
// https://github.com/Ragora/T2-DXAI.git
//
// The AICommander type is a complex beast. They have the following proerties associated
// with them:
//      * %commander.botList: A SimSet of all bots that are currently associated with the
// given commander.
//      * %commander.idleBotList: A SimSet of all bots that are currently considered be idle.
// These bots were not explicitly given anything to do by the commander AI and so they are
// not doing anything particularly helpful.
//      * %commander.botAssignments[%assignmentID]: An associative container that maps
// assignment ID's (those desiginated by $DXAI::Priorities::*) to the total number of
// bots assigned.
//      * %commander.objectiveCycles[%assignmentID]: An associative container that maps assignment
// ID's (those desiginated by $DXAI::Priorities::*) to an instance of a CyclicSet which contains
// the ID's of AI nav graph placed objective markers to allow for cycling through the objectives
// set for the team.
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

$DXAI::ActiveCommanderCount = 2;

$DXAI::Priorities::DefendGenerator = 0;
$DXAI::Priorities::DefendFlag = 1;
$DXAI::Priorities::ScoutBase = 2;
//-----------------------------------------------
$DXAI::Priorities::CaptureFlag = 3;
$DXAI::Priorities::CaptureObjective = 5;
$DXAI::Priorities::AttackTurret = 6;
$DXAI::Priorities::Count = 4;

//------------------------------------------------------------------------------------------
// Description: These global variables are the default priorities that commanders will
// initialize with for specific tasks that can be distributed to the bots on the team.
//
// NOTE: These should be fairly laid back initially and allow for a good count of idle bots.
//------------------------------------------------------------------------------------------
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::DefendGenerator] = 2;
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::DefendFlag] = 3;
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::ScoutBase] = 1;
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::CaptureFlag] = 2;

$DXAI::Priorities::Text[$DXAI::Priorities::DefendGenerator] = "Defending a Generator";
$DXAI::Priorities::Text[$DXAI::Priorities::DefendFlag] = "Defending the Flag";
$DXAI::Priorities::Text[$DXAI::Priorities::ScoutBase] = "Scouting a Location";
$DXAI::Priorities::Text[$DXAI::Priorities::CaptureFlag] = "Capture the Flag";

//------------------------------------------------------------------------------------------
// Description: Sets up the AI commander by creating the bot list sim sets as well as
// claiming the bots that are currently on their team. Bots claimed will have all of their
// independent ticks started up such as the visual acuity tick.
//------------------------------------------------------------------------------------------
function AICommander::setup(%this)
{
    %this.botList = new SimSet();
    %this.idleBotList = new SimSet();

    for (%iteration = 0; %iteration < ClientGroup.getCount(); %iteration++)
    {
        %currentClient = ClientGroup.getObject(%iteration);

        if (%currentClient.isAIControlled() && %currentClient.team == %this.team)
        {
            %this.botList.add(%currentClient);
            %this.idleBotList.add(%currentClient);

            %currentClient.commander = %this;

            %currentClient.initialize();
            %currentClient.visibleHostiles = new SimSet();

            // Start our ticks.
            %currentClient.updateVisualAcuity();
            %currentClient.stuckCheck();
        }
    }

    %this.setDefaultPriorities();

    // Also set the assignment tracker and the cyclers for each objective type
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
    {
        %this.assignments[%iteration] = 0;
        %this.objectiveCycles[%iteration] = CyclicSet::create();
    }
}

//------------------------------------------------------------------------------------------
// Description: Skims the objectives for the AI commander's team and pulls out any that
// we can use when assigning tasks to bots. This is done with some recursion down the line
// of nested SimGroup instances. When a usable objective is located, it is added to the
// cycler associated with the most appropriate task.
// Param %group: The group to recurse down.
// NOTE: This is an internal function and therefore should not be called directly.
//------------------------------------------------------------------------------------------
function AICommander::_skimObjectiveGroup(%this, %group)
{
    for (%iteration = 0; %iteration < %group.getCount(); %iteration++)
    {
        %current = %group.getObject(%iteration);

        // We're getting ballsy here, recursion in TS!
        if (%current.getClassName() $= "SimGroup")
            %this._skimObjectiveGroup(%current);
        else
        {
            // Which objective type are we looking at?
            switch$ (%current.getName())
            {
                case "AIOAttackObject":
                case "AIOMortarObject":
                case "AIODefendLocation":
                    // FIXME: Console spam from .targetObjectID not being set?
                    %datablockName = %current.targetObjectID.getDatablock().getName();

                    // Defending the flag?
                    if (%datablockName $= "FLAG")
                        %this.objectiveCycles[$DXAI::Priorities::DefendFlag].add(%current);
                    else if (%datablockName $="GeneratorLarge")
                        %this.objectiveCycles[$DXAI::Priorities::DefendGenerator].add(%current);

                case "AIORepairObject":
                case "AIOTouchObject":
                case "AIODeployEquipment":
            }
        }
    }
}

//------------------------------------------------------------------------------------------
// Description: Loads or reloads the objectives for the AI commander's team. It searches
// for the AIObjectives group associated with the team (all teams have one called that)
// and passes it off to _skimObjectGroup which does the actual objective processing.
//------------------------------------------------------------------------------------------
function AICommander::loadObjectives(%this)
{
    // First we clear the old cyclers
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
        %this.objectiveCycles[%iteration].clear();

    %teamGroup = "Team" @ %this.team;
    %teamGroup = nameToID(%teamGroup);

    if (!isObject(%teamGroup))
        return;

    // Search this group for something named "AIObjectives". Each team has one, so we can't reliably just use that name
    %group = %teamGroup;
    for (%iteration = 0; %iteration < %group.getCount(); %iteration++)
    {
        %current = %group.getObject(%iteration);
        if (%current.getClassName() $= "SimGroup" && %current.getName() $= "AIObjectives")
        {
            %group = %current;
            break;
        }
    }

    if (%group == %teamGroup)
        return;

    // Now that we have our objective set, skim it for anything usable
    %this._skimObjectiveGroup(%group);

    // We also need to determine some locations for objectives not involved in the original game, such as the AIEnhancedScout task.

    // Simply create a scout objective on the flag with a distance of 100m
    %scoutLocationObjective = new ScriptObject() { distance = 100; };
    %defendFlagObjective = %this.objectiveCycles[$DXAI::Priorities::DefendFlag].next();
    %scoutLocationObjective.location = %defendFlagObjective.location;

    %this.objectiveCycles[$DXAI::Priorities::ScoutBase].add(%scoutLocationObjective);
}

//------------------------------------------------------------------------------------------
// Description: Distributes and assigns tasks to all bots under the jurisdiction of the
// AI commander according to current tasks priorities.
//
// TODO: Assign something to bots that are considered to be idle so they can stop sitting
// on top of the inventory stations like lazy bums.
// FIXME: This will be called when the commander wants to distribute tasks differently
// than what was previous set, and so blindly instructing bots to rearm isn't exactly
// the best choice in hindsight.
//------------------------------------------------------------------------------------------
function AICommander::assignTasks(%this)
{
    // First, assign objectives that all bots should have
    for (%iteration = 0; %iteration < %this.botList.getCount(); %iteration++)
    {
      %bot = %this.botList.getObject(%iteration);
      %bot.addTask(AIEnhancedEngageTarget);
      %bot.addTask(AIEnhancedRearmTask);
      %bot.addTask(AIEnhancedPathCorrectionTask);

      // We only need this task if we're actually playing CTF.
      if ($CurrentMissionType $= "CTF")
      {
        %bot.addTask(AIEnhancedReturnFlagTask);
        %bot.addTask(AIEnhancedFlagCaptureTask);
      }

      // Assign the default loadout
      %bot.targetLoadout = $DXAI::DefaultLoadout ;
      %bot.shouldRearm = true;
    }

    // Calculate how much priority we have total
    %totalPriority = 0.0;
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
    {
        %totalPriority += %this.priorities[%iteration];
        %botAssignments[%iteration] = 0;
    }

    // We create a priority queue preemptively so we can sort task priorities as we go and save a little bit of time
    %priorityQueue = PriorityQueue::create();

    // Now calculate how many bots we need per objective, and count how many we will need in total
    %lostBots = false; // Used for a small optimization
    %botCountRequired = 0;
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
    {
        %botAssignments[%iteration] = mCeil(((%totalPriority / $DXAI::Priorities::Count) * %this.priorities[%iteration]) / $DXAI::Priorities::Count);
        %botAssignments[%iteration] -= %this.botAssignments[%iteration]; // If we already have bots doing this, then we don't need to replicate them
        %botCountRequired += %botAssignments[%iteration];
        if (%botAssignments[%iteration] < 0)
            %lostBots = true;
        else
        {
            %priorityQueue.add(%botAssignments[%iteration], %iteration);
            echo(%botAssignments[%iteration] SPC " bots on task " @ $DXAI::Priorities::Text[%iteration]);
        }
    }

    // Deassign from objectives we need less bots for now and put them into the idle list
    // When we lose bots, our %botAssignments[%task] value will be a negative, so we just need
    // to ditch mAbs(%botAssignments[%task]) bots from that given task.
    for (%taskIteration = 0; %lostBots && %taskIteration < $DXAI::Priorities::Count; %taskiteration++)
        // Need to ditch some bots
        if (%botAssignments[%taskIteration] < 0)
            %this.deassignBots(%taskIteration, mAbs(%botAssignments[%taskIteration]));

    // Do we have enough idle bots to just shunt everyone into something?
    if (%this.idleBotList.getCount() >= %botCountRequired)
    {
        for (%taskIteration = 0; %taskIteration < $DXAI::Priorities::Count; %taskiteration++)
            for (%botIteration = 0; %botIteration < %botAssignments[%taskIteration]; %botIteration++)
                %this.assignTask(%taskIteration, %this.idleBotList.getObject(0));
    }
    // Okay, we don't have enough bots currently so we'll try to satisfy the higher priority objectives first
    else
        while (!%priorityQueue.isEmpty() && %this.idleBotList.getCount() != 0)
        {
            %taskID = %priorityQueue.topValue();
            %requiredBots = %priorityQueue.topKey();
            %priorityQueue.pop();

            for (%botIteration = 0; %botIteration < %requiredBots && %this.idleBotList.getCount() != 0; %botIteration++)
                %this.assignTask(%taskID, %this.idleBotList.getObject(0));
        }

    // Regardless, we need to make sure we cleanup the queue
    // FIXME: Perhaps just create one per commander and reuse it?
    %priorityQueue.delete();
}

//------------------------------------------------------------------------------------------
// Description:
//------------------------------------------------------------------------------------------
function AICommander::deassignBots(%this, %taskID, %count)
{
    // TODO: More efficient removal?
    for (%iteration = 0; %count > 0 && %iteration < %this.botList.getCount(); %iteration++)
    {
        %bot = %this.botList.getObject(%iteration);
        if (%bot.assignment == %taskID)
        {
            %bot.clearTasks();
            %this.idleBotList.add(%bot);
            %count--;
        }
    }

    return %count == 0;
}

function AICommander::assignTask(%this, %taskID, %bot)
{
    // Don't try to assign if the bot is already assigned something
    if (!%this.idleBotList.isMember(%bot))
        return;

    %this.idleBotList.remove(%bot);

    switch (%taskID)
    {
        case $DXAI::Priorities::DefendGenerator or $DXAI::Priorities::DefendFlag:
            %objective = %this.objectiveCycles[%taskID].next();

            // Set the bot to defend the location
            %bot.defendTargetLocation = %objective.location;
            %datablockName = %objective.targetObjectID.getDatablock().getName();

            switch$(%datablockName)
            {
                case "FLAG":
                    %bot.defenseDescription = "flag";
                case "GeneratorLarge":
                    %bot.defenseDescription = "generator";
            }

            %bot.primaryTask = "AIEnhancedDefendLocation";
            %bot.addTask(%bot.primaryTask);

        case $DXAI::Priorities::ScoutBase:
            %objective = %this.objectiveCycles[%taskID].next();

            // Set the bot to defend the location
            %bot.scoutTargetLocation = %objective.location;
            %bot.scoutDistance = %objective.distance;

            %bot.primaryTask = "AIEnhancedScoutLocation";
            %bot.addTask(%bot.primaryTask);

        case $DXAI::Priorities::CaptureFlag:
            %bot.shouldRunFlag = true;
    }

    %this.botAssignments[%taskID]++;
    %bot.assignment = %taskID;
}

//------------------------------------------------------------------------------------------
// Description: Helper function that is used to set the default task prioritizations on
// the commander object.
//------------------------------------------------------------------------------------------
function AICommander::setDefaultPriorities(%this)
{
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
        %this.priorities[%iteration] = $DXAI::Priorities::DefaultPriorityValue[%iteration];
}

//------------------------------------------------------------------------------------------
// Description: Performs a deinitialization that should be ran before deleting the
// commander object itself.
//
// NOTE: This is called automatically by .delete so this shouldn't have to be called
// directly.
//------------------------------------------------------------------------------------------
function AICommander::cleanUp(%this)
{
    for (%iteration = 0; %iteration < %this.botList.getCount(); %iteration++)
    {
        %current = %this.botList.getObject(%iteration);
        cancel(%current.visualAcuityTick);
        cancel(%current.stuckCheckTick);
    }

    %this.botList.delete();
    %this.idleBotList.delete();
}

//------------------------------------------------------------------------------------------
// Description: An overwritten delete function to perform proper cleanup before actually
// deleting the commander object.
//------------------------------------------------------------------------------------------
function AICommander::delete(%this)
{
    %this.cleanUp();
    ScriptObject::delete(%this);
}

function AICommander::update(%this)
{
    for (%iteration = 0; %iteration < %this.botList.getCount(); %iteration++)
        %this.botList.getObject(%iteration).update();
}

//------------------------------------------------------------------------------------------
// Description: Removes the given bot from this AI commander's jurisdiction.
// Param %bot: The AIConnection to remove.
//------------------------------------------------------------------------------------------
function AICommander::removeBot(%this, %bot)
{
    %this.botList.remove(%bot);
    %this.idleBotList.remove(%bot);

    %bot.commander = -1;
}

//------------------------------------------------------------------------------------------
// Description: Adds the given bot to the jurisdiction of this AI commander if the bot and
// commander work for the same team.
// Param %bot: The AIConnection to add.
// Return: A boolean representing whether or not the bot was successfully added. False is
// returned if the two are not on the same time at the time of adding.
//------------------------------------------------------------------------------------------
function AICommander::addBot(%this, %bot)
{
    if (%bot.team != %this.team)
        return false;

    %this.botList.add(%bot);
    %this.idleBotList.add(%bot);

    %bot.commander = %this;
    return true;
}

function AICommander::notifyPlayerDeath(%this, %killedClient, %killedByClient)
{
}

function AICommander::notifyFlagGrab(%this, %grabbedByClient)
{
    %this.priority[$DXAI::Priorities::DefendFlag]++;

    // ...well, balls, someone nipped me flag! Are there any bots sitting around being lazy?
    // TODO: We should also include nearby scouting bots into this, as well.
    if (%this.idleBotList.getCount() != 0)
    {
        // Go full-force and try to kill that jerk!
        for (%iteration = 0; %iteration < %this.idleBotList.getCount(); %iteration++)
        {
            %idleBot = %this.idleBotList.getObject(%iteration);
            %idleBot.attackTarget = %grabbedByClient.player;
        }
    }
}

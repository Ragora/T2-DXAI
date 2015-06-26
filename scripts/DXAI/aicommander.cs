//------------------------------------------------------------------------------------------
// aicommander.cs
// Source file for the DXAI commander AI implementation.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

$DXAI::ActiveCommanderCount = 2;

$DXAI::Priorities::DefendGenerator = 1;
$DXAI::Priorities::DefendFlag = 2;
$DXAI::Priorities::ScoutBase = 3;
//-----------------------------------------------
$DXAI::Priorities::CaptureFlag = 4;
$DXAI::Priorities::CaptureObjective = 5;
$DXAI::Priorities::AttackTurret = 6;
$DXAI::Priorities::Count = 3;

// # of bots assigned = mCeil(((totalPriorityValues / priorityCounts) * priority) / priorityCounts)

// totalPriorityValues = 3 + 2
// priorityCounts = 2
// Priority for Gen: 2
// Priority for Flag: 3
// flagBots = ((5.0 / 2.0) * 3.0) / 2.0
// Gen bots = ((5.0 / 2.0) * 2.0) / 2.0
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::DefendGenerator] = 2;
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::DefendFlag] = 3;
$DXAI::Priorities::DefaultPriorityValue[$DXAI::Priorities::ScoutBase] = 1;

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
        }
    }
    
    %this.setDefaultPriorities();
    
    // Also set the assignment tracker
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
        %this.assignments[%iteration] = 0;
}

function AICommander::assignTasks(%this)
{
    // Calculate how much priority we have total, first
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
            %priorityQueue.add(%iteration, %botAssignments[%iteration]);
    }
    
    // Deassign from objectives we need less bots for now and put them into the idle list
    // When we lose bots, our %botAssignments[%task] value will be a negative, so we just need
    // to ditch mAbs(%botAssignments[%task]) bots from that given task.
    for (%taskIteration = 0; %lostBots && %taskIteration < $DXAI::Priorities::Count; %taskiteration++)
        // Need to ditch some bots
        if (%botAssignments[%taskIteration] < 0)
            %this.deassignBots(%taskIteration, mAbs(%botAssignments[%taskIteration]);
    
    // Do we have enough idle bots to just shunt everyone into something?
    if (%this.idleBotList.getCount() >= %botCountRequired)
    {
        for (%taskIteration = 0; %taskIteration < $DXAI::Priorities::Count; %taskiteration++)
            for (%botIteration = 0; %botIteration < %botAssignments[%taskIteration]; %botIteration++)
                %this.assignTask(%taskIteration, %this.idleBotList.getObject(0));
    }
    // Okay, we don't have enough bots currently so we'll try to satisify the higher priority objectives first
    else
        while (!%priorityQueue.isEmpty() && %this.idleBotList.getCount() != 0)
        {
            %taskID = %priorityQueue.topKey();
            %requiredBots = %priorityQueue.topValue();
            %priorityQueue.pop();
            
            for (%botIteration = 0; %botIteration < %requiredBots && %this.idleBotList.getCount() != 0; %botIteration++)
                %this.assignTask(%taskID, %this.idleBotList.getObject(0));
        }
    
    // Regardless, we need to make sure we cleanup the queue
    // FIXME: Perhaps just create one per commander and reuse it?
    %priorityQueue.delete();
}

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
    if (!%this.idleBotList.contains(%this))
        return;
    
    %this.idleBotList.remove(%this);
    
    switch (%taskID)
    {
        case $DXAI::Priorities::DefendGenerator:
            break;
        case $DXAI::Priorities::DefendFlag:
            break;
        case $DXAI::Priorities::ScoutBase:
            break;
    }
    
    %this.botAssignments[%taskID]++;
    %bot.assignment = %taskID;
}

function AICommander::setDefaultPriorities(%this)
{
    for (%iteration = 0; %iteration < $DXAI::Priorities::Count; %iteration++)
        %this.priorities[%iteration] = $DXAI::Priorities::DefaultPriorityValue[%iteration];
}

function AICommander::cleanUp(%this)
{
    %this.botList.delete();
    %this.idleBotList.delete();
}

function AICommander::update(%this)
{
    for (%iteration = 0; %iteration < %this.botList.getCount(); %iteration++)
        %this.botList.getObject(%iteration).update();
}

function AICommander::removeBot(%this, %bot)
{
    %this.botList.remove(%bot);
    %this.idleBotList.remove(%bot);
    
    %bot.commander = -1;
}

function AICommander::addBot(%this, %bot)
{
    if (!%this.botList.isMember(%bot))
        %this.botList.add(%bot);
    
    if (!%this.idleBotList.isMember(%bot))
        %this.idleBotList.add(%bot);
    
    %bot.commander = %this;
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
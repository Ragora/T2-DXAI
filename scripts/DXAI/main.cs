//------------------------------------------------------------------------------------------
// main.cs
// Main source file for the DXAI experimental AI enhancement project.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

exec("scripts/DXAI/objectives.cs");
exec("scripts/DXAI/helpers.cs");
exec("scripts/DXAI/config.cs");
exec("scripts/DXAI/aicommander.cs");
exec("scripts/DXAI/aiconnection.cs");
exec("scripts/DXAI/priorityqueue.cs");
exec("scripts/DXAI/cyclicset.cs");
exec("scripts/DXAI/loadouts.cs");

//------------------------------------------------------------------------------------------
// Description: This cleanup function is called when the mission ends to clean up all
// active commanders and to delete them for the next mission which guarantees a blank
// slate.
//------------------------------------------------------------------------------------------
function DXAI::cleanup()
{
    $DXAI::System::Setup = false;
    
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
        $DXAI::ActiveCommander[%iteration].delete();
    
    $DXAI::ActiveCommanderCount = 0;
}

//------------------------------------------------------------------------------------------
// Description: This cleanup function is called when the mission starts to to instantiate
// and setup all AI commanders for the game.
// Param %numTeams: The number of teams to initialize for.
//
// TODO: Perhaps calculate %numTeams from the game object?
//------------------------------------------------------------------------------------------
function DXAI::setup(%numTeams)
{
    // Mark the environment as invalidated for each new run so that our hooks
    // can be verified
    $DXAI::System::InvalidatedEnvironment = true;
    
    // Set our setup flag so that the execution hooks can behave correctly
    $DXAI::System::Setup = true;
    
    for (%iteration = 1; %iteration < %numTeams + 1; %iteration++)
    {
        %commander = new ScriptObject() { class = "AICommander"; team = %iteration; };
        %commander.setup();
        
        $DXAI::ActiveCommander[%iteration] = %commander;
        %commander.loadObjectives();
        %commander.assignTasks();
    }
    
    // And setup the default values
    for (%iteration = 0; %iteration < ClientGroup.getCount(); %iteration++)
    {
        %currentClient = ClientGroup.getObject(%iteration);
        
        %currentClient.viewDistance = $DXAI::Bot::DefaultViewDistance;
        %currentClient.fieldOfView = $DXAI::Bot::DefaultFieldOfView;
    }
        
    $DXAI::ActiveCommanderCount = %numTeams;
}

//------------------------------------------------------------------------------------------
// Why: Due to the way the AI system must hook into some functions and the way game 
// modes work, we must generate runtime overrides for some gamemode related functions. We
// can't simply hook DefaultGame functions base game modes will declare their own and so
// we'll need to hook those functions post-start as the game mode scripts are executed for
// each mission run.
// Description: This function is called once per update tick (roughly 32 milliseconds) to
// check that the hooks we need are actually active if the system detects that may be a
// necessity to do so. A runtime check is initiated at gamemode start and for each exec
// call made during runtime as any given exec can overwrite the hooks we required.
// If they were not overwritten, the function will return 11595 and do nothing else if the 
// appropriate dummy parameters are passed in.
//
// TODO: Perhaps calculate %numTeams from the game object?
//------------------------------------------------------------------------------------------
function DXAI::validateEnvironment()
{
        %gameModeName = $CurrentMissionType @ "Game";
        
        %payloadTemplate =  %payload = "function " @ %gameModeName @ "::<METHODNAME>() { return DefaultGame::<METHODNAME>($DXAI::System::RuntimeDummy); } ";
        if (game.AIChooseGameObjective($DXAI::System::RuntimeDummy) != 11595)
        {
            error("DXAI: Function 'DefaultGame::AIChooseGameObjective' detected to be overwritten by the current gamemode. Correcting ...");
            
            eval(strReplace(%payloadTemplate, "<METHODNAME>", "AIChooseGameObjective"));
            
            // Make sure the patch took
            if (game.AIChooseGameObjective($DXAI::System::RuntimeDummy) != 11595)
                error("DXAI: Failed to patch 'DefaultGame::AIChooseGameObjective'! DXAI may not function correctly.");
        }
        
        if (game.onAIRespawn($DXAI::System::RuntimeDummy) != 11595)
        {
            error("DXAI: Function 'DefaultGame::onAIRespawn' detected to be overwritten by the current gamemode. Correcting ... ");
            
            eval(strReplace(%payloadTemplate, "<METHODNAME>", "onAIRespawn"));
            
            if (game.onAIRespawn($DXAI::System::RuntimeDummy) != 11595)
                error("DXAI: Failed to patch 'DefaultGame::onAIRespawn'! DXAI may not function correctly.");
        }
        
        $DXAI::System::InvalidatedEnvironment = false;
}

//------------------------------------------------------------------------------------------
// Description: This update function is scheduled to be called roughly once every 32
// milliseconds which updates each active commander in the game as well as performs
// an environment validation if necessary.
//
// NOTE: This is called on its own scheduled tick, therefore it should not be called
// directly.
//------------------------------------------------------------------------------------------
function DXAI::update()
{
    if (isEventPending($DXAI::updateHandle))
        cancel($DXAI::updateHandle);
    
    if (!isObject(Game))
        return;
    
    // Check if the bound functions are overwritten by the current gamemode, or if something
    // may have invalidated our hooks
    if ($DXAI::System::InvalidatedEnvironment && $DXAI::System::Setup)
        DXAI::validateEnvironment();
        
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
        $DXAI::ActiveCommander[%iteration].update();
    
    // Apparently we can't schedule a bound function otherwise
    $DXAI::updateHandle = schedule(32, 0, "eval", "DXAI::update();");
}

function DXAI::notifyPlayerDeath(%killed, %killedBy)
{
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
        $DXAI::ActiveCommander[%iteration].notifyPlayerDeath(%killed, %killedBy);
}

//------------------------------------------------------------------------------------------
// Description: There is a series of functions that the AI code can safely hook without
// worry of being overwritten implicitly such as the disconnect or exec functions. For
// those that can be, there is an environment validation that is performed to ensure that
// the necessary code will be called in response to the events we need to know about in
// this AI system.
//------------------------------------------------------------------------------------------
package DXAI_Hooks
{
    //------------------------------------------------------------------------------------------
    // Description: Called when the mission ends. We use this to perform any necessary cleanup
    // operations between games.
    //------------------------------------------------------------------------------------------
    function DefaultGame::gameOver(%game)
    {
        parent::gameOver(%game);
        
        DXAI::cleanup();
    }
    
    //------------------------------------------------------------------------------------------
    // Description: Called when the mission starts. We use this to perform initialization and
    // to start the update ticks.
    //------------------------------------------------------------------------------------------
    function DefaultGame::startMatch(%game)
    {
        parent::startMatch(%game);
        
        DXAI::setup(%game.numTeams);
        DXAI::update();
    }
    
    //------------------------------------------------------------------------------------------
    // Description: We hook the disconnect function as a step to fix console spam from leaving
    // a listen server due to the AI code continuing to run post-server shutdown in those
    // cases.
    //------------------------------------------------------------------------------------------
    function disconnect()
    {
        parent::disconnect();
        
        DXAI::cleanup();
    }
    
    //------------------------------------------------------------------------------------------
    // Description: In the game, bots can be made to change teams which means we need to hook
    // this event so that commander affiliations can be properly updated.
    //------------------------------------------------------------------------------------------
    function DefaultGame::AIChangeTeam(%game, %client, %newTeam)
    {
        // Remove us from the old commander's control first
        $DXAI::ActiveCommander[%client.team].removeBot(%client);
        
        parent::AIChangeTeam(%game, %client, %newTeam);
        
        $DXAI::ActiveCommander[%newTeam].addBot(%client);
    }
    
    //------------------------------------------------------------------------------------------
    // Description: In the game, bots can be kicked like regular players so we hook this to
    // ensure that commanders are properly notified of lesser bot counts.
    //------------------------------------------------------------------------------------------
    function AIConnection::onAIDrop(%client)
    {
        if (isObject(%client.commander))
            %client.commander.removeBot(%client);
            
        parent::onAIDrop(%client);
    }
    
    // Hooks for AI System notification
    function DefaultGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation)
    {
        parent::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);
        
        DXAI::notifyPlayerDeath(%clVictim, %clKiller);
    }
    
    function DefaultGame::onAIKilled(%game, %clVictim, %clKiller, %damageType, %implement)
    {
        parent::onAIKilled(%game, %clVictim, %clKiller, %damageType, %implement);
        
        DXAI::notifyPlayerDeath(%clVictim, %clKiller);
    }
    
    //------------------------------------------------------------------------------------------
    // Description: We hook this function to implement some basic sound simulation for bots.
    // This means that if something explodes, a bot will hear it and if the sound is close
    // enough, they will shimmy away from the source using setDangerLocation.
    //------------------------------------------------------------------------------------------
    function ProjectileData::onExplode(%data, %proj, %pos, %mod)
    {
        parent::onExplode(%data, %proj, %pos, %mod);
        
        // Look for any bots nearby
        InitContainerRadiusSearch(%pos, 100, $TypeMasks::PlayerObjectType);
        
        while ((%targetObject = containerSearchNext()) != 0)
        {
            %currentDistance = containerSearchCurrRadDamageDist();
            
            if (%currentDistance > 100 || !%targetObject.client.isAIControlled())
                continue;
                  
            // Get the projectile team
            %projectileTeam = -1;
            if (isObject(%proj.sourceObject))
                %projectileTeam = %proj.sourceObject.client.team;
                
            // Determine if we should run based on team & Team damage
            %shouldRun = false;
            if (isObject(%proj.sourceObject) && %projectileTeam == %targetObject.client.team && $TeamDamage)
                %shouldRun = true;
            else if (isObject(%proj.sourceObject) && %projectileTeam != %targetObject.client.team)
                %shouldRun = true;
                
            // Determine if we 'heard' it. The sound distance seems to be roughly 55m or less and we check the maxDistance
            // IIRC The 55m distance should scale with the min/max distances and volume but I'm not sure how those interact
            %heardHit = false;
            %hitDistance = vectorDist(%targetObject.getWorldBoxCenter(), %pos);
            
            if (%hitDistance <= 20 && %hitDistance <= %data.explosion.soundProfile.description.maxDistance)
                %heardHit = true;
                
            // If the thing has any radius damage (and we heard it), run around a little bit if we need to, and look at it for a bit
            if (%data.indirectDamage != 0 && %heardHit)
            {
                %targetObject.client.schedule(getRandom(250, 400), "setDangerLocation", %pos, 20);
                // TODO: Perhaps attempt to discern the direction of fire?
                %targetObject.client.aimAt(%pos);
            }
            
            // If we should care and it wasn't a teammate projectile, notify
            if (%shouldRun && %projectileTeam != %targetObject.client.team)
                %targetObject.client.notifyProjectileImpact(%data, %proj, %pos);
        }
    }
    
    //------------------------------------------------------------------------------------------
    // Description: This function is hooked so that we can try and guarantee that the DXAI 
    // gamemode hooks still exist in the runtime as game mode scripts are executed for each
    // mission load.
    //------------------------------------------------------------------------------------------
    function CreateServer(%mission, %missionType)
    {   
        // Perform the default exec's
        parent::CreateServer(%mission, %missionType);
        
        // Ensure that the DXAI is active.
        DXAI::validateEnvironment();
    }
    
    // Make this do nothing so the bots don't ever get any objectives by default
    function DefaultGame::AIChooseGameObjective(%game, %client) { return 11595; }
    
    function DefaultGame::onAIRespawn(%game, %client)
    {
        // Make sure the bot has no objectives
       // %client.reset();
      //  %client.defaultTasksAdded = true;
        %client.shouldRearm = true;
        %client.targetLoadout = 1;
        
        %client.engageTargetLastPosition = "";
        %client.engageTarget = -1;
        
        return 11595;
    }
    
    // We package hook the exec() and compile() functions to perform execution environment
    // checking because these can easily load code that overwrites methods that are otherwise
    // hooked by DXAI. This can happen with gamemode specific events because DXAI only hooks into
    // DefaultGame. This is mostly helpful for developers.
    function exec(%file)
    {
        $DXAI::System::InvalidatedEnvironment = true;
        parent::exec(%file);
    }
    
    function compile(%file)
    {
        $DXAI::System::InvalidatedEnvironment = true;
        parent::compile(%file);
    }
    
    function AIRespondToEvent(%client, %eventTag, %targetClient)
    {
        %clientPos = %client.player.getWorldBoxCenter();
        //switch$ (%eventTag)
        //{
        schedule(250, %targetClient, "AIPlayAnimSound", %targetClient, %clientPos, "cmd.decline", $AIAnimSalute, $AIAnimSalute, 0);
        schedule(2000, %targetClient, "AIPlayAnimSound", %targetClient, %clientPos, ObjectiveNameToVoice(%targetClient), $AIAnimSalute, $AIAnimSalute, 0);
        schedule(3700, %targetClient, "AIPlayAnimSound", %targetClient, %clientPos,  "vqk.sorry", $AIAnimSalute, $AIAnimSalute, 0);
    }
    
    function AISystemEnabled(%enabled)
    {
        parent::AISystemEnabled(%enabled);
        
        echo(%enabled);
        $DXAI::AISystemEnabled = %enabled;
    }
    
    function AIConnection::onAIDrop(%client)
    {
        parent::onAIDrop(%client);
        
        if (isObject(%client.visibleHostiles))
            %client.visibleHostiles.delete();
    }
    
    function Station::stationTriggered(%data, %obj, %isTriggered)
    {
      parent::stationTriggered(%data, %obj, %isTriggered);

      // TODO: If the bot isn't supposed to be on the station, at least restock ammunition?
      if (%isTriggered && %obj.triggeredBy.client.isAIControlled() && %obj.triggeredBy.client.shouldRearm)
      {
        %bot = %obj.triggeredBy.client;
          
        %bot.shouldRearm = false;
        %bot.player.clearInventory();
        
        %bot.player.setArmor($DXAI::Loadouts[%bot.targetLoadout, "Armor"]);
        %bot.player.setInventory($DXAI::Loadouts[%bot.targetLoadout, "Pack"], 1, true);
        
        for (%iteration = 0; %iteration < $DXAI::Loadouts[%bot.targetLoadout, "WeaponCount"]; %iteration++)
        {
          %bot.player.setInventory($DXAI::Loadouts[%bot.targetLoadout, "Weapon", %iteration], 1, true);
          %bot.player.setInventory($DXAI::Loadouts[%bot.targetLoadout, "Weapon", %iteration].Image.Ammo, 999, true); // TODO: Make it actually top out correctly!
        }
      }
    }
};

// Only activate the package if it isn't already active.
if (!isActivePackage(DXAI_Hooks))
    activatePackage(DXAI_Hooks);

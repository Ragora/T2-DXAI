// DXAI_Main.cs
// Experimental AI System for ProjectR3
// Copyright (c) 2014 Robert MacGregor

exec("scripts/Server/DXAI_Objectives.cs");
exec("scripts/Server/DXAI_Helpers.cs");
exec("scripts/Server/DXAI_Config.cs");

$DXAI::ActiveCommanderCount = 2;

// AICommander
// This is a script object that exists for every team in a given
// gamemode and performs the coordination of bots in the game.

function AICommander::notifyPlayerDeath(%this, %killed, %killedBy)
{
}

function AICommander::setup(%this)
{    
    %this.botList = new Simset();
    %this.idleBotList = new Simset();
    
    for (%iteration = 0; %iteration < ClientGroup.getCount(); %iteration++)
    {
        %currentClient = ClientGroup.getObject(%iteration);
        
        if (%currentClient.team == %this.team && %currentClient.isAIControlled())
        {
            %this.botList.add(%currentClient);
            %this.idleBotList.add(%currentClient);
            
            %currentClient.commander = %this;
        }
    }
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

function AICommander::cleanup(%this)
{
    %this.botList.delete();
    %this.idleBotList.delete();
}

function AICommander::update(%this)
{
    for (%iteration = 0; %iteration < %this.botList.getCount(); %iteration++)
        %this.botList.getObject(%iteration).update();
}

// General DXAI API implementations
function DXAI::cleanup()
{
    $DXAI::System::Setup = false;
    
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
    {
        $DXAI::ActiveCommander[%iteration].cleanup();
        $DXAI::ActiveCommander[%iteration].delete();
    }
    
    $DXAI::ActiveCommanderCount = 0;
}

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

function DXAI::update()
{
    if (isEventPending($DXAI::updateHandle))
        cancel($DXAI::updateHandle);
    
    // Check if the bound functions are overwritten by the current gamemode, or if something
    // may have invalidated our hooks
    if ($DXAI::System::InvalidatedEnvironment && $DXAI::System::Setup)
        DXAI::validateEnvironment();
        
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
        $DXAI::ActiveCommander[%iteration].update();
    
    // Apparently we can't schedule a bound function otherwise
    $DXAI::updateHandle = schedule(32,0,"eval", "DXAI::update();");
}

function DXAI::notifyPlayerDeath(%killed, %killedBy)
{
    for (%iteration = 1; %iteration < $DXAI::ActiveCommanderCount + 1; %iteration++)
        $DXAI::ActiveCommander[%iteration].notifyPlayerDeath(%killed, %killedBy);
}

// AIPlayer
// This is a script object that contains DXAI functionality on a per-soldier
// basis
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


// Hooks for the AI System
package DXAI_Hooks
{
    function DefaultGame::gameOver(%game)
    {
        parent::gameOver(%game);
        
        DXAI::cleanup();
    }
    
    function DefaultGame::startMatch(%game)
    {
        parent::startMatch(%game);
        
        DXAI::setup(%game.numTeams);
        DXAI::update();
    }
    
    // Listen server fix
    function disconnect()
    {
        parent::disconnect();
        
        DXAI::Cleanup();
    }
    
    function DefaultGame::AIChangeTeam(%game, %client, %newTeam)
    {
        // Remove us from the old commander's control first
        $DXAI::ActiveCommander[%client.team].removeBot(%client);
        
        parent::AIChangeTeam(%game, %client, %newTeam);
        
        $DXAI::ActiveCommander[%newTeam].addBot(%client);
    }
    
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
                
            // If the thing has any radius damage (and we heard it), run around a little bit if we need to
            if (%data.indirectDamage != 0 && %heardHit)
                %targetObject.client.schedule(getRandom(250, 400), "setDangerLocation", %pos, 20);
            
            // If we should care and it wasn't a teammate projectile, notify
            if (%shouldRun && %projectileTeam != %targetObject.client.team)
                %targetObject.client.notifyProjectileImpact(%data, %proj, %pos);
        }
    }
    
    function CreateServer(%mission, %missionType)
    {   
        // Perform the default exec's
        parent::CreateServer(%mission, %missionType);
        
        // Ensure that the DXAI is active.
        $DXAI::System::InvalidatedEnvironment = true;
    }
    
    // Make this do nothing so the bots don't ever get any objectives by default
    function DefaultGame::AIChooseGameObjective(%game, %client) { return 11595; }
    
    function DefaultGame::onAIRespawn(%game, %client)
    {
        // Make sure the bot has no objectives
       // %client.reset();
      //  %client.defaultTasksAdded = true;
        
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
};

if (!isActivePackage(DXAI_Hooks))
    activatePackage(DXAI_Hooks);

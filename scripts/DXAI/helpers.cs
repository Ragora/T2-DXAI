// helpers.cs
// Helper functions for the AI System
// Copyright (c) 2014 Robert MacGregor

function sameSide(%p1, %p2, %a, %b)
{
    %cp1 = vectorCross(vectorSub(%b, %a), vectorSub(%p1, %a));
    %cp2 = vectorCross(vectorSub(%b, %a), vectorSub(%p2, %a));
    
    if (vectorDot(%cp1, %cp2) >= 0)
        return true;
        
    return false;
}

function SimSet::contains(%this, %contained)
{
    for (%iteration = 0; %iteration < %this.getCount(); %iteration++)
        if (%this.getObject(%iteration) == %contained)
            return true;
    return false;
}

function pointInTriangle(%point, %a, %b, %c)
{
    if (sameSide(%point, %a, %b, %c) && sameSide(%point, %b, %a, %c) && sameSide(%point, %c, %a, %b))
        return true;
    
    return false;
}

function AIConnection::reset(%this)
{
    AIUnassignClient(%this);
    
    %this.stop();
    %this.clearTasks();
    %this.clearStep();
    %this.lastDamageClient = -1;
    %this.lastDamageTurret = -1;
    %this.shouldEngage = -1;
    %this.setEngageTarget(-1);
    %this.setTargetObject(-1);
    %this.pilotVehicle = false;
    %this.defaultTasksAdded = false;
    
    if (isObject(%this.controlByHuman))
        aiReleaseHumanControl(%this.controlByHuman, %this);
}

// TODO: Return in a faster-to-read format: Could try as static GVar names
// as the game's scripting environment for the gameplay is single threaded
// and it probably does a hash to store the values.
// TODO: Mathematical optimizations, right now it's a hack because of no
// reliable way of getting a player's X facing?
function GameConnection::calculateViewCone(%this, %distance)
{
    //%xFacing = %this.player.getXFacing();
    %halfView = %this.fieldOfView / 2;
    %coneOrigin = %this.player.getMuzzlePoint($WeaponSlot);
    
    %forwardVector = %this.player.getForwardVector();
    %sideVector = vectorCross("0 0 1", %forwardVector);
    
    // Clockwise
    //%viewConeClockwise = %xFacing - %halfView;
    
   // %viewConeClockwisePoint = mCos(%viewConeClockwise) SPC mSin(%viewConeClockwise) SPC "0";
    %viewConeClockwisePoint = mCos(-%halfView) SPC mSin(-%halfView) SPC "0";
    %viewConeClockwisePoint = vectorScale(%viewConeClockwisePoint, %this.viewDistance);
    //%viewConeClockwisePoint = vectorAdd(%viewConeClockwisePoint, %coneOrigin);
    
    // Counter Clockwise
    //%viewConeCounterClockwise = %xFacing + %halfView;
    
    //%viewConeCounterClockwisePoint = mCos(%viewConeCounterClockwise) SPC mSin(%viewConeCounterClockwise) SPC "0";
    %viewConeCounterClockwisePoint = mCos(%halfView) SPC mSin(%halfView) SPC "0";
    %viewConeCounterClockwisePoint = vectorScale(%viewConeCounterClockwisePoint, %this.viewDistance);
    //%viewConeCounterClockwisePoint = vectorAdd(%viewConeCounterClockwisePoint, %coneOrigin);
    
    // Offsets    
    %halfDistance = vectorDist(%viewConeCounterClockwisePoint, %viewConeClockwisePoint) / 2;
    
    %viewConeCounterClockwisePoint = vectorScale(%sideVector, %halfDistance);
    %viewConeCounterClockwisePoint = vectorAdd(%coneOrigin, %viewConeCounterClockwisePoint);
    
    %viewConeClockwisePoint = vectorScale(vectorScale(%sideVector, -1), %halfDistance);
    %viewConeClockwisePoint = vectorAdd(%coneOrigin, %viewConeClockwisePoint);
    
    // Translate the upper and lower points
    %viewForwardPoint = vectorScale(%forwardVector, %this.viewDistance);
    
    %viewConeUpperPoint = vectorAdd(vectorScale("0 0 1", %halfDistance), %viewForwardPoint);
    %viewConeUpperPoint = vectorAdd(%coneOrigin, %viewConeUpperPoint);
    
    %viewConeLowerPoint = vectorAdd(vectorScale("0 0 -1", %halfDistance), %viewForwardPoint);
    %viewConeLowerPoint = vectorAdd(%coneOrigin, %viewConeLowerPoint);
    
    // Now cast them forward
    %viewConeClockwisePoint = vectorAdd(%viewConeClockwisePoint, vectorScale(%this.player.getForwardVector(), %this.viewDistance));
    %viewConeCounterClockwisePoint = vectorAdd(%viewConeCounterClockwisePoint, vectorScale(%this.player.getForwardVector(), %this.viewDistance));

    return %coneOrigin SPC %viewConeClockwisePoint SPC %viewConeCounterClockwisePoint SPC %viewConeUpperPoint SPC %viewConeLowerPoint;
}

// View cone simulation function
function GameConnection::getObjectsInViewcone(%this, %typeMask, %distance, %performLOSTest)
{
    // FIXME: Radians
    if (%this.fieldOfView < 0 || %this.fieldOfView > 3.14)
    {
        %this.fieldOfView = $DXAPI::Bot::DefaultFieldOfView;
        error("DXAI: Bad field of view value! (" @ %this @ ".fieldOfView > 3.14 || " @ %this @ ".fieldOfView < 0)");
    }
    
    if (%this.viewDistance <= 0)
    {
        %this.viewDistance = $DXAPI::Bot::DefaultViewDistance;
        error("DXAI: Bad view distance value! (" @ %this @ ".viewDistance <= 0)");
    }
    
    if (%distance $= "")
        %distance = %this.viewDistance;
        
    %viewCone = %this.calculateViewCone(%distance);
    
    // Extract the results: See TODO above ::calculateViewCone implementation
    %coneOrigin = getWords(%viewCone, 0, 2);
    %viewConeClockwiseVector = getWords(%viewCone, 3, 5);
    %viewConeCounterClockwiseVector = getWords(%viewCone, 6, 8);
    %viewConeUpperVector = getWords(%viewCone, 9, 11);
    %viewConeLowerVector = getWords(%viewCone, 12, 14);
    
    %result = new SimSet();
    
    // Doing a radius search should hopefully be faster than iterating over all objects in MissionCleanup.
    // Even if the game did that internally it's definitely faster than doing it in TS
    InitContainerRadiusSearch(%coneOrigin, %distance, %typeMask);
    while((%currentObject = containerSearchNext()) != 0)
    {
        if (%currentObject == %this || !isObject(%currentObject) || containerSearchCurrRadDamageDist() > %distance)
            continue;

        // Check if the object is within both the horizontal and vertical triangles representing our view cone
        if (%currentObject.getType() & %typeMask && pointInTriangle(%currentObject.getPosition(), %viewConeClockwiseVector, %viewConeCounterClockwiseVector, %coneOrigin) && pointInTriangle(%currentObject.getPosition(), %viewConeLowerVector, %viewConeUpperVector, %coneOrigin))
        {
            if (!%performLOSTest)
                %result.add(%currentObject);
            else
            {
                %rayCast = containerRayCast(%coneOrigin, %currentObject.getWorldBoxCenter(), -1, 0);
                
                %hitObject = getWord(%raycast, 0);
                
                // Since the engine doesn't do raycasts against projectiles correctly, we just check if the bot
                // hit -nothing- when doing the raycast rather than checking for a hit against the object
                if (%hitObject == %currentObject || (%currentObject.getType() & $TypeMasks::ProjectileObjectType && !isObject(%hitObject)))
                    %result.add(%currentObject);
            }
        }
    }
    
    return %result;
}

function getRandomPosition(%position, %distance)
{
    // First, we determine a random direction vector
    %direction = vectorNormalize(getRandom(0, 10000) SPC getRandom(0, 10000) SPC getRandom(0, 10000));
    // Return the scaled result
    return vectorAdd(%position, vectorScale(%direction, getRandom(0, %distance)));
}

function getRandomPositionOnTerrain(%position, %distance)
{
    %result = getRandomPosition(%position, %distance);
    return setWord(%result, 2, getTerrainHeight(%result));
}

function vectorMultiply(%vec1, %vec2)
{
    return (getWord(%vec1, 0) * getWord(%vec2, 0)) SPC 
    (getWord(%vec1, 1) * getWord(%vec2, 1)) SPC 
    (getWord(%vec1, 2) * getWord(%vec2, 2));
}

// If the map editor was instantiated, this will prevent a little bit
// of console warnings
function Terraformer::getType(%this) { return 0; }

// Dummy ScriptObject methods to silence console warnings when testing the runtime
// environment; this may not silence for all mods but it should help.
$DXAI::System::RuntimeDummy = new ScriptObject(RuntimeDummy) { class = "RuntimeDummy"; };

function RuntimeDummy::addTask() { }
function RuntimeDummy::reset() { }

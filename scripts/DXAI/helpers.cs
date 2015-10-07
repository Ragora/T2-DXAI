//------------------------------------------------------------------------------------------
// helpers.cs
// Helper functions used in the experimental DXAI system.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. 
// Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

function sameSide(%p1, %p2, %a, %b)
{
    %cp1 = vectorCross(vectorSub(%b, %a), vectorSub(%p1, %a));
    %cp2 = vectorCross(vectorSub(%b, %a), vectorSub(%p2, %a));
    
    if (vectorDot(%cp1, %cp2) >= 0)
        return true;
        
    return false;
}

//------------------------------------------------------------------------------------------
// Description: Returns whether or not the given point resides inside of the triangle
// denoted by points %a, %b and %c.
// Param %point: The point to test.
// Param %a: One point of the triangle.
// Param %b: One point of the triangle.
// Param %c: One point of the triangle.
// Return: A boolean representing whether or not the given point resides inside of the 
// triangle.
//------------------------------------------------------------------------------------------
function pointInTriangle(%point, %a, %b, %c)
{
    if (sameSide(%point, %a, %b, %c) && sameSide(%point, %b, %a, %c) && sameSide(%point, %c, %a, %b))
        return true;
    
    return false;
}

//------------------------------------------------------------------------------------------
// Description: Calculates all the points of the given client's view cone given a maximum
// view distance and returns them in a long string.
// Param %distance: The distance of their view cone.
// Return: A string in the following format:
// "OriginX OriginY OriginZ Outer1X Outer1Y Outer1Z Outer2X Outer2Y Outer2Z UpperX UpperY UpperZ
// LowerX LowerY LowerZ"
//
// TODO: Return in a faster-to-read format: Could try as static GVar names
// as the game's scripting environment for the gameplay is single threaded
// and it probably does a hash to store the values.
// FIXME: Mathematical optimizations, right now it's a hack because of no
// reliable way of getting a player's X facing?
//------------------------------------------------------------------------------------------
function GameConnection::calculateViewCone(%this, %distance)
{
    if (!isObject(%this.player) || %this.player.getState() !$= "Move")
        return -1;
        
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

//------------------------------------------------------------------------------------------
// Description: Returns a SimSet of all contained object ID's inside of the given SimSet,
// including those in child SimGroup and SimSet instances.
// Return: The ID of the SimSet that contains all child objects that are not containers
// themselves.
//------------------------------------------------------------------------------------------
function SimSet::recurse(%this, %result)
{
  if (!isObject(%result))
    %result = new SimSet();
    
  for (%iteration = 0; %iteration < %this.getCount(); %iteration++)
  {
    %current = %this.getObject(%iteration);
    
    if (%current.getClassName() $= "SimGroup" || %current.getClassName() $= "SimSet")
      %current.recurse(%result);
    else
      %result.add(%current);
  }
  
  return %result;
}

//------------------------------------------------------------------------------------------
// Description: Returns the closest friendly inventory station to the given client.
// Return: The object ID of the inventory station determined to be the closest to this
// client.
//
// TODO: Use the nav graph to estimate an actual distance?
// FIXME: Return *working* stations only.
//------------------------------------------------------------------------------------------
function GameConnection::getClosestInventory(%this)
{
  if (!isObject(%this.player))
    return -1;
    
  %group = nameToID("Team" @ %this.team);
  if (!isObject(%group))
    return -1;
  
  %teamObjects = %group.recurse();
  
  %closestInventory = -1;
  %closestInventoryDistance = 9999;
  for (%iteration = 0; %iteration < %teamObjects.getCount(); %iteration++)
  {
    %current = %teamObjects.getObject(%iteration);
    
    if (%current.getClassName() $= "StaticShape" && %current.getDatablock().getName() $= "StationInventory")
    {
      %inventoryDistance = vectorDist(%current.getPosition(), %this.player.getPosition());
      
      if (%inventoryDistance < %closestInventoryDistance)
      {
        %closestInventoryDistance = %inventoryDistance;
        %closestInventory = %current;
      }
    }
  }
  
  %teamObjects.delete();
  
  return %closestInventory;
}

//------------------------------------------------------------------------------------------
// Description: Calculates a list of objects that can be seen by the given client using
// distance & field of view values passed in for evaluation.
// Param %typeMask: The typemask of all objects to consider.
// Param %distance: The maximum distance to project our view cone checks out to.
// Param %performLOSTest: A boolean representing whether or not found objects should be
// verified using a raycast test. If you cannot draw a line from the player to the potential
// target, then it the potential target is discarded.
// Return: A SimSet of objects that can be seen by the given client.
//------------------------------------------------------------------------------------------
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
                %rayCast = containerRayCast(%coneOrigin, %currentObject.getWorldBoxCenter(), $TypeMasks::AllObjectType, %this.player);
                
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

//------------------------------------------------------------------------------------------
// Description: Gets a random position somewhere within %distance of the given position.
// Param %position: The position to generate a new position around.
// Param %distance: The maximum distance the new position may be 
//------------------------------------------------------------------------------------------
function getRandomPosition(%position, %distance, %raycast)
{
    // First, we determine a random direction vector
    %direction = vectorNormalize(getRandom(0, 10000) SPC getRandom(0, 10000) SPC getRandom(0, 10000));
    
    // Return the scaled result
    %result = vectorAdd(%position, vectorScale(%direction, getRandom(0, %distance)));
    
    if (!%raycast)
        return %result;
    
    %rayCast = containerRayCast(%position, %result, $TypeMasks::AllObjectType, 0);
    %result = getWords(%raycast, 1, 3);
    
    return %result;
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

function listStuckBots()
{
    for (%iteration = 0; %iteration < ClientGroup.getCount(); %iteration++)
    {
        %client = ClientGroup.getObject(%iteration);
        if (%client.isAIControlled() && %client.isPathCorrecting)
            error(%client);
    }
}

// If the map editor was instantiated, this will prevent a little bit
// of console warnings
function Terraformer::getType(%this) { return 0; }

// Dummy ScriptObject methods to silence console warnings when testing the runtime
// environment; this may not silence for all mods but it should help.
$DXAI::System::RuntimeDummy = new ScriptObject(RuntimeDummy) { class = "RuntimeDummy"; };

function RuntimeDummy::addTask() { }
function RuntimeDummy::reset() { }

$TypeMasks::InteractiveObjectType = $TypeMasks::PlayerObjectType | $TypeMasks::VehicleObjectType | $TypeMasks::WaterObjectType | $TypeMasks::ProjectileObjectType | $TypeMasks::ItemObjectType | $TypeMasks::CorpseObjectType;
$TypeMasks::UnInteractiveObjectType = $TypeMasks::StaticObjectType | $TypeMasks::TerrainObjectType | $TypeMasks::InteriorObjectType | $TypeMasks::StaticTSObjectType | $TypeMasks::StaticRenderedObjectType;
$TypeMasks::BaseAssetObjectType = $TypeMasks::ForceFieldObjectType | $TypeMasks::TurretObjectType | $TypeMasks::SensorObjectType | $TypeMasks::StationObjectType | $TypeMasks::GeneratorObjectType;
$TypeMasks::GameSupportObjectType = $TypeMasks::TriggerObjectType | $TypeMasks::MarkerObjectType | $TypeMasks::CameraObjectType | $TypeMasks::VehicleBlockerObjectType | $TypeMasks::PhysicalZoneObjectType;
$TypeMasks::GameContentObjectType = $TypeMasks::ExplosionObjectType | $TypeMasks::CorpseObjectType | $TypeMasks::DebrisObjectType;
$TypeMasks::DefaultLOSObjectType = $TypeMasks::TerrainObjectType | $TypeMasks::InteriorObjectType | $TypeMasks::StaticObjectType;
$TypeMasks::AllObjectType = -1;

// DXAI_Objectives.cs
// Objectives for the AI system
// Copyright (c) 2014 Robert MacGregor

//----------------------------------------------------------------------
// The AIVisualAcuity task is a complementary task for the AI grunt systems
// to perform better at recognizing things visually with reasonably
// Human perception capabilities.
// ---------------------------------------------------------------------

function AIVisualAcuity::initFromObjective(%task, %objective, %client)
{
    // Called to initialize from an objective object
}

function AIVisualAcuity::assume(%task, %client)
{
    // Called when the bot starts the task
}

function AIVisualAcuity::retire(%task, %client)
{
    // Called when the bot stops the task
}

function AIVisualAcuity::weight(%task, %client)
{
    %task.setWeight(999);
}

function AIVisualAcuity::monitor(%task, %client)
{
    // Called when the bot is performing the task
    
    if (%client.enableVisualDebug)
    {
        if (!isObject(%client.originMarker))
        {
            %client.originMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Origin"; };
            %client.clockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Clockwise"; };
            %client.counterClockwiseMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Counter Clockwise"; };
            %client.upperMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Upper"; };
            %client.lowerMarker = new Waypoint(){ datablock = "WaypointMarker"; team = %client.team; name = %client.namebase SPC " Lower"; };
        }
            
        %viewCone = %client.calculateViewCone();
        %coneOrigin = getWords(%viewCone, 0, 2);
        %viewConeClockwiseVector = getWords(%viewCone, 3, 5);
        %viewConeCounterClockwiseVector = getWords(%viewCone, 6, 8);
        
        %viewConeUpperVector = getWords(%viewCone, 9, 11);
        %viewConeLowerVector = getWords(%viewCone, 12, 14);
        
        // Update all the markers
        %client.clockwiseMarker.setPosition(%viewConeClockwiseVector);
        %client.counterClockwiseMarker.setPosition(%viewConeCounterClockwiseVector);
        %client.upperMarker.setPosition(%viewConeUpperVector);
        %client.lowerMarker.setPosition(%viewConeLowerVector);
        %client.originMarker.setPosition(%coneOrigin);
    }
    else if (isObject(%client.originMarker))
    {
        %client.originMarker.delete();
        %client.clockwiseMarker.delete();
        %client.counterClockwiseMarker.delete();
        %client.upperMarker.delete();
        %client.lowerMarker.delete();
    }
    
    %result = %client.getObjectsInViewcone($TypeMasks::ProjectileObjectType | $TypeMasks::PlayerObjectType, %client.viewDistance, true);
    
    echo(%result.getCount());
    
    %result.delete();
}

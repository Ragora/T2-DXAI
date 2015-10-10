//------------------------------------------------------------------------------------------
// cyclicset.cs
// Main source file for the CyclicSet implementation. A CyclicSet is simply a set of objects
// that is cycled through from start to finish before looping back to start.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: Adds an object to the cyclic set.
// Param %object: The object to be added to the cyclic set.
//------------------------------------------------------------------------------------------
function CyclicSet::add(%this, %object)
{
    %this.set.add(%object);
}

//------------------------------------------------------------------------------------------
// Description: An overrided implementation of the .delete() function that will properly
// cleanup the cyclic set before deleting itself proper.
//------------------------------------------------------------------------------------------
function CyclicSet::delete(%this)
{
    %this.set.delete();
    ScriptObject::delete(%this);
}

//------------------------------------------------------------------------------------------
// Description: Clears the cyclic set of all objects of which none are deleted.
//------------------------------------------------------------------------------------------
function CyclicSet::clear(%this)
{
    %this.index = 0;
    %this.set.clear();
}

//------------------------------------------------------------------------------------------
// Description: Gets the next object in the cyclic set.
// Return: The next object ID in the cyclic set.
//------------------------------------------------------------------------------------------
function CyclicSet::next(%this)
{
    if (%this.set.getCount() == 0)
        return -1;
    
    %result = %this.set.getObject(%this.index);
    
    %this.index++;
    %this.index %= %this.set.getCount();
    
    return %result;
}

//------------------------------------------------------------------------------------------
// Description: Randomizes the index that the cyclic set is currently at.
//------------------------------------------------------------------------------------------
function CyclicSet::randomizeIndex(%this)
{
    %this.index = getRandom(0, %this.set.getCount());
}

//------------------------------------------------------------------------------------------
// Description: Creates a new cyclic set with the given name.
// Param %name: The name to give to the new cyclic set.
// Param %container: If specified, the cyclic set will copy data contained in the given
// container into itself. This container be a SimGroup, a SimSet or another cyclic set.
// Return: The ID of the new cyclic set.
//
// Usage: %set = CyclicSet::create("MySet");
//------------------------------------------------------------------------------------------
function CyclicSet::create(%name, %container)
{
    %set = new SimSet();
    %result = new ScriptObject(%name)
    {
        index = 0;
        class = "CyclicSet";
        set = %set;
    };
    
    if (isObject(%container))
    {
        if (%container.class $= "CyclicSet")
            %container = %container.set;
            
        if (%container.getClassName() $= "SimSet" || %container.getClassName() $= "SimGroup")
            for (%iteration = 0; %iteration < %container.getCount(); %iteration++)
                %result.set.add(%container.getObject(%iteration));
    }
    
    return %result;
}
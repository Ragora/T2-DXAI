//------------------------------------------------------------------------------------------
// priorityqueue.cs
// Source file for the priority queue implementation.
// https://github.com/Ragora/T2-DXAI.git
//
// FIXME: Make the keys regular priorities so more than one value can occupy the same
// priority value.
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

//------------------------------------------------------------------------------------------
// Description: Adds a new value to the priority queue.
// Param %key: The key (or priority) to map to %value. This must be a numeric value that
// can be compared using the relational operators.
// Param %value: The value to map. This can be arbitrary data or object ID's as Torque
// script treats object ID's and regular numerics as the same thing until you try to
// actually use them as an object.
//------------------------------------------------------------------------------------------
function PriorityQueue::add(%this, %key, %value)
{
    // If we already have a key, update it
    if (%this.hasKey[%key])
    {
        %this.values[%this.keyIndex[%key]] = %value;
        return;
    }
        
    %this.hasKey[%key] = true;
    
    // Traverse the queue and discover our insertion point
    for (%iteration = 0; %iteration < %this.count; %iteration++)
        if (%key <= %this.keys[%iteration])
        {
            //%this.count++;
            %this._shift(%iteration, false);
            %this.values[%iteration] = %value;
            %this.keys[%iteration] = %key;
            %this.keyIndex[%key] = %iteration;
            %this.count++;
            
            return;
        }
    
    // If we never made an insertion, just stick our key and value at the end
    %this.values[%this.count] = %value;
    %this.keys[%this.count] = %key;
    %this.keyIndex[%key] = %this.count;
    %this.count++;
}

//------------------------------------------------------------------------------------------
// Description: Removes a value from the priority queue with the given key (priority).
// Param %key: The key (priority) to remove from the priority queue.
// Return: A boolean representing whether or not anything was actually removed.
//------------------------------------------------------------------------------------------
function PriorityQueue::remove(%this, %key)
{
    if (!%this.hasKey[%key])
        return false;
    
    %this.hasKey[%key] = false;
    %this._shift(%this.keyIndex[%key], true);
    %this.count--;
    return true;
}

//------------------------------------------------------------------------------------------
// Description: An internal function used by the priority queue to shift values around.
// Param %index: The index to start at.
// Param %isRemoval: A boolean representing whether or not this shift is supposed to be
// a removal.
// NOTE: This is an internal function and therefore should not be invoked directly.
//------------------------------------------------------------------------------------------
function PriorityQueue::_shift(%this, %index, %isRemoval)
{
    if (%isRemoval)
    {
        for (%iteration = %index; %iteration < %this.count; %iteration++)
        {
            %this.values[%iteration] = %this.values[%iteration + 1];
            %this.keys[%iteration] = %this.keys[%iteration + 1];
            
            %this.keyIndex[%this.keys[%iteration]] = %iteration;
        }
        
        return;
    }
        
    for (%iteration = %this.count; %iteration >= %index; %iteration--)
    {
        %this.values[%iteration] = %this.values[%iteration - 1];
        %this.keys[%iteration] = %this.keys[%iteration - 1];
        
        %this.keyIndex[%this.keys[%iteration]] = %iteration - 1;
    }
}

//------------------------------------------------------------------------------------------
// Description: Returns the value in this priority queue with the current highest known
// priority.
// Return: The current value with the highest known priority. This returns -1 in the event
// that the priority queue is empty. However, this may be a valid value in whatever is
// using the priority queue so the ::isEmpty function should be used.
//------------------------------------------------------------------------------------------
function PriorityQueue::topValue(%this)
{
    if (%this.count <= 0)
        return -1;
        
    return %this.values[%this.count - 1];
}

//------------------------------------------------------------------------------------------
// Description: Returns the highest key (priority)
// Return: The current value with the highest known key *priority. This returns -1 in the event
// that the priority queue is empty. However, this may be a valid value in whatever is
// using the priority queue so the ::isEmpty function should be used.
//------------------------------------------------------------------------------------------
function PriorityQueue::topKey(%this)
{
    if (%this.count <= 0)
        return -1;
        
    return %this.keys[%this.count - 1];
}

//------------------------------------------------------------------------------------------
// Description: Pops off the value with the current highest key (priority). This value
// is then no longer present in the priority queue.
//------------------------------------------------------------------------------------------
function PriorityQueue::pop(%this)
{
    if (%this.count == 0)
        return;
    
    %this.hasKey[%this.keys[%this.count]] = false;
    %this.count--;
}

//------------------------------------------------------------------------------------------
// Description: Makes the entire priority queue empty.
//------------------------------------------------------------------------------------------
function PriorityQueue::clear(%this)
{
    for (%iteration = 0; %iteration < %this.count; %iteration++)
        %this.hasKey[%this.keys[%iteration]] = false;
    
    %this.count = 0;
}

//------------------------------------------------------------------------------------------
// Description: Returns whether or not the priority queue is empty.
// Return: A boolean representing whether or not the priority queue is empty.
//------------------------------------------------------------------------------------------
function PriorityQueue::isEmpty(%this)
{
    return %this.count <= 0;
}

//------------------------------------------------------------------------------------------
// Description: Prints a mapping of key (priority) to their respective values to the console
// for debugging purposes. The format is such:
// Key (Priority) -> Mapped Value
//------------------------------------------------------------------------------------------
function PriorityQueue::dump(%this)
{
    for (%iteration = 0; %iteration < %this.count; %iteration++)
        echo(%iteration SPC %this.keys[%iteration] SPC "-> " @ %this.values[%iteration]);
}

//------------------------------------------------------------------------------------------
// Description: Creates a new priority queue with the given name and returns the ID of
// the new priority queue created.
// Param %name: The name of the new priority queue.
//
// Usage: %queue = PriorityQueue::create("MyQueue");
//------------------------------------------------------------------------------------------
function PriorityQueue::create(%name)
{
    %result = new ScriptObject(%name) 
    { 
        class = "PriorityQueue";
        count = 0;
    };
    
    return %result;
}
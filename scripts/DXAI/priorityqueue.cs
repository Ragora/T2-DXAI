//------------------------------------------------------------------------------------------
// priorityqueue.cs
// Source file for the priority queue implementation.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
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

function PriorityQueue::remove(%this, %key)
{
    if (!%this.hasKey[%key])
        return;
    
    %this.hasKey[%key] = false;
    %this._shift(%this.keyIndex[%key], true);
    %this.count--;
}

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

function PriorityQueue::topValue(%this)
{
    return %this.values[%this.count - 1];
}

function PriorityQueue::topKey(%this)
{
    return %this.keys[%this.count - 1];
}

function PriorityQueue::pop(%this)
{
    if (%this.count == 0)
        return;
    
    %this.hasKey[%this.keys[%this.count]] = false;
    %this.count--;
}

function PriorityQueue::clear(%this)
{
    for (%iteration = 0; %iteration < %this.count; %iteration++)
        %this.hasKey[%this.keys[%iteration]] = false;
    
    %this.count = 0;
}

function Priorityqueue::isEmpty(%this)
{
    return %this.count == 0;
}

function PriorityQueue::dump(%this)
{
    for (%iteration = 0; %iteration < %this.count; %iteration++)
        echo(%iteration SPC %this.keys[%iteration] SPC "-> " @ %this.values[%iteration]);
}

function PriorityQueue::create(%name)
{
    return new ScriptObject(%name) 
    { 
        class = "PriorityQueue";
        count = 0;
    };
}
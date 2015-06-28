//------------------------------------------------------------------------------------------
// cyclicset.cs
// Main source file for the CyclicSet implementation.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2014 Robert MacGregor
// This software is licensed under the MIT license. Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

function CyclicSet::add(%this, %item)
{
    %this.set.add(%item);
}

function CyclicSet::delete(%this)
{
    %this.set.delete();
    ScriptObject::delete(%this);
}

function CyclicSet::clear(%this)
{
    %this.index = 0;
    %this.set.clear();
}

function CyclicSet::next(%this)
{
    if (%this.set.getCount() == 0)
        return -1;
    
    %result = %this.set.getObject(%this.index);
    
    %this.index++;
    %this.index %= %this.set.getCount();
    
    return %result;
}

function CyclicSet::randomize(%this)
{
    %this.index = getRandom(0, %this.set.getCount());
}

function CyclicSet::create(%name)
{
    %set = new SimSet();
    return new ScriptObject(%name)
    {
        index = 0;
        class = "CyclicSet";
        set = %set;
    };
}
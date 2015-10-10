//------------------------------------------------------------------------------------------
// config.cs
// Configuration file for the experimental DXAI system.
// https://github.com/Ragora/T2-DXAI.git
//
// Copyright (c) 2015 Robert MacGregor
// This software is licensed under the MIT license. 
// Refer to LICENSE.txt for more information.
//------------------------------------------------------------------------------------------

$DXAI::Commander::MinimumFlagDefense = 1;
$DXAI::Commander::MinimumGeneratorDefense = 1;

// This is the default view angle that bots will use. Probably
// shouldn't be changed much as I've never seen any mod ever that
// actually changed player FOV as part of its gameplay short of
// zooming.
$DXAI::Bot::DefaultFieldOfView = 3.14159 / 2;

// This is the default view distance that bots will be able to see for.
// This isn't necessarily the view distance they will use, as its more 
// or less going to be deprecated in favor of one calculated from the
// map fog.
$DXAI::Bot::DefaultViewDistance = 300;

// This is the minimum scheduled time in milliseconds that the AI visual 
// acuity code will run at. This is more of a setting to tweak performance
// as the associated visual acuity code will perform its own "perceptual"
// chekcs to ensure that bot reaction times are roughly equivalent to that
// of a Human.
$DXAI::Bot::MinimumVisualAcuityTime = 200;

// This is the maximum scheduled time in milliseconds that the AI visual 
// acuity code will run at. This is more of a setting to tweak performance
// as the associated visual acuity code will perform its own "perceptual"
// chekcs to ensure that bot reaction times are roughly equivalent to that
// of a Human.
$DXAI::Bot::MaximumVisualAcuityTime = 400;

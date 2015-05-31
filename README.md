Tribes 2 AI Enhancement
=======

This is an attempt to enhance the capabilities of the Tribes 2 artificial intelligence system. It attempts to work
out several quirks in the game's AI implementation:

   * Bots are assigned tasks randomly on a per-spawn basis.
   * Bots usually end up ignoring hostiles within their line of sight (LOS), unless they linger.
   * Bots ignore nearby audible explosions and projectiles crossing into their LOS altogether.
   * If a bot is struck, they instantly know where the attacker is.

Taking all that, the solution that was deviced was to create a commander meta-AI per team that assigned the tasks based on the necessity of a given task to be completed. This commander would balance out the number of available bots across all known tasks using a dynamic weighting system, similar to how at the bot-level there is weighted tasks to determine what the bot will do and when. If the base defenses have fallen, then the weight for restoring the defense and defending critical assets will rise and the AI Commander will pull bots performing auxilliary (non-essential) tasks and put them onto defending against the onslaught. 

At the bot level, there is going to be several changes in how they operate. The most notable one is the implementation of a view cone to check anything in their LOS. This LOS check is also used to determine if they can see a projectile headed right at them. This projectile LOS allows them to dodge incoming projectiles, which the AI has never done previously. Further, sound stimulation is simulated when things explode nearby, which can cause the bot to danger-step away from the source of the sound.

All of this in theory should create a more challenging computer controlled foe that is also a bit more fair in terms of what they know and when.

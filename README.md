Tribes 2 AI Enhancement
=======

Status: **Work in Progress**

This is an attempt to enhance the capabilities of the Tribes 2 artificial intelligence system. It attempts to work
out several quirks in the game's AI implementation:

   * Bots are assigned tasks randomly on a per-spawn basis.
   * Bots usually end up ignoring hostiles within their line of sight (LOS), unless they linger.
   * Bots ignore nearby audible explosions and projectiles crossing into their LOS altogether.
   * If a bot is struck, they instantly know where the attacker is.

Taking all that, the solution that was devised was to create a commander meta-AI per team that assigned the tasks based on the necessity of a given task to be completed and greatly improve the per-bot logic to have better target engagement, weapon selection & firing logic as well as an LOS that actually works. All of this in theory should create a more challengingcomputer controlled foe that is also a bit more fair in terms of what they know and when.

Commander Behavior
=======

This commander would balance out the number of available bots across all known tasks using a dynamic weighting system, similar to how at the bot-level there is weighted tasks to determine what the bot will 
do and when. If the base defenses have fallen, then the weight for restoring the defense and defending critical assets will rise and the AI Commander will pull bots performing auxiliary (non-essential) tasks and put them onto defending against the onslaught. 

Planned and implemented Features:

- [x] Commanders ensure deterministic task assignment to bots, guaranteeing sane assignments.
- [ ] Commanders adjust priorities in response to mission events and adjust bot task allocations accordingly.
- [ ] Commanders decide whether or not to give Human control over a bot, particularly lax with idle bots.
- [ ] Commanders ditch prioritization in emergency situations (like the flag is grabbed and its the last point to win).

Bot Behavior
=======

At the bot level, there is going to be several changes in how they operate. The most notable one is the implementation of a 
view cone to check anything in their LOS. This LOS check is also used to determine if they can see a projectile headed right 
at them. This projectile LOS allows them to dodge incoming projectiles, which the AI has never done previously. Further, 
sound stimulation is simulated when things explode nearby, which can cause the bot to danger-step away from the source of 
the sound.

Planned and implemented Features:

- [x] Bots dodge grenades, but only when seen.
- [x] Bots move away from explosion sound sources when heard.
- [x] Bots pick a loadout that is meaningful for their commander-assigned task.
- [ ] Bots rearm at the nearest available inventory station.
- Bots do rearm, but not necessarily at the closest inventory station.
- [ ] Bots defend critical mission elements.
- Only in CTF: Bots defend the generators and flag.
- [ ] Bots attack critical mission elements.
- Only in CTF: Bots can run the flag.
- [ ] Bots have a view cone and react to visual sightings with a randomized delay and see reasonable distances.
- Mostly implemented; doesn't take fog into account.
- [ ] Bots use missile launchers against vehicles when available.
- [ ] Bots deal with cloakers correctly.
- Bots totally ignore cloakers right now.
- [ ] Bots dodge incoming linear pattern projectiles when seen.
- Partially implemented; only works for LinearProjectile and LinearFlareProjectile types.
- [ ] Bots take routes to avoid dangerous static placements, like turrets.
- [ ] Bots scout meaningful areas of the base.
- Only in CTF.
- [ ] Bots can engage hostile targets.
- [ ] Bots can use packs where applicable.
- Only ShieldPack: Bots use the shieldpack when being fought.
- [ ] Bots react to projectiles crossing into their line of sight.
- [ ] Bots report their current task when queried using VCW (What's your assignment?)
- Partially implemented; only works correctly for some tasks.
- [ ] Bots pick the best weapon in their inventory during engagement using either weapon meta data or data gleamed from the profiler.
- [ ] Bots return to base to rearm when sufficiently out of ammo or low on health.
- [ ] Bots coordinate on field. For example, some scouts escorting a heavy that will mortar some stuff.
- [ ] Bots repair destroyed base assets.
- [ ] Bots deploy motion sensors, pulse sensors, spider clamps and spike turrets.

Planned and supported gamemodes:
- [ ] CTF
- [ ] Hunters


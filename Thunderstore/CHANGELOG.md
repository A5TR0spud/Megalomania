
## 1.3.1

- Updated readme (discord) and description (etc+) a little bit
- Fixed default ConvertTo config causing a warning
- Fixed NRE caused by Lunar Minigun ability ending when used by Engineer turrets
- Fixed NRE caused by damage?
- Fixed NRE caused by bomb generation?
- Fixed NRE caused by Engineer turrets using Lunar Minigun trying to trigger Ego procs?

## 1.3.0

- Added On Hit Bomb Attack config option
- Added Bomb Density config option
- Checked the spelling of some config titles and descriptions
- Adjusted config in various ways to better support Gale mod manager
- Fixed language override not working?
- Fixed log using backticks in some places when it should be using single quotes

## 1.2.1 - *SotS 1.3.5 Fix*

- Fixed language override not working
- Fixed Egocentrism causing stacks of curse for some reason
- Internally set some things straight

## 1.2.0

- Added config options for initial stat boosts
- Max health, attack speed, and damage no longer increase from the initial stack of Egocentrism
- Added config option to consume regenerating scrap when consumed by Egocentrism, allowing it to regenerate next stage
- The short pickup description now changes based on config
- Updated readme
- Added Lunar Minigun skill
- Polished Chimera Shell skill
- Touched up Twin Shot and Chimera Bomb
- Buffed Conceit damage 60% -> 100%
- Attack skills now apply spread bloom (crosshair effects)

*Max health, attack speed, and damage still increase from stack 2+ of Egocentrism.*

## 1.1.1

- Adjusted Chimera Shell buff color to distinguish it from the vanilla Lunar Shell
- Fixed Chimera Shell visual overlay not syncing with buff

## 1.1.0 - *Skills*

- Added Stack Size Matters and 2 related configurable properties
- The logbook description of Egocentrism is now altered based on config settings
- Migrated Primary Replacement to new section *(7. Skills)*
- Now defaults to replacing primary with Conceit, and immediately corrupting Visions of Heresy to prevent conflict or confusion.
- Rename Primary Enhancement -> Primary Targetting
- New skills: Monopolize, Chimera Bomb, Twin Shot, Chimera Shell
- Polish Conceit skill

*See config for descriptions of new skills.*

## 1.0.1

- Fix primary replacement config using wrong option (enhancement)

## 1.0.0

- Added Convert To configurable property
- Added Conversion Selection Type configurable property
- Added Minimum Time configurable property
- Added Primary Replacement configurable property
- Added Primary Enhancement configurable property
- Migrated Benthic-like transformation to its own properties
- Adjust default weights for a larger difference and a higher focus on void items
- Nerf default bomb generation frequency stacking rate *(1 -> 0.5)*
- Buff default bomb damage *(200% -> 300%)*
- Nerf default stacking bomb damage *(10% -> 5%)*
- Buff default bomb range *(15 -> 17.5)*
- Nerf default stacking bomb range *(1 -> 0.5)*
- Default multiplier per stack *(0.9 -> 1)*
- Internally, things may be a bit more *"oh god"* but it works on my device
- Swapped ordering of changelog so most recent is on top

## 0.1.1

- Fix unnecessary dependency error

## 0.1.0

- Publish

# About This Project
This is a fun game I made about my friend Ryan. It's an pokemon (was being converted to Final Fantasy) style game about my friends, with Ryan being the main character. I added some simple story elements to it for some flavor. I worked on this project sporadically between 2019-2021.

# Highlights
Cutscene Editor: There was an in-engine editor I made that allowed easy creation of cutscenes involving characters talking, giving emphasis to the current speaker and typing out text at a variable speed.
Event System: There was a very simple and rudimentary event system that could trigger cutscenes, fights, and addition of characters from different events such as other cutscenes, position of the player, and more.
Combat System: Every character has their own attacks and strengths, which depend on multiple stats of the character to affect enemy stats and health.
Saving System: Saved the player's position, party, triggered events and cutscenes, their current stats, and nearby enemies and their stats.
Music: This was one of my first attempts at trying to create music for a game, so every song is created by me in a simple 8-bit song editor. It's not very great, but it lead me towards thinking about sound/music managers which I've used since.

# Insight On Drawbacks and Issues
Initially the game was created to be like pokemon, meaning every character had 4 set attacks. I started reworking this to be like final fantasy RPGs, meaning they had unlimited attacks that were sorted in simple menus. Sadly I never finished this.
There was a known issue in the main combat loop. Essentially in a fight, the main combat loop would call the methods in the combatants without properly wrapping code in try catch blocks. This lead to the occassional GameObject is null popping up and crashing the combat loop, which lead to the whole game being frozen on the combat screen. This would force the player to restart the game, losing all of their progress since the last autosave.

# Final Comments
Overall this project was definitely something I never saw going anywhere meaningful, but it was a lot of fun playing it with others in Discord. It taught me a lot about larger project structure and helped a lot with learning how to refactor and intepret non-ideal code (most of it propably doesn't look intepretable to this day). Good starting point for creating an RPG fighting game, lots of setup work still needed.
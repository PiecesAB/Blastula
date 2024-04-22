![blastula danmaku framework](/Icons/outlined256.png)

## Warning: WIP

### This project is not yet in a usable state. More work is being done every day.

Blastula will be an add-on for the [Godot Game Engine](https://godotengine.org/) that aims to make the development of danmaku shooting games, particularly Touhou-likes, easy in a new way, balancing power and convenience. 
It's inspired by amazing existing editors/frameworks as [Danmakufu](https://github.com/Natashi/Touhou-Danmakufu-ph3sx-2), [LuaSTG](https://github.com/9chu/LuaSTGPlus), [Danmokou](https://github.com/Bagoum/danmokou).

## Features
* Complex patterns and bullet behaviors are possible using modular APL-inspired operations.
* Versatile scheduling operations, used for shot, enemy movement, stage planning.
* Tree-based sub-engine in C# organizes patterns; works very well with modular operations.
* Controlled multithreading allows the game to remain deterministic while accelerating certain actions.
  * (Although it seems that with the rest of game logic, it is currently just on par with other popular engines.)
* Easily customizable graphics and collision information.
* Assorted STG infrastructure, such as grazing, player(s), and enemy behaviors.
* Debug console for comprehensive testing.
* MIT license, same as Godot: free for all purposes with no catch. (Though it would be nice to give credit.)

## Setup
1. This is a C# project last updated with Godot 4.2.1, so be sure to get the .NET enabled version of the editor.
2. Create a new Godot project, or navigate to the existing folder of the project. Close Godot (If you leave it open during the next steps, you will suffer. Everything will fall apart.)
3. In Git Bash or equivalent Git interface, make the current working directory the "addons" folder within the project.
4. Clone the repository into the addons folder. If using Git Bash, this is done with the command "git clone https://github.com/PiecesAB/Blastula". This will create a "Blastula" folder within the "addons" folder. Be sure this is capital B Blastula. If you don't want to deal with Git, you can also just download the repository and copy it into the addons folder manually.
5. Now open Godot and try to build the code by clicking the hammer icon in the top right corner, just left of the play button. If the hammer isn't there, try making a new placeholder C# class, and it should appear.
6. It likely failed to build the code because of complaining about an "/unsafe" directive. This is because Blastula sacrifices guaranteed memory safety for performance. To fix this, navigate to the .csproj file in the root of the Godot project (which has been generated for you by Godot when we tried to build) and add "&lt;AllowUnsafeBlocks&gt;true&lt;/AllowUnsafeBlocks&gt;" as an extra PropertyGroup.
7. Try to build with the hammer again. It should succeed.
8. In the Plugins tab of Project Settings in Godot, be sure to enable the Blastula plugin. This should automatically set it up so that the start scene is "Main Scene.tscn", and other things such as the resolution are modified.
9. Everything should now be in place!

As the project develops (pun intended), more information will be found [on the wiki.](https://piecesab.github.io/blastula/)

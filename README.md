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
  * Will 40000+ bullets at 60 fps be enough?
* Easily customizable graphics and collision information.
* Assorted STG infrastructure, such as grazing, player(s), and enemy behaviors.
* Debug console for comprehensive testing.
* MIT license, same as Godot: free for all purposes with no catch. (Though it would be nice to give credit.)

## Setup
The best way to get started is by cloning this entire repository, which is a Godot project. (I know everything's in a plugin, which is weird. Please sway my opinion on this.)
This is a C# project last updated with Godot 4.2.1, so be sure to get the .NET enabled version of the editor.
It also relies extensively on unsafe C# (ability to use pointers) for boosted performance. As such, this repository has a modified .csproj file, where "&lt;AllowUnsafeBlocks&gt;true&lt;/AllowUnsafeBlocks&gt;" is an extra PropertyGroup to make unsafe code possible.

As the project develops (pun intended), more information will be found [on the wiki.](https://piecesab.github.io/blastula/)

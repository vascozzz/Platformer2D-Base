# Platformer2D-Base 

A 2D character controller for Unity. Adapted from prime31's fantastic [CharacterController2D](https://github.com/prime31/CharacterController2D) and Sebastian Lague's [2D platformer tutorial](https://github.com/SebLague/2DPlatformer-Tutorial). Physics are forsaken in favor of reliable, consistent control over code.

## Features

- Slopes
- Jumping (with variable jump height based on keypress duration)
- Wall-sliding
- Wall-jumping
- Dashing
- One-way platforms

The one-way platform check is made against an individual platform, ensuring the character's body passes through it entirely. From personal experience, this eliminates the issues of characters getting stuck in both prime31 (single frame) and Sebastian's (time-based) solutions.

## Todo

- Double-jumping
- Forces between characters/objects
- Customizable controls
- ...

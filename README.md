# Noisier-Nodes
This repository contains a collection of Unity/HLSL noise functions and utilities to assist with shader development.

## Installation
Download the latest package release in https://github.com/RelaxItsDax/Noisier-Nodes/releases, and import it (Assets -> Import Package -> Custom Package) and select the package to import it into your project.

## Usage
### Shader Graph Nodes
Shader Graph nodes should be automatically imported, so just type in the name of the node you want to use and create it from there.

### Perlin, Simplex:
Sampling coordinates: The coordinates to sample.
Scale: The scale of the noise.

### Peridoic:
Period: The period over which the noise should repeat. This is in terms of the scale, so a node with scale 10 and period (2, 4) will repeat 5 times in the x direction and 2.5 in the y direction. 

### Simplex Gradient:
Ouput Value: The length of the gradient vector.
Output Gradient: The gradient vector.

### Voronoi:
Sampling coordinates: The coordinates to sample.
Angle Offset: The offset of the cell angles. Values closer to 0 look more square, and higher values become more chaotic.
Cell Density: How close the cells are. Effectively scale.

### Noise Generator
There is a Noise Generator implemented alongside the noise functions in this project. It can be used to generate 2D and 3D noise textures Eand 2D PNGs for sampling. It is located in `Noisier-Nodes/Noise/NoiseGenerator.asset`.
#### Parameters:
Output Settings:
- Noise Generators: **DO NOT MODIFY THIS!** This contains references to all of the compute shaders used to calculate noise values.
- Type: The type of noise to generate.
- File Name: The file name (path) to write to. Starts from `Assets/(Your file name).asset`.
- Height: The height of the sample.
- Width: The width of the sample.
- Depth: The depth of the sample. Only used for 3D noise.
- Encode to PNG: Wether the generator should output a Texture2D asset file or a PNG. Only works for 2D noise.
- Seed: The seed to use in generation.
Perlin/Simplex Inputs: (Used for Perlin and Simplex)
- Scale: The noise scale.
Voronoi Inputs:
- Cell Density: The cell density.
- Angle Offset: The angle offset.

Output:
- Ouput Texture: If you wrote to a texture (not PNG), this will contain a reference to the most recently generated texture.

## References
This code and repository are based off [@JimmyCushnie](https://github.com/JimmyCushnie)'s [Noisy Nodes package](https://github.com/JimmyCushnie/Noisy-Nodes), which is based off [@Keijiro](https://github.com/keijiro)'s [Noise Shaders](https://github.com/keijiro/NoiseShader).

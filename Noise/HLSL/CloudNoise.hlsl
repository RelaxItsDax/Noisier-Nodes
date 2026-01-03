#include "Assets/Shaders/Noise/HLSL/Voronoi3D.hlsl"

float cloud_noise(float3 pos, float stepPower, float density, float angleOffset)
{
    float firstDegree = 0;
    float secondDegree = 0;
    float thirdDegree = 0;
    float firstCells = 0;
    float secondCells = 0;
    float thirdCells = 0;
    Voronoi3D_float(pos, angleOffset, density * stepPower * stepPower, firstDegree, firstCells);
    Voronoi3D_float(pos, angleOffset, density * stepPower, secondDegree, secondCells);
    //Voronoi3D_float(pos, angleOffset, density, thirdDegree, thirdCells);
    secondDegree *= stepPower;
    thirdDegree *= stepPower * stepPower;
    
    float max = 1 + stepPower;// + stepPower * stepPower; //Max value.
    float sum = firstDegree + secondDegree + thirdDegree;
    return 1 - Remap(0, max, 0, 1, sum);
}
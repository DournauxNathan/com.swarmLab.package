using SwarmLab;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MyCustomRule : SteeringRule
{
    public override Vector3 CalculateForce(Entity entity, List<Entity> neighbors)
    {
        // Your logic here
        return Vector3.zero;
    }
}
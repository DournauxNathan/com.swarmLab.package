# SwarmLab Documentation

## Overview
SwarmLab is a Unity package designed to simplify the creation and management of rule-based particle systems (swarms) within the runtime environment. It allows for the simulation of complex autonomous behaviors such as flocking, following, and avoiding, usable in both volumetric (3D) and planar (2D) modes.

## Package contents
The package is organized as follows:
- **Runtime**: Contains the core logic for the simulation, including `SwarmManager`, `SwarmConfig`, and the base `SteeringRule` classes.
- **Editor**: Custom inspectors and gizmos to facilitate visual debugging and setup.
- **Samples**: Example scenes and assets to demonstrate the package capabilities.

## Installation instructions
### Via Package Manager (Git)
1. Open the Unity Package Manager.
2. Click the `+` button in the top left.
3. Select "Add package from git URL..."
4. Paste the following URL: `https://github.com/xaxam2001/SwarmLab.git?path=/SwarmLab-UP`

### Via Source Code (Local)
If you want to modify the package source code:
1. Download the repository as a ZIP or Clone it.
2. Copy the `SwarmLab-UP` folder into your Unity project's `Packages` folder (or anywhere in your project assets).
3. Open the Unity Package Manager.
4. Click the `+` button in the top left.
5. Select **"Add package from disk..."**.
6. Select the `package.json` file inside the `SwarmLab-UP` folder you just copied.

## Requirements
- **Unity Version**: 6000.2 or higher.
- **Hardware**: Standard desktop runtime requirements.

## Limitations
- **Performance**: The current interaction algorithm is **O(N²)**. Optimizations (such as spatial partitioning) are not yet implemented in this version, which explains the limitation on the number of entities. It is recommended to keep the entity count below 300-500 for stable frame rates on average hardware.
- **Physics**: The system uses a custom velocity/position integration and does not rely on Unity's Rigidbody physics engine for movement, though it can govern objects that *have* colliders.

## Workflows

### 1. Creating a Swarm Configuration
The `SwarmConfig` asset defines *who* is in the swarm.
1. Right-click in the Project window.
2. Navigate to **Create > SwarmLab > Swarm Config**.
![Create Swarm Config](Documentation~/create-swarmConfig-so.gif)
3. In the Inspector, you can add "Species Configs".
![Configure Swarm Config](Documentation~/config-swarmConfig-so.gif)
4. For each species, assign a **Species Definition**.
   - You can add **Steering Rules** to each species configuration to define their behavior (e.g., Alignment, Cohesion).
   ![Add Rules](Documentation~/add-rule-foreach-species.gif)

5. To create a Species Definition:
   - Right-click > **Create > SwarmLab > Species Definition**.
   ![Create Species Definition](Documentation~/create-species-so.gif)
   - **Prefab**: Assign the GameObject to spawn.
   - **Species Name**: The unique identifier for this species (used in rules).
   - **Max Speed**: The absolute speed limit for entities of this species.

![Species Definition Inspector](Documentation~/config-specie-in-inspector.png)

### 2. Setting up the Swarm Manager
The `SwarmManager` is the brain of the simulation.
1. Create an empty GameObject in your scene name "SwarmManager".
2. Add the `SwarmManager` component.
3. Assign your **Swarm Config** asset to the `Swarm Config` field.
4. **Volumetric vs Planar**: 
   - By default, the simulation is **Volumetric** (3D sphere).
   - To switch to **Planar** (2D), assign a Transform to the `Planar Boundary` field.
   - Shortcut: Right-click the `SwarmManager` component header and select **Create Simulation Plane**. This will automatically generate a boundary plane and switch the mode to Planar.
   ![Create Planar Simulation](Documentation~/create-simulation-planar-and-change-planar-settings.gif)

### 3. Running the Simulation
1. In the Inspector, click the **Generate Swarm** button (green).
2. Press **Play**.
3. The entities (already instantiated) will start moving according to the active Steering Rules.

## Advanced topics

### Custom Steering Rules
You can create your own behaviors by inheriting from `SteeringRule`.
1. Create a new C# script.
2. Inherit from `SwarmLab.SteeringRule`.
3. Implement the `CalculateForce(Entity entity, List<Entity> neighbors)` method.
4. The method must return a `Vector3` representing the desired acceleration force.
5. You can now add this new rule to any `SpeciesConfig` in your scriptable objects.

```csharp
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
```

### SteeringRule Architecture
The `SteeringRule` is the abstract base class for all behaviors. If you are extending the system, it is important to understand its members:

| Member | Type | Description |
| :--- | :--- | :--- |
| `CalculateForce` | `abstract Vector3` | **Required**. The core logic method. Receives the `Entity` (self) and a list of `neighbors` (all entities). Must return an acceleration `Vector3`. |
| `OnValidate` | `virtual void` | Use this to perform editor-time validation (e.g. clamping values). Always call `base.OnValidate()` if you override it. |
| `GetWeightFor` | `float` | **Helper**. Returns the specific weight for a given `SpeciesDefinition` based on the configuration in the inspector. Use this to implement species-specific logic. |
| `speciesWeights` | `List<SpeciesWeight>` | The raw list of species-weight pairs. Generally you should use `GetWeightFor` instead of accessing this directly. |

### Multi-Species Interactions
The system supports multiple species interacting. Each `SteeringRule` has a `SpeciesWeights` list.
- You can define that "Species A" ignores "Species B" but is strongly attracted to "Species C".
- Use the **Species Weights** list in the rule inspector to fine-tune these interactions per species.

## Reference

### SwarmManager Inspector
| Property | Description |
| :--- | :--- |
| **Draw Spawn Zones** | Toggles the editor gizmos for spawn locations. |
| **Swarm Config** | The configuration asset defining populations. |
| **Simulation Mode** | **Volumetric**: 3D Sphere. **Planar**: 2D Plane (ignores Y axis). |
| **Planar Boundary** | The Transform defining the 2D plane (Ground). Required for Planar mode. |
| **Planar Size** | The X/Z dimensions of the simulation area in Planar mode. Entities wrap around edges. |

### Common Rule Properties
Most rules share these settings:
- **Neighbor Radius**: How far an entity can "see" others.
- **Max Force**: The physical limit of how fast the entity can change direction.
- **Species Weights**: A multiplier for how strictly this rule applies to specific neighboring species.

## Samples
The package includes a **Demo Scene** located in `Samples/Demo Scene`.
- This scene demonstrates a multi-species setup with Alignment, Cohesion, and Separation rules.
- To install it, go to the Package Manager > SwarmLab > Samples and click **Import**.

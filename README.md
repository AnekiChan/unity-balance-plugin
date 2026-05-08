# Unity Balance Plugin

Unity plugin for game balancing similar to Machinations. Provides node-based visual scripting for economy and resource flow simulation.

## Features

- Visual node-based editor for balancing gameplay economy
- Support for different node types: Source, Drain, Pool, Converter
- Resource flow simulation with customizable tick counts
- Formula support for dynamic output values
- Graph visualization of resource changes over time
- Multiple currency support

## Installation

### Via Git URL (Recommended)

1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click the `+` button in the top-left corner
4. Select `Add package from git URL...`
5. Enter the following URL (note the leading slash in the path):
   ```
   https://github.com/AnekiChan/unity-balance-plugin.git?path=/Assets/Balance%20Plugin
   ```
6. Click `Add`

### Via Package Manager (manifest.json)

Add the following to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.anekichan.balance-plugin": "https://github.com/AnekiChan/unity-balance-plugin.git?path=/Assets/Balance%20Plugin"
  }
}
```

## Usage

1. Open the Balancing Window: `Tools > Balancing Window`
2. Create a new BalancingData asset or use an existing one
3. Add currencies in the sidebar
4. Create nodes (Source, Drain, Pool, Converter) by right-clicking on the canvas
5. Connect nodes by right-clicking on output points or using Connect mode
6. Run simulation with the "Predict" button
7. Export graphs for visualization

## Node Types

- **Source**: Generates resources
- **Drain**: Consumes resources
- **Pool**: Stores and forwards resources
- **Converter**: Converts resources with formulas

## License

See LICENSE file for details.

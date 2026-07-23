# HSIApp

A desktop application for loading, visualizing, and processing hyperspectral imaging (HSI) data.

The goal of this project is to develop a user-friendly interface for working with hyperspectral cubes, including data loading, visualization, metadata inspection, and future integration of analysis and machine learning workflows.

## Features

Currently implemented:

- WPF desktop application using C# and .NET
- Hyperspectral image metadata loading
- ENVI-style header (`.hdr`) parsing
- Basic HSI cube structure
- Initial application architecture for:
  - Data input/output
  - Visualization
  - User interface components

Planned features:

- Loading hyperspectral cubes (`.raw` + `.hdr`)
- Interactive band visualization
- Spectral profile visualization
- Masking capabilities
- Integration with machine learning models
- Connection with Python-based hyperspectral processing workflows

## Project Structure
```
HSIApp/  
│  
├── HSIApp/ # Main WPF application  
│ ├── Controls/ # Reusable UI components  
│ ├── IO/ # File loading and data handling  
│ ├── Models/ # Data structures (e.g. HSI cube, metadata)  
│ ├── Rendering/ # Visualization-related functionality  
│ └── ...  
│  
├── HSIPlayground/ # Console project for testing and development  
│  
├── HSIApp.slnx # Visual Studio solution  
├── HSIApp_Roadmap # Project roadmap  
└── README.md  
```

## Development Notes

The HSIPlayground project contains experimental code used during development and may not always represent the current application state.

# HSI Desktop Application Roadmap

**Project:** HSI Desktop Analysis Platform

**Author:** Luka

**Status:** In Development

---

# Vision

Create a desktop application that allows researchers to analyze hyperspectral images without writing code.

The application should provide a complete workflow for loading hyperspectral cubes, inspecting them, preprocessing them, applying masks, running trained machine learning models, visualizing prediction maps, and exporting results.

The application should eventually integrate closely with the existing Python HSI library while providing an intuitive graphical interface suitable for everyday use within the department.

---

# Core Design Principles

## 1. Image First

The image viewer is always the center of the application.

Everything else exists to support image interpretation.

* Metadata
* Spectrum viewer
* Project explorer
* Mask controls
* Prediction controls

should never distract from the image.

---

## 2. Every Phase Produces a Working Program

No unfinished branches.

Every milestone should leave the application usable.

---

## 3. Build Generic Components

Avoid writing features that only solve one problem.

Instead create reusable components such as

* ImageViewer
* SpectrumViewer
* MetadataPanel
* ProjectExplorer
* MaskManager
* PredictionManager

These can then be reused throughout the application.

---

## 4. Separate Logic from UI

Whenever possible

UI

↓

calls

↓

Application logic

↓

calls

↓

HSI library

The GUI should contain as little processing code as possible.

---

## 5. Python Does the Heavy Work

The desktop application is primarily

* visualization
* workflow
* user interaction

Heavy processing should remain in the Python library whenever practical.

---

# Long-Term Workflow

```
Load Cubes

↓

Inspect

↓

Preprocess

↓

Mask

↓

Predict

↓

Analyze

↓

Export
```

---

# Version Roadmap

---

# Version 0.1 — Simple Viewer

Goal:

Open a cube and inspect it.

This is the first usable release.

## Required Features

### Project

* Open cube
* Close cube
* Recent files (optional)

---

### Viewer

* Display first band

* Band slider

* Previous / Next buttons

* Display current band number

* Display wavelength

---

### Metadata

Display

* filename

* width

* height

* bands

* datatype

* interleave

* byte order

* wavelength range

---

### UI

Toolbar

Base tab

Image viewer

Metadata panel

Band controls

---

### Nice to Have

Image title

Status bar

Loading indicator

Keyboard shortcuts

---

## Completion Criteria

A user can

* open any supported cube

* browse every band

* inspect metadata

without writing code.

---

# Version 0.2 — Better Viewer

Goal

Turn the viewer into a proper inspection tool.

## Features

Image zoom

Mouse wheel zoom

Pan

Fit to window

Reset view

Pixel coordinates

Crosshair

Image histogram

Image statistics

Current pixel value

Display wavelength

Current intensity

Colormap selection

Contrast stretching

Percentile normalization

Min-max normalization

---

## Completion Criteria

The application is pleasant to explore cubes with.

---

# Version 0.3 — Spectral Exploration

Goal

Allow users to inspect spectra.

## Features

Interactive spectrum plot

Hover mode

Click mode

ROI mode

Rectangle selection

Mean spectrum

Multiple spectra

Spectrum manager

Spectrum legend

Show / hide spectra

Export spectrum

Import spectrum

Save spectrum

Compare spectra

---

## Completion Criteria

The application becomes useful for exploratory analysis.

---

# Version 0.4 — Project Explorer

Goal

Support multiple datasets.

## Features

Project panel

Multiple cubes

Rename entries

Remove cubes

Current selection

Project save

Project load

Drag and drop cubes

Batch loading

Project notes

---

## Completion Criteria

Multiple cubes can be managed simultaneously.

---

# Version 0.5 — Preprocessing

Goal

Prepare data before prediction.

## Features

Normalization

SNV

MSC

Savitzky-Golay

Band selection

Cropping

ROI extraction

Block averaging

Preview preprocessing

Undo

Reset preprocessing

---

## Completion Criteria

All preprocessing can be performed from the GUI.

---

# Version 0.6 — Masking

Goal

Create and manage masks.

## Features

Threshold masking

KMeans masking

Manual masking

Brush tool

Polygon ROI

Load mask

Save mask

Invert mask

Overlay transparency

Mask statistics

Mask manager

Apply mask

Remove mask

Preview mask

---

## Completion Criteria

Users can isolate pixels before prediction.

---

# Version 0.7 — Prediction

Goal

Run trained models.

## Features

Load model

Model metadata

Predict current cube

Predict batch

Prediction progress

Prediction overlay

Confidence map

Prediction legend

Prediction statistics

Export prediction

---

## Completion Criteria

A researcher can use a trained model without Python.

---

# Version 0.8 — Results

Goal

Analyze predictions.

## Features

Pixel counts

Area percentages

Summary statistics

Confusion matrix

Class distributions

Interactive plots

Linked selection

Export figures

Export CSV

Generate report

---

## Completion Criteria

The application produces publication-ready outputs.

---

# Version 0.9 — Batch Processing

Goal

Automate repetitive work.

## Features

Batch masking

Batch prediction

Progress window

Processing queue

Error handling

Continue after failure

Result summary

---

## Completion Criteria

Entire folders can be processed automatically.

---

# Version 1.0 — Department Release

Goal

Stable software for everyday use.

## Features

Installer

Settings

User preferences

Documentation

Example datasets

Model management

Logging

Crash reporting

Automatic updates

---

## Future Ideas

### Live Acquisition

Connect directly to HSI camera

Display incoming cube

Live prediction

---

### Online Learning

Model updating

Incremental training

Version management

---

### Advanced Visualization

RGB composites

False-color composites

3D visualization

Linked plots

Animation

Time series

---

### Analysis Tools

PCA visualization

PLS visualization

Feature importance

Band selection

Spectral libraries

Distance metrics

---

### Export

PNG

TIFF

PDF

PowerPoint

CSV

Excel

JSON

---

# Technical Improvements

Create reusable controls

* ImageViewer
* SpectrumViewer
* MetadataPanel
* Colorbar
* Histogram
* StatusBar

Separate into projects

```
HSIApp.UI

HSIApp.Core

HSIApp.IO

HSIApp.Visualization

HSIApp.Models

HSIApp.PythonBridge
```

Use MVVM once the project becomes sufficiently large.

---

# Development Rules

## Never build multiple major features simultaneously.

Only one feature branch at a time.

---

## Every commit should leave the application runnable.

---

## Refactor often.

If code feels duplicated more than twice,
stop and improve the design.

---

## Prefer reusable components.

Avoid writing code that only works in one window.

---

## Finish before expanding.

A completed simple feature is more valuable than five unfinished advanced ones.

---

# Current Focus

**Version 0.1 — Simple Viewer**

Current tasks:

* [ ] Finish clean WPF layout
* [ ] Open cube
* [ ] Load metadata
* [ ] Display first band
* [ ] Band slider
* [ ] Previous / Next buttons
* [ ] Display wavelength
* [ ] Display filename
* [ ] Status bar
* [ ] Basic error handling
* [ ] Save screenshot
* [ ] Test with multiple cubes
* [ ] Refactor viewer code
* [ ] Tag first release (v0.1.0)

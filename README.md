# GDWeave.Sample

A sample for C# [GDWeave](https://github.com/NotNite/GDWeave) mods.

## Setup

Clone/fork/whatever this repository. Pick an ID for your project (people like to do `Username.ProjectName`, but there is no enforced naming scheme). Rename the following:

- Solution name, project name, and project namespace to your project ID
- Various fields in the manifest.json to your project ID and name

## Building

To build the project, you need to set the `GDWeavePath` environment variable to your game install's GDWeave directory (e.g. `G:\games\steam\steamapps\common\WEBFISHING\GDWeave`). This can also be done in Rider with `File | Settings | Build, Execution, Deployment | Toolset and Build | MSBuild global properties` or with a .user file in Visual Studio.

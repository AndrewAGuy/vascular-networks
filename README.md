<h1> <img align="left" src="package/icon.svg" width="48"/> &nbsp; Vascular.Networks </h1>

#### Procedural Plumbing for Bioengineers
This package contains the core libraries for defining vascular trees, their bounding geometries and functional structures, and the operations to build, optimize, constrain, analyze and triangulate them. Libraries for .stl, .csv and .json import/export are also provided.

#### Collaboration/Support
This is not a complete piece of software; rather, a set of libraries that may be used to build one. If you are an experimentalist and want custom networks or bespoke software for your use case without needing to write code, get in touch.

#### License
This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).

## Installation
Add to project: `dotnet add package Vascular.Networks`

Install to directory: `nuget install Vascular.Networks -OutputDirectory <target-directory>`

Download directly

#### Requirements
.NET 5.0

Vascular.Analysis was developed to interoperate with Python 3.7.8 - earlier versions may work. Requires NumPy, optionally SciPy.

The FreeCAD .csv import macro was developed to work with FreeCAD 0.19, but will most likely work with earlier versions as it uses only basic functionality.

## Build
The project copies its output to a [NuGet convention-based working directory](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package#from-a-convention-based-working-directory) /Release/ in release configuration: `dotnet build -c Release`. All additional content files are copied to this structure. In Debug, output files are copied to a flat folder structure /Debug/.

To clean the release folder, copy over package assets and build the package, use the build file with the 'pack' target: `dotnet msbuild build.targets t:pack`. This sets the configuration to release and builds documentation.

To build XML documentation, set the property `-p:BuildDocs=True`.

#### Conditional compilation

- `NoEffectiveLength` - The most common use case for optimization is a combination of work and volume, so the branches by default keep track of their effective lengths and propagate these changes upstream, allowing instant query of the volume from the source node. Defining this disables the effective length caching.
- `NoDepthPathLength` - Path lengths and logical depths are calculated from the root downwards in a single pass and stored at each node. If defined, these fields and methods are removed. Note that some optimization predicates may require depths to be defined, although flow rate could be used as a proxy in this case.
- `NoPressure` - Pressures may be calculated downwards from the root after radii are assigned. If defined, these fields and methods are removed.

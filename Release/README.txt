--------- Vascular.Networks : "Procedural Plumbing for Bioengineering" ---------
                                Andrew Guy, 2021                                

This package contains the core libraries for defining vascular trees, their
bounding geometries and functional structures, and the operations to build, 
optimize, constrain, analyze and triangulate them. Libraries for .stl, .csv and 
.json import/export are also provided.

--- LICENSE --------------------------------------------------------------------
The software is distributed under the GNU Affero General Public License v3.0 
(agpl-3.0-only). Any software derived from this must be distributed under a 
compatible license.

Source code is available at https://github.com/AndrewAGuy/vascular-networks.

--- REQUIREMENTS ---------------------------------------------------------------
Common - .NET 5.0 runtime.
 
Vascular.Analysis - contains classes for python interoperation. This will 
    require you to configure a python environment with numpy (and optionally
    scipy) and pass the path to this along with the path to the required script
    to the process wrapper, or use a third-party library to embed a python 
    environment and execute the scripts. Written for python 3.7.8, may work with
    earlier versions.
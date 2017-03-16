# AI-ROCKS

Readme in progress...

This README has various sections:
- Building (and dependencies)
- Running
- References

## Building:

To build AI-ROCKS, all dependencies must have their `.dll`s in the top level of AI-ROCKS.
AI-ROCKS has the following dependencies: **ObstacleLibrary**, **LRFLibrary**, **EmguCV**, and <other things from Vision>.

**Build everything in x64.**

### Building Dependencies
Refer to each library's README for more complete build details. Summaries of building dependencies are given here:

#### ObstacleLibrary:
```
Buildling ObstacleLibrary:
- Open `csharp/ObstacleLibrary.sln` in Visual Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager. 
  Set `Active Solution Platform` to x64 and both `ObstacleLibrary` and `ObstacleLibraryNative` projects to x64.
- Build solution, or if there are issues, `ObstacleLibraryNative` and then `ObstacleLibrary` in that order
- Resultant `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` are in the `csharp/x64/Debug` directory
- Copy `ObstacleLibrary.dll` into top level of AI-ROCKS
```

#### LRFLibrary (Windows):
LRFLibrary has ObstacleLibrary as a dependency itself. As a result, do the following for ObstacleLibrary:
```
Building ObstacleLibrary for LRFLibrary:
- Clone ObstacleLibrary into the top level of LRFLibrary
  (Note: required to clone into top level - cannot just copy .dlls)
- Complete the above build instructions for ObstacleLibrary (which is cloned in LRFLibrary top level)
- Copy the `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` files into the top level of LRFLibrary
```

Now, build LRFLibrary:
```
Building LRFLibrary:
- Open `build/windows/LRFLibrary.sln` in Vision Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager.
  Set `Active Solution Platform` to x64 and both `LRFLibrarySharp` and `LRFLibrary` projects to x64.
- Build solution, or if there are issues, `LRFLibrary` and then `LRFLibrarySharp` in that order
- Resultant `LRFLibrarySharp.dll` is in the `build/windows/x64/Debug` directory
- Copy `LRFLibrarySharp.dll` into top level of AI-ROCKS
```

#### EmguCV: <Joe has instructions>
TODO 

#### Other (yet to come...)
TODO

### Building AI-ROCKS
Once all dependencies are in the top level of AI-ROCKS (above), build AI-ROCKS:
```
Building AI-ROCKS:
- Open `AI-ROCKS.sln`
- Ensure you are building in x64: Navigate to Build > Configuration Manager.
  Set `Active Solution Platform` to x64 and the `AI-ROCKS` project to x64.
- Build solution
```

## Running:
### Running AI-ROCKS
AI-ROCKS is currently run from Visual Studio in Windows. To run, do the following:
```
- Open `AI-ROCKS.sln` in Visual Studio (VS).
- Ensure AI-ROCKS builds. Refer to 'Building' for details.
- Navigate to Solution Explorer in VS. Right click on AI-ROCKS project > Properties.
- Navigate to 'Debug' tab. Under 'Start options', specify required command line args. These are specified below.
- Hit 'Start' and AI-ROCKS should run.
```

### Command line arguments
The command line arguments to AI-ROCKS are as follows:
- `-l <port>`		- COM or UDP port that the LRF is communicating over. 
COM ports are specfied like eg. `COM4` and UDP ports are specified by their number, eg `20001`.
Note: COM ports are primarily used when the LRF is plugged into the computer running AI-ROCKS (i.e. Ascent),
while UDP ports are primarly used when the LRF data is coming from some other source over UDP (i.e. Gazebo).

- `-d <state>`		- Initial `DriveState` to start AI-ROCKS in, according to `StateType` enum.
Currently 0 = `GPSDriveState`, 1 = `VisionDriveState`, and 2 = `ObstacleAvoidanceDriveState`.
The default `DriveState` is `GPSDriveState`.

- `-g <address>`	- IP address to communicate to ROCKS over. The default is loopback (127.0.0.1) as this is used
for communicating to Ascent, so if no value is specified, this is what's used. If using Gazebo (i.e. testing), 
specify the IP address of the computer running Gazebo (i.e. the IP of the comupter acting as Ascent) in dot notation. 
Example: `-g 192.168.1.80`.


## References:
(Pretty much just a dump of useful resources for now)

We will use our own rendition of the Follow the Gap method.

Here is a reference: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
Here is a YouTube video: https://www.youtube.com/watch?v=TohW9xokbaM
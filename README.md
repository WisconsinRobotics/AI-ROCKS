# AI-ROCKS

Readme yet to come..

## Building:

To build AI-ROCKS, all dependencies must have their .dlls in the top level of AI-ROCKS. *Build everything in x64.*
AI-ROCKS has the following dependencies: *ObstacleLibrary*, *LRFLibrary*, *EmguCV*, and <other things from Vision>.

### Building Dependencies
Refer to each library's README for more complete build details. Summaries of building dependencies are given here:

#### ObstacleLibrary:
```
- Open `csharp/ObstacleLibrary.sln` in Visual Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager. Set `Active Solution Platform` to x64 and both
`ObstacleLibrary` and `ObstacleLibraryNative` projects to x64.
- Build solution, or if there are issues, `ObstacleLibraryNative` and then `ObstacleLibrary` in that order
- Resultant `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` are in the `csharp/x64/Debug` directory
- Copy `ObstacleLibrary.dll` into top level of AI-ROCKS
```

### LRFLibrary (Windows):
LRFLibrary has ObstacleLibrary as a dependency itself. As a result, do the following for ObstacleLibrary:
```
- Clone ObstacleLibrary into the top level of LRFLibrary (required to clone into top level - cannot just copy .dlls)
- Complete the above build instructions for ObstacleLibrary (which is cloned in LRFLibrary top level)
- Copy the `ObstacleLibrary.dll` and `ObstacleLibraryNative.lib` files into the top level of LRFLibrary
```

Now, build LRFLibrary:
```
- Open `build/windows/LRFLibrary.sln` in Vision Studio
- Ensure you are building in x64: Navigate to Build > Configuration Manager. Set `Active Solution Platform` to x64 and both
`LRFLibrarySharp` and `LRFLibrary` projects to x64.
- Build solution, or if there are issues, `LRFLibrary` and then `LRFLibrarySharp` in that order
- Resultant `LRFLibrarySharp.dll` is in the `build/windows/x64/Debug` directory
- Copy `LRFLibrarySharp.dll` into top level of AI-ROCKS
```

### EmguCV: <Joe has instructions>
TODO 

### Other (yet to come...)
TODO

### Building AI-ROCKS
Once all dependencies are in the top level of AI-ROCKS (above), build AI-ROCKS:
```
- Open `AI-ROCKS.sln`
- Ensure you are building in x64: Navigate to Build > Configuration Manager. Set `Active Solution Platform` to x64 and 
the `AI-ROCKS` project to x64.
-- Build solution
```

## References:
(Pretty much just a dump of useful resources for now)

We will use our own rendition of the Follow the Gap method.

Here is a reference: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
Here is a YouTube video: https://www.youtube.com/watch?v=TohW9xokbaM
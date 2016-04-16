#Tutorial 04

##Goals
 - Learn to load 3D models as assets.
 - Understand Fusee's bilt-in ModelView and Projection matrices.
 - See how hierachicak geometry can be handled in object hierarchies.
 - Implement a very simple lighting calculation in the pixel shader.
 
#Meshes from files
FUSEE comes with a set of classes designed to be written to and loaded from files (seralized). These classes are containers for data types typically found in 3D scenes, such as polygonal geometry, material (color) 
settings, textures, and hierarchies. In this tutorial we will only look at how to read mesh geometry from files
in the FUSEE file forman (*.fus).

Open the Tutorial 04 project and build it on your preferred platform (Desktop, Android or Web). Running 
the result will show more or less the state where we left off at [Tutorial 03] (../Tutorial03):

![Tutorial 04 in its initial state] (Tutorial04Start.png)

The only visible change is that the background color changed from dark green to white. But let's look under the hood. Open
[Tutorial.cs] (Core/Tutorial.cs) and look at the first lines of the ```Init()``` method:

```C#
	public override void Init()
	{
		// Load the scene file "Cube.fus"
		SceneContainer sc = AssetStorage.Get<SceneContainer>("Cube.fus");
		
		// Extract the 'First' object of type 'MeshComponent' found in 'sc'`s list of 'Children' without 
		// further specifying any search criterion ('c => true' means: any found MeshComponent will do).
		MeshComponent mc = sc.Children.FindComponents<MeshComponent>(c => true).First();

		// Generate a mesh from the MeshComponent's vertices, normals and triangles.
		_mesh = new Mesh
		{
			Vertices = mc.Vertices,
			Normals = mc.Normals,
			Triangles = mc.Triangles
		};
		...
```

Notice that the explicit definition of the cube geometry, where every of the 24 vertices and 24 normals were listed
together with the 12 triangles making up the cube. We still have the ```_mesh``` field instantiated with a ```Mesh````
object but the ```Vertices```, ```Normals``` and ```Triangles``` arrays are now taken from some object ```mc``` which
is of type ```MeshComponent```.

```MeshComponent``` is one of the serialization classes, together with ```SceneContainer```, which are used as storage
objects that can be written to and loaded from files. Look at the first line: This is where a file called ```Cube.fus``` is
loaded (from some asset storage). You can find this file in the [Core/Assets] (Core/Assets) folder (together with some 
other *.fus files). To make any file below the ```Core/Assets``` folder to be an asset of the application and thus giving
you access to it throgh the ```AssetStorage```, you need to do two things (already done for ```Cube.fus```):

 1. Include it in the ```Assets``` folder in Visual Studio's Solution Exporer
    ![Cube.fus in Solution Explorer] (CubeInSolution.png)
   
 2. Set the asset file's properties in Visual Studion like so:
	- ```Build Action``` must be ```Content```
	- ```Copy to Output Directory``` must be ```Copy if newer```
    ![Cube.fus in Properties Editor] (CubeProperties.png)
	
Any file listed this way in the ```Core``` projects's ```Asset``` folder will be added as an asset to the resulting application
on the respective platform (Desktop: .exe file; Android: apk package; Web: html-file and subfolder structure). All assets added
in this way can be loaded with the ```AssetStorage.Get<>()``` method.

The next line extracts a ```MeshComponent``` object from the ```SceneContainer``` stored in ```Cube.fus```. The contents of a 
*.fus file is a tree-like structure starting with a root node of type ```SceneContainer```. Somewhere within the tree there can 
be ```MeshComponent``` objects storing chunks of geometry data. In ```Cube.fus```, there is only one such ```MeshComponent```
object and we retrieve that object with the second code line in ```Init()```. See the comment above that line for an explanation
how to understand this pretty complex instruction.

Now let's remove the scaling of the cube(s) from the setup of the 
	
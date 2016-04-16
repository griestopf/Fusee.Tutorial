#Tutorial 04

##Goals
 - Learn to load 3D models as assets.
 - Understand Fusee's bilt-in ModelView and Projection matrices.
 - See how hierachicak geometry can be handled in object hierarchies.
 - Implement a very simple lighting calculation in the pixel shader.
 
##Meshes from files
FUSEE comes with a set of classes designed to be written to and loaded from files (seralized). These classes are containers for data types typically found in 3D scenes, such as polygonal geometry, material (color) 
settings, textures, and hierarchies. In this tutorial we will only look at how to read mesh geometry from files
in the FUSEE file forman (*.fus).

Open the Tutorial 04 project and build it on your preferred platform (Desktop, Android or Web). Running 
the result will show more or less the state where we left off at [Tutorial 03] (../Tutorial03):

![Tutorial 04 in its initial state] (_images/Tutorial04Start.png)

The only visible change is that the background color changed from dark green to white. 

But let's look under the hood and try to understand what happened here first. 
Open [Tutorial.cs] (Core/Tutorial.cs) and look at the first lines of the ```Init()``` method:

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
together with the 12 triangles making up the cube is no longer there. We still have the ```_mesh``` field instantiated 
with a ```Mesh``` object but the ```Vertices```, ```Normals``` and ```Triangles``` arrays are now taken from some 
object ```mc``` which is of type ```MeshComponent```.

```MeshComponent``` is one of the serialization classes, together with ```SceneContainer```, which are used as storage
objects that can be written to and loaded from files. Look at the first line: This is where a file called ```Cube.fus``` is
loaded (from some asset storage). You can find this file in the [Core/Assets] (Core/Assets) folder (together with some 
other *.fus files). To make any file below the ```Core/Assets``` folder to be an asset of the application and thus giving
you access to it through the ```AssetStorage```, you need to do two things (already done for ```Cube.fus```):

 1. The asset file must be included in the ```Assets``` folder in Visual Studio's Solution Exporer:
 
    ![Cube.fus in Solution Explorer] (_images/CubeInSolution.png)
   
 2. The asset file's properties in Visual Studion must be set like so:
	- ```Build Action``` must be ```Content```
	- ```Copy to Output Directory``` must be ```Copy if newer```
	
    ![Cube.fus in Properties Editor] (_images/CubeProperties.png)
	
Any file listed this way in the ```Core``` projects's ```Asset``` folder will be added as an asset to the resulting application
on the respective platform (Desktop: .exe file; Android: apk package; Web: html-file and subfolder structure). All assets added
in this way can be loaded with the ```AssetStorage.Get<>()``` method. 
 
The next line extracts a ```MeshComponent``` object from the ```SceneContainer``` stored in ```Cube.fus```. The contents of a 
*.fus file is a tree-like structure starting with a root node of type ```SceneContainer```. Somewhere within the tree there can 
be ```MeshComponent``` objects storing chunks of geometry data. In ```Cube.fus```, there is only one such ```MeshComponent```
object and we retrieve that object with the second code line in ```Init()```. See the comment above that line for an explanation
how to understand this pretty complex instruction.

To conclude the changes applied to the completed state of Tutorial 03 to in order to yield the inital state of Tutorial 04 it 
should be mentioned that the namespaces ```Fusee.Serialization``` and  ```Fusee.Xene``` were announced with ```using``` statements
at the top of the source code file.

Now let's apply further changes to the current state. Inside ```RenderAFrame()```:
 1. Completely Remove the second of the two cubes from the scene.
 2. Apply a uniform scale to the first cube of 0.5 along all three axes.

Here's the resulting code:

```C#
		// First cube
		var cube1Model = ModelXForm(new float3(-0.5f, 0, 0), new float3(_pitchCube1, _yawCube1, 0), new float3(0, 0, 0));
		_xform = projection*view*cube1Model * float4x4.CreateScale(0.5f, 0.5f, 0.5f);
		RC.SetShaderParam(_xformParam, _xform);
		RC.Render(_mesh); 
```

 
###Practice
 - Get a grasp of the contents of a .fus file: Set a breakpoint AFTER (!!!) the first, the second and the third instruction of
   ```Init()``` and watch the contents of ```sc```, ```mc``` and ```_mesh``` after they have been assigned values to. Open the 
   hierachical structure in the watch window and look at the contents.
 - Try to replace the cube asset with some of the other files in the [Core/Assets] (Core/Assets) folder and see how they look like.
   Again, set a breakpoint and see the contents. How many vertices/triangles is the SoccerBall model made of?
 - Load one of the models containing hard edges as well as curved surfaces (Cylinder or Cone) into the ```_mesh```. Explain how 
   the rendering pipeline with your vertex and pixel shader assign one single color to the circular bottom (and probably top) faces
   while giving each pixel of the curved coating surface an individual color changing from pixel to pixel. 
 
##Lighting calculation basics
Make sure the file ```Cylinder.fus``` is loaded into the ```_mesh``` and the result looks like this:

![Cylinder with flat and faded color surfaces] (_images/CylinderColors.png)

Now let's answer the last question of the practice block above: As you remember from [Tutorial 03] (../Tutorial03), there's one single
normal present at each vertex of every triangle. The curved coating surface of the cylinder is made up of individual triangles as well. 
But instead of assigning normals to the three 


   
   
 

	
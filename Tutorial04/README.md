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

#Use FUSEE's Standard Matrices
Instead of using our self-defined ```_xform``` we can use a set of matrices which are maintained by FUSEE's render 
context (```RC```) and automatically propagated from the main application running on the CPU to the vertex shader 
on the GPU. The two commonly used matrices here are the **ModelView** and the **Projection** matrices.

From the CPU-Code (e.g. from inside ```RenderAFrame```) you can access (typically write) these two matrices using
the ```RC.ModelView``` and the ```RC.Projection``` properties. These are defind as ```float4x4``` properties - the 
FUSEE standard type for matrices.  From within your shader code, you can access these matrices by defining ```uniform```
properties with special names. Here you can, for example, declare variables like ```uniform vec4 FUSEE_MV;``` 
or ```uniform vec4 FUSEE_P;``` and read out the values currently set from CPU-Code. You can also access premultiplied 
versions of *ModelView* and *Projection* as well as inverted or transposed versions of all kinds of combinations of 
the above. In particular, the following matrices are available

 CPU-Code Name                    | CPU-Code Access | Shader-Code Declaration    |   Description  
----------------------------------|-----------------|----------------------------|---------------------------------------------------------------   
 `RC.ModelView`                   | Read/Write      | `uniform vec4 FUSEE_MV`    | The Model-View matrix transforming from model to camera space.
 `RC.Projection`                  | Read/Write      | `uniform vec4 FUSEE_P`     | The Projection matrix transforming from camera to clip space.
 `RC.ModelViewProjection`         | Read            | `uniform vec4 FUSEE_MVP`   | The combined (multiplied) result of `MV*P`
 `RC.InvModelView`                | Read            | `uniform vec4 FUSEE_IMV`   | The inverted Model-View matrix transforming from camera to model space.
 `RC.InvProjection`               | Read            | `uniform vec4 FUSEE_IP`    | The inverted Projection matrix transforming from clip to camera space.
 `RC.InvModelViewProjection`      | Read            | `uniform vec4 FUSEE_IMVP`  | `Invert(MV*P)`
 `RC.TransModelView`              | Read            | `uniform vec4 FUSEE_TMV`   | The transposed Model-View matrix.
 `RC.TransProjection`             | Read            | `uniform vec4 FUSEE_TP`    | The trasposed Projection matrix.
 `RC.TransModelViewProjection`    | Read            | `uniform vec4 FUSEE_TMVP`  | `Transpsoe(MV*P)`
 `RC.InvTransModelView`           | Read            | `uniform vec4 FUSEE_ITMV`  | The inverted transposed Model-View matrix.
 `RC.InvTransProjection`          | Read            | `uniform vec4 FUSEE_ITP`   | The inverted trasposed Projection matrix.
 `RC.InvTransModelViewProjection` | Read            | `uniform vec4 FUSEE_ITMVP` | `Invert(Transpsoe(MV*P))`

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
 
##A Very Basic Lighting Calculation
Make sure the file ```Cylinder.fus``` is loaded into the ```_mesh``` and the result looks like this:

![Cylinder with flat and faded color surfaces] (_images/CylinderColors.png)

Now let's answer the last question of the practice block above: As you remember from [Tutorial 03] (../Tutorial03#normals), there's one single
normal present at each vertex of every triangle. The curved coating surface of the cylinder is made up of individual triangles as well. 
But instead of copying each vertex as many times as there are triangles hung up on that vertex, all vertices on curved surfaces are
present only once. In addition each vertex along a curved surface gets assigned a normal that's calculated as the mean of the triangle
normals meeting at that vertex. Take a look at the following image:

![Cylinder with vertices, faces and normals] (_images/CylinderPolysVertsNormals.png)

Note that the purple top vertex normals have exactly the same direction as the top surface normal itself would have. This does not hold for 
the orange/yellow/green vertex normals at the rims of the coating surface. These normals are each somewhat half way between the normals that 
would be present on the respective two neighboring rectangles that build up the coating surface.

On each vertex you see the normals present at that vertex. If a vertex has more than one normal (as seen on the top rim of the cylinder),
then the vertex is present multiple times in the vertex list as seen on the cube from [Tutorial 03] (../Tutorial03#normals). These vertex 
normals are given in the file ```Cylinder.fus```, from there they are copied to the ```_mesh``` and then are passed into ```fuNormal```
when the ```_mesh``` gets rendered. The normal colors represent
directions: Normals with the same color look into the same direction. The normals at the vertices are passed to the vertex shader just
as they are shown here. Our vertex shader simply passes through the normals to the pixel shader:

```C#
	normal = fuNormal;
```

Since ```normal``` is a ```varying``` variable their values are interpolated when arriving at the pixel shader. 
In the image above you can see six pixels for which the pixel shader is called. In each of these six calls, 
the value of ```normal``` is different because each pixel's positon is different from the others with respect to 
the positions (and thus the normals) of the surronding vertices. Since in our pixel shader we directly interpret
the normals as colors

```C#
	gl_FragColor = vec4(normal*0.5 + 0.5, 1);
```

we're ending up with each pixel given a different color.

We we want to change this by calculating 






   
   
 

	
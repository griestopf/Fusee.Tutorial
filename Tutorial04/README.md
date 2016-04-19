#Tutorial 04

##Goals
 - Learn to load 3D models as assets.
 - Understand Fusee's built-in ModelView and Projection matrices.
 - See how hierachical geometry can be handled in object hierarchies.
 - Implement a very simple lighting calculation in the pixel shader.
 
##Reading Meshes From Files
FUSEE comes with a set of classes designed to be written to and loaded from files (serialized). These classes are 
containers for data types typically found in 3D scenes, such as polygonal geometry, material (color) 
settings, textures, and hierarchies. In this tutorial we will only look at how to read mesh geometry from files
in the FUSEE file format (*.fus).

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

You may notice that the explicit definition of the cube geometry, where every of the 24 vertices and 24 normals were listed
together with the 12 triangles making up the cube, is no longer there. We still have the ```_mesh``` field instantiated 
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

##Using FUSEE's Standard Matrices
Instead of using our self-defined `_xform` we can use a set of matrices which are maintained by FUSEE's render 
context (`RC`) and automatically propagated from the main application running on the CPU to the vertex shader 
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
 `RC.TransProjection`             | Read            | `uniform vec4 FUSEE_TP`    | The transposed Projection matrix.
 `RC.TransModelViewProjection`    | Read            | `uniform vec4 FUSEE_TMVP`  | `Transpsoe(MV*P)`
 `RC.InvTransModelView`           | Read            | `uniform vec4 FUSEE_ITMV`  | The inverted transposed Model-View matrix.
 `RC.InvTransProjection`          | Read            | `uniform vec4 FUSEE_ITP`   | The inverted trasposed Projection matrix.
 `RC.InvTransModelViewProjection` | Read            | `uniform vec4 FUSEE_ITMVP` | `Invert(Transpsoe(MV*P))`
 
Quite a lot - but keep in mind that theses matrices are the vehicles that bring coordinates back and forth through the various 
steps taken by geometry when transformed from model coordinates into clip coordinates. In upcoming tutorials we will see a number
of examples where some of the above matrices will be used. In this tutorial we will only write to 
`RC.ModelView` and to `RC.Projection` from within `RenderAFrame()` and the product of ModelView and Projection 
in the vertex shader out of `uniform vec4 FUSEE_MVP`. To do this follow these steps:

 - Inside the vertex shader simply replace the identifier `xform` with `FUSEE_MVP`:

   ```C#
		private Mesh _mesh;
		private const string _vertexShader = @"
			attribute vec3 fuVertex;
			attribute vec3 fuNormal;
			uniform mat4 FUSEE_MVP;
			varying vec3 modelpos;
			varying vec3 normal;
			void main()
			{
				modelpos = fuVertex;
				normal = fuNormal;
				gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
			}";
   ```

 - On the class level, you can completely remove the declaration of the fields `private IShaderParam _xformParam;` and
   `private float4x4 _xform;`. Just delete them.

 - Inside `Init()` completely remove the initialization of `_xformParam` and `_xform`. 
 
 - Inside `RenderAFrame()` assign the calculation result for the projection matrix directly to `RC.Projection`. Remove the local 
   variable `projection`:
   ```C#
      RC.Projection = float4x4.CreatePerspectiveFieldOfView(3.141592f * 0.25f, aspectRatio, 0.01f, 20);
   ```
  
 - In the *two* `_xform` matrix setup lines before the *two* `RC.Render(_mesh);` replace `_xform` with `RC.ModelView` and omit
   the `projection` matrix from the calculation, since it's already set. Here's the first of the *two* calls:
   ```C#
   RC.ModelView = view *  cube1Model * float4x4.CreateScale(0.5f, 0.1f, 0.1f);
   ``` 
 - At both places delete the `RC.SetShaderParam(_xformParam, _xform);` line below since FUSEE takes care of passing the contents of 
   `RC.ModelView` up to the vertex shader.

As a result, the application should build and run with no visible changes. So why did we do that? There are a number of 
advantages in using these pre-defined matrices over our first approach using our self-defined `_xform`:
 1. Shader code is re-usable using the above conventions. Otherwise the CPU-Code needs to be adapted to the shader code to set these
    bread-and-butter states.
 2. FUSEE automatically keeps these values actual on the CPU-Side. Multiplications, inversions and transpositions are only calculated
    once unless any of the writable matrices is updated. Calculations also only take place if a certain matrix is read from.
 3. FUSEE checks shader code if any of the above matrices are declared as `uniform` variables and only calculates/propagates the
    matrices needed by a shader. 
 4. When replacing the current shader during rendering, FUSEE automatically updates any of the matrices above. No need to call
    `RC.SetShaderParam` after each shader change.
 
Now let's apply further changes to the current state. Inside ```RenderAFrame()```:
 1. Completely Remove the second of the two cubes from the scene.
 2. Apply a uniform scale to the first cube of 0.5 along all three axes.

Here's the resulting code:

```C#
	// First cube
    var cube1Model = ModelXForm(new float3(-0.5f, 0, 0), new float3(_pitchCube1, _yawCube1, 0), new float3(0, 0, 0));
    RC.ModelView = view*cube1Model * float4x4.CreateScale(0.5f, 0.5f, 0.5f);
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

We we want to change this by applying a more sophisticated color calculation taking the normal into account. Imagine we
had a light source emitting parallel light into the viewing direction of the camera looking at the scene. 
Surfaces that are oriented towards the camera`s viewing direction (and thus the light rays) will be lit more intensive than surfaces 
facing away from the camera.


Take a look at the image below. You can see the camera and the view coordinate system. The blue coordinate axis is the z-axis of the view
coordinate system and this is also the direction of all light rays. There are three example positions on the cylinder where an 
intensity should be calculated. The normals at these example positions are given and also the opposite light ray direction in blue. Imagine
the opposite light ray direction as the "direction towards the light source". In short we will call this opposite light ray direction just
the *light direction*. Now you can see that the intensity (how light it is) at a given point depends on how close the normal vector at that point
is to the light direction:

![Simple Lighting Model] (_images/LightingSimple.png)

In view coordinates (the coordinate system where the virtual camera is the center and the z-axis is the viewing direction), 
the light direction is specified by the vector `(0, 0, -1)`. So we could calculate the angle between a normal vector in view coordinates, 
and the vector `(0, 0, -1)` and could derive an intensity from this vector: The smaller the angle, the lighter, the bigger the angle, the
darker it becomes at that position. If the angle is 90° or bigger, no light at all will be at that position.

Instead of first calculating the angle and then invent some function as above we can directly use the [dot product] (https://en.wikipedia.org/wiki/Dot_product) between the two vectors. If the two vectors both have have length 1 (if they are normalized)
then the dot product yields the cosine of the angle and that's pretty much what we want: A value that's 1 if the angle is 0 and that's 0 
if the angle is 90° or bigger. So in a first step we directly want to use the result of the dot product between the normal vector and 
the light direction as the red, green and blue intensity of the resulting color. Thus we need to change our pixel shader code to look like this:

```C#
	private const string _pixelShader = @"
		#ifdef GL_ES
			precision highp float;
		#endif
		varying vec3 modelpos;
		varying vec3 normal;

		void main()
		{
			float intensity = dot(normal, vec3(0, 0, -1));
			gl_FragColor = vec4(intensity, intensity, intensity, 1);
		}";

```
Note how the `intensity` is calculated as the cosine betwenn `(0, 0, -1)` and the normal vector in `float intensity = dot(normal, vec3(0, 0, -1));`.
In the next line this intensity is used as red, green and blue for the resulting color.

If you build and run this change, the cylinder is lit as if the light source would be attached to the cylinder and not to the camera.
This is because we perform this calculation using the normal in model coordinates and NOT in view coordinates. So we also need to adjust
our vertex shader to transform the normals into view coordinates first:

```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;
		attribute vec3 fuNormal;
		uniform mat4 FUSEE_MVP;
		uniform mat4 FUSEE_MV;
		varying vec3 modelpos;
		varying vec3 normal;
		void main()
		{
			modelpos = fuVertex;
			normal = normalize(mat3(FUSEE_MV) * fuNormal);
			gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
		}";
```

Two things happened: 

 1. We do not only need the composite ModelViewProjection matrix but additionally the ModelView transformation as well. This is
    because we want to perform our lighting calculation in view space and NOT in clip space. So we simply declare 
	`uniform mat4 FUSEE_MV` and can be sure to get the modelview matrix as well. 
 2. In `main()` we then multiply the normal with FUSEE_MV. But FUSEE_MV is of course a 4x4 matrix because it typically 
    contains translations which cannot be expressed in 3x3 matrices. Our normal by the way is somewhat different from a 
	position vector. It contains an orientation and NOT a position in 3-space. So all we want to do with a normal to beam
	it up into view space is, to perform the rotations (and to some extent the scale) part of the transfromation on it. 
	Thus we cast our 4x4 ModelView matrix to a 3x3 matrix (`mat3(FUSEE_MV)`). 

After transforming the normal with this matrix, we normalize the normal, that is, we stretch or shrink it
appropriately to make its length 1. Remember, that the dot product only returns cosine values if the vectors passed into it have unit length.
Since we built scale components into our modelview matrix, we need to normalize the results here.

Building and running thesse changes show a lit cylinder:

![A lit cylinder] (_images/CylinderDiffuse.png)

###Practice
 - Want some maths? In the vertex shader use the `FUSEE_ITMV` matrix instead of the `FUSEE_MV` matrix. The result seems to be the same!
   Look at the table above what's behind `FUSEE_ITMV`, then read  
   [OpenGL Red Book, Appendix F] (http://www.glprogramming.com/red/appendixf.html) *Transforming Normals* until *Whew!* and then try to explain:
   - Why it is mathematically correct to use this matrix and why it's wrong to use the modelview matrix to tranform
     the normals?
   - Why - at least in our example - it seems to make no difference using the MV or ITMV matrix?
 - What would happen if we performed the lighting calculation in clip space - in other words: if we transformed the 
   normals using MVP and not MV only?
 - More hands-on and less maths: How would you apply colors to objects other than the grey we're having now, but still
   maintaining that 3D shaded look we worked so hard on?
   
##Self Contained Objects
We can now have different sets of 3D geometry each making a model (cubes, spheres, cylinders, ...), We can position, scale and rotate 
them and we can (or we soon learn how to) to give them individual colors.

Imagine to create a scene built of a lot of individual models like those currently in the `Core/Assets` folder. Your
`RenderAFrame()` method would become a long list of repeated instructions like:
 - Set the current ModelView transformation.
 - Set the current color (and probably other calculation parameters in the future).
 - Render a certain mesh.
If you have hierarchies of objects you would additionally have to track parent-child relationships by chaining the
correct model matrices of parents and grandparents before rendering the children. Remember the 
[robot exercise from Tutorial 03] (../Tutorial03#exercise).

Soon you would end up with a cluttered and unstructured block of code. To avoid that,
many 3D-realtime environments use the concept of a ***Scene Graph*** or ***Scene Tree***.
Such a data structure is made up out of nodes where each node represents a visible 
object on screen. Each node contains all relevant data needed to render a node, e.g.
the geometry, the color/material settings and its position and orientation in 
the 3D world coordinate system.

In object-oriented programming environments it is common to use the programming 
language's possibility to express objects to create such structures. Let's 
do this here by adding a new class to our project that will contain all relevant
information. Right-Click on the **Fusee.Tutorial04.Core** project and Choose "Add->Class"
from the context menu. 

In the "Add New Item" dialog name your class "SceneOb.cs". A new source code file will
be created and added to the project. Fill in the following code:

```C#
using Fusee.Engine.Core;
using Fusee.Math.Core;

namespace Fusee.Tutorial.Core
{
    public class SceneOb
    {
        public Mesh Mesh;
        public float3 Albedo = new float3(0.8f, 0.8f, 0.8f);
        public float3 Pos = float3.Zero;
        public float3 Rot = float3.Zero;
        public float3 Pivot = float3.Zero;
        public float3 Scale = float3.One;
        public float3 ModelScale = float3.One;
    }
}
```
You should be familiar with most entries from the parameters of the `ModelXForm(..)` 
method introduced in the [Parent-Child relations section of Tutorial 03]
(../Tutorial03#parent-child-relations). What's new here is that we split up the 
scale into two parts, `Scale` and `ModelScale`. We choose to do this because once 
we expand `SceneOp` to handle parent-child relations, we want to be able to apply
a scale either on the contained geometry only, or on the entire object including
all children and grand-children. In addition, note the `Albedo` entry which we
will use as the base color of an object - it's a float3 containing a red, a
green and a blue component. The standard color is initialized to 80% grey.

Add the cone, the cube, the cylinder, the pyramid and the sphere to the project's 
`Assets` folder and set their `Build Action` and `Copy...` properties as described
above.

Let's first setup our pixel shader to handle the `albedo`:

```C#
	...
	uniform vec3 albedo;

	void main()
	{
		float intensity = dot(normal, vec3(0, 0, -1));
		gl_FragColor = vec4(intensity * albedo, 1);
	}
```

Then add a small helper method to our `Tutorial` class that allows us to 
load a `Mesh` with a single method call.

```C#
	public static Mesh LoadMesh(string assetName)
	{
		SceneContainer sc = AssetStorage.Get<SceneContainer>(assetName);
		MeshComponent mc = sc.Children.FindComponents<MeshComponent>(c => true).First();
		return new Mesh
		{
			Vertices = mc.Vertices,
			Normals = mc.Normals,
			Triangles = mc.Triangles
		};
	}
```

On the `Tutorial` class level, add two fields, one to access the `albedo` and another one to hold a list of
`SceneOb` instances:

```C#
    private IShaderParam _albedoParam;
	private List<SceneOb> _sceneList;
```

In our `Init` method, load the meshes and then fill the `_sceneList` with `SceneOb` instances holding these meshes.

```C#
	public override void Init()
	{
		// Initialize the shader(s)
		var shader = RC.CreateShader(_vertexShader, _pixelShader);
		RC.SetShader(shader);
		_albedoParam = RC.GetShaderParam(shader, "albedo");

		// Load some meshes
		Mesh cone = LoadMesh("Cone.fus");
		Mesh cube = LoadMesh("Cube.fus");
		Mesh cylinder = LoadMesh("Cylinder.fus");
		Mesh pyramid = LoadMesh("Pyramid.fus");
		Mesh sphere = LoadMesh("Sphere.fus");

		// Setup a list of objects
		_sceneList = new List<SceneOb>(new []
		{
			// Body
			new SceneOb { Mesh = cube,     Pos = new float3(0, 2.75f, 0),     ModelScale = new float3(0.5f, 1, 0.25f),      },
			// Legs
			new SceneOb { Mesh = cylinder, Pos = new float3(-0.25f, 1, 0),    ModelScale = new float3(0.15f, 1, 0.15f),     },
			new SceneOb { Mesh = cylinder, Pos = new float3( 0.25f, 1, 0),    ModelScale = new float3(0.15f, 1, 0.15f),     },
			// Shoulders
			new SceneOb { Mesh = sphere,   Pos = new float3(-0.75f, 3.5f, 0), ModelScale = new float3(0.25f, 0.25f, 0.25f), },
			new SceneOb { Mesh = sphere,   Pos = new float3( 0.75f, 3.5f, 0), ModelScale = new float3(0.25f, 0.25f, 0.25f), },
			// Arms
			new SceneOb { Mesh = cylinder, Pos = new float3(-0.75f, 2.5f, 0), ModelScale = new float3(0.15f, 1, 0.15f),     },
			new SceneOb { Mesh = cylinder, Pos = new float3( 0.75f, 2.5f, 0), ModelScale = new float3(0.15f, 1, 0.15f),     },
			// Head
			new SceneOb { Mesh = sphere,   Pos = new float3(0, 4.2f, 0),      ModelScale = new float3(0.35f, 0.5f, 0.35f),  },
		});

		// Set the clear color for the backbuffer
		RC.ClearColor = new float4(1, 1, 1, 1);
	}
```

Finally in `RenderAFrame()` iterate over the `_sceneList` and render each object.
```C#
	public override void RenderAFrame()
	{
		// Clear the backbuffer
		RC.Clear(ClearFlags.Color | ClearFlags.Depth);

		float2 speed = Mouse.Velocity + Touch.GetVelocity(TouchPoints.Touchpoint_0);
		if (Mouse.LeftButton || Touch.GetTouchActive(TouchPoints.Touchpoint_0))
		{
			_alpha -= speed.x*0.0001f;
			_beta  -= speed.y*0.0001f;
		}

		// Setup matrices
		var aspectRatio = Width / (float)Height;
		RC.Projection = float4x4.CreatePerspectiveFieldOfView(3.141592f * 0.25f, aspectRatio, 0.01f, 20);
		var view = float4x4.CreateTranslation(0, 0, 8)*float4x4.CreateRotationY(_alpha)*float4x4.CreateRotationX(_beta)*float4x4.CreateTranslation(0, -2f, 0);

		foreach (var so in _sceneList)
		{
			RC.ModelView = view * ModelXForm(so.Pos, so.Rot, so.Pivot) * float4x4.CreateScale(so.ModelScale);
			RC.SetShaderParam(_albedoParam, so.Albedo);
			RC.Render(so.Mesh);
		}

		// Swap buffers: Show the contents of the backbuffer (containing the currently rendered farame) on the front buffer.
		Present();
	}
```
Note how we changed the setting of the view matrix to make the overall object fit into 
the viewing frustum: We simply place the camera now 8 units away from the object (instead of 3) and position the camera 2 units up (or rather take the object 2 units down).

The result should look like this:

![Man made of primitives] (_images/TinMan.png)


###Practice
 - Add more objects to make a more sophisticated tin man.
 - Set the arms' and legs' pivot points correctly and add some input axes to 
   control their rotation angles.
 - Toy around with the objecs' albedos to add some more color.
 
As a last step we will now add the possibility of creating hiearchies to our
`SceneOb` Class. To do this, we put the `_sceneList` into the object itself-

Create a field `Children` within `SceneOb`:
```C#
	public List<SceneOb> Children;
```
Note that each `SceneOb` can now hold an arbitrary number of `SceneOb`s as children.

In the `Tutorial` class replace the `_sceneList` field by a single `SceneOb` field called `_root`:
```C#
	private SceneOb _root;
```

Without having a `_sceneList` any more, we now need to instantiate the `_root` and fill its
list of children instead inside our `Init()` method:

```C#
	// Setup a list of objects
	_root = new SceneOb { 
		Children = new List<SceneOb>(new []
		{
			// Body
			new SceneOb { Mesh = cube,     Pos = new float3(0, 2.75f, 0),     ModelScale = new float3(0.5f, 1, 0.25f),      },
			// Legs
			new SceneOb { Mesh = cylinder, Pos = new float3(-0.25f, 1, 0),    ModelScale = new float3(0.15f, 1, 0.15f),     },
			new SceneOb { Mesh = cylinder, Pos = new float3( 0.25f, 1, 0),    ModelScale = new float3(0.15f, 1, 0.15f),     },
			// Shoulders
			new SceneOb { Mesh = sphere,   Pos = new float3(-0.75f, 3.5f, 0), ModelScale = new float3(0.25f, 0.25f, 0.25f), },
			new SceneOb { Mesh = sphere,   Pos = new float3( 0.75f, 3.5f, 0), ModelScale = new float3(0.25f, 0.25f, 0.25f), },
			// Arms
			new SceneOb { Mesh = cylinder, Pos = new float3(-0.75f, 2.5f, 0), ModelScale = new float3(0.15f, 1, 0.15f),     },
			new SceneOb { Mesh = cylinder, Pos = new float3( 0.75f, 2.5f, 0), ModelScale = new float3(0.15f, 1, 0.15f),     },
			// Head
			new SceneOb { Mesh = sphere,   Pos = new float3(0, 4.2f, 0),      ModelScale = new float3(0.35f, 0.5f, 0.35f),  },
		})};
```

Add a method to render a `SceneOb` to the Tutorial class. Rendering a `SceneOb` is now not only
setting the `SceneOb`'s parameters and render its mesh but also ***recursively*** render all 
listed `Children`. In addition, a SceneOb may have no mesh at all and may also have an empty
list of children, so we need to check these for `null`. Even without a valid `Mesh`, 
a resulting transformation will be calculated and passed on the children. This way, a child
object inherits its parent's transformations and can apply its own transformations to be relative
to its parent.

```C#
	void RenderSceneOb(SceneOb so, float4x4 modelView)
	{
		modelView = modelView * ModelXForm(so.Pos, so.Rot, so.Pivot) * float4x4.CreateScale(so.Scale);
		if (so.Mesh != null)
		{
			RC.ModelView = modelView*float4x4.CreateScale(so.ModelScale);
			RC.SetShaderParam(_albedoParam, so.Albedo);
			RC.Render(so.Mesh);
		}

		if (so.Children != null)
		{
			foreach (var child in so.Children)
			{
				RenderSceneOb(child, modelView);
			}
		}
	}
```

Finally, in `RenderAFrame()` we can strip down the rendering of the scene to a single line rendering the `_root` and passing the `view` matrix as the
ModelView matrix to start with.

```C#
	// Setup matrices
	var aspectRatio = Width / (float)Height;
	RC.Projection = float4x4.CreatePerspectiveFieldOfView(3.141592f * 0.25f, aspectRatio, 0.01f, 20);
	float4x4 view = float4x4.CreateTranslation(0, 0, 8)*float4x4.CreateRotationY(_alpha)*float4x4.CreateRotationX(_beta)*float4x4.CreateTranslation(0, -2f, 0);

	RenderSceneOb(_root, view);
```

The result should look and behave exactly as before. But now we can build up a hierarchy of objects instead of one single flat list.

![Man made of primitives] (_images/TinManColor.png)

 - Visit the [result as web application]
  (https://cdn.rawgit.com/griestopf/Fusee.Tutorial/d704a6f/Tutorial04Completed/out/Fusee.Tutorial.Web.html) 
  (Ctrl-Click or Long-Press to open in new tab).
 
 - See [Tutorial.cs] (../Tutorial04Completed/Core/Tutorial.cs) in the [Tutorial04 Completed] (../Tutorial04Completed) folder for 
   the overall state so far.

##Exercise

 - Create a hierarchy of objects with at least three levels, e.g. by extending our model to have elbows and wrists / knees and feet. Use color 
   on your objects to make the scene look nice.
 - Add the possibility to give your `SceneOb` names (strings). Add a method declared as
   
   ```C#
	public static SceneOb FindSceneOb(SceneOb so, string name)
   ```
   that will recursively traverse the tree starting with `so` and return the first `SceneOb` with the given name.
   
 - Add some way to either animate or interactively control some of the object's rotation angles. You will probably need to keep explicit 
   references to those objects. Maybe the `FindSceneOb` method is useful here. Maybe you want to use a single input axis to control more than 
   one object's rotation axis simultaneously. 

 - Just for practice: Explain to yourself how the parent object inherits its transformation to its children: Set a breakpoint inside `RenderSceneOb` and
   observe the modelView matrices.











   
   
 

	
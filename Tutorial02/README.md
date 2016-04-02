#Tutorial 02

##Goals
 - Understand how Geometry is pumped through the rendering pipeline.
 - Understand uniform shader variables.
 - Understand how data is passed from pixel to vertex shader.
 - Grasp basics of 3D transformations.

##Goals Tut 03
 - Understand why we need 4x4 matrices (and not 3x3) to perform transformations.

##Prerequisistes
 - Make sure you got [Tutorial01] (../Tutorial01) up and running.
 
##Passing more information through the pipeline
First, let's add some more triangles to the geometry. Add one more vertex and span four triangles with the four vertices to creatae a Tetrahedron ("a triangular pyramid").
```C#
	_mesh = new Mesh
	{
		Vertices = new[]
		{
			new float3(-0.8165f, -0.3333f, -0.4714f), // Vertex 0
			new float3(0.8165f, -0.3333f, -0.4714f),  // Vertex 1
			new float3(0, -0.3333f, 0.9428f),         // Vertex 2
			new float3(0, 1, 0),                      // Vertex 3
		},
		Triangles = new ushort[]
		{
			0, 2, 1,  // Triangle 0 "Bottom" facing towards negative y axis
			0, 1, 3,  // Triangle 1 "Back side" facing towards negative z axis
			1, 2, 3,  // Triangle 2 "Right side" facing towards positive x axis
			2, 0, 3,  // Triangle 3 "Left side" facing towrads negative x axis
		},
	};
```
Building and running the solution this way will still result in a single triangle being displayed. 
![Tetrahedron back side] (_images/TetrahedronBackSide.png)

This is Triangle 1, the "back side" of the tetrahedron. The other 
three triangles are obscured by the one we are seeing. Note that the visible triangle is now somewhat out of center towards the upper 
border of the window. This is because the vertex coordinates used above are chosen to make their common origin (0, 0, 0) to be the tetrahedron's center of gravity. 

##Rotating it
Now we would like to rotate the tetrahedron. 

There are two ways to accomplish this:

 1 We could change the Mesh's vertex coordinates every frame within `RenderAFrame`.
 2 We could transform the Mesh's vertex coordinates every frame from within the vertex shader.

 The typical way to perform coordinate transformations is option 2, especially if we are performing linear transformations (a rotation is a linear tranformation). 
Let's recall some maths to see how an arbitrary 2D vector (x, y) is rotated around an angle alpha:
```
x' = x * cos(alpha) + y * -sin(alpha)
y' = x * sin(alpha) + y *  cos(alpha)
```
From linear algebra you might remember that this can as well be written in matrix format. We will revisit matrices later on. Right now we want to apply the above 1:1 to 
our vertex shader:
```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;

		void main()
		{
			float alpha = 3.1415 / 4; // (45 degrees)
			float s = sin(alpha);
			float c = cos(alpha);
			gl_Position = vec4( fuVertex.x * c - fuVertex.y * s,   // The transformed x coordinate
								fuVertex.x * s + fuVertex.y * c,   // The transformed y coordinate
								fuVertex.z,                        // z is unchanged
								1.0);
		}";

```
Building and running should result in the triangle rotated about 45 degrees in counterclockwise order.
![Tetrahedron 45 degrees] (_images/Tetrahedron45Deg.png)

###Practice
 - Try other amounts for alpha to see how the rotation behaves.
 - Go 3D! Rotate around the y-axis instead of the z-axis (that is, leave fuVertex.y unchanged and apply the sin/cos factors to x and z. This way you will 
   get to see the other sides of the tetrahedron. Unfortunately you cannot tell the border between the different sides because they are all white,
   so you'll always see a triangular silhouette.

   
## Animation
Notice that we now have a single parameter (`alpha`) controlling the transformation of all vertices within our mesh. This parameter is constantly set in
the vertex shader. If we could find a way to alter the value of `alpha` from one frame to the other, we could implement a rotation animation. Shader
languages allow to set individual values from the "outside world" through so called "uniform variables". Let's change `alpha` from a constant local variable
inside the vertex shader's `main` function to a more "global" uniform variable. Change the first lines of the vertex shader like this:

```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;
		uniform float alpha;

		void main()
		{
			float s = sin(alpha);
	...
```
'alpha' now looks like a global variable (outside of `main`). In addition it is decorated with the `uniform` keyword, which marks it being a value that changes
rather infrequently (the vertex shader will be called for a lot of vertices while `alpha`'s value doesn't change). This is in contrast to the `fuVertex` variable
on the line above which contains a different value (the vertex itself) for each time the vertex shader is called.

Before we can change the value of `alpha` (which - as part of the vertex shader - lives on the GPU) from the application code (which runs on the CPU), we need to 
akquire an identifier to access our variable. First we need to declare two fields within our `Tutorial` class:

```C#
	private IShaderParam _alphaParam;
	private float _alpha;
```

The filed `_alphaParam` will keep an identifier (a handle) to the shader variable `alpha` while the field `_alpha` will keep the actual value of 
the GPU-varialbe `alpha` in CPU-Land.

Inside the `Init` method, right after creating the shader from source code, we can initialize both fields:
```C#
	var shader = RC.CreateShader(_vertexShader, _pixelShader);
	RC.SetShader(shader);
	_alphaParam = RC.GetShaderParam(shader, "alpha");
	_alpha = 0;
```

Then, in the `RenderAFrame` method we can alter the contents of `_alpha` and then pass the new value up to the GPU's `alpha` variable:
```C#
	_alpha += 0.01f;
	RC.SetShaderParam(_alphaParam, _alpha);
```

This way, each frame the angle `alpha` will be incremented about 0.01 radians.

Compiling and building will show a somewhat rotating triangle. If you rotate around the y-axis as proposed in the previus paragraph, you will rather see 
a triangle bouncing back and forth. Remember that you are really seeing the triangular silhouette of a rotating threedimensional tetrahedron. 

##Color


##Interaction



##Practice
 - Create a more complex geometry (e.g. a little house)
 - Create 


